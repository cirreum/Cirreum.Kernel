# Cirreum.Kernel v1 → v2 Migration

v2 carries three independent breaking changes: Conductor's publish/subscribe markers are renamed,
`DomainContext.CurrentActivityKind` is renamed, and `IdentityProviderType` is removed.

---

## `DomainContext.CurrentActivityKind` → `EntryPointActivityKind`

| Renamed | To |
|---|---|
| `DomainContext.CurrentActivityKind` | `DomainContext.EntryPointActivityKind` |

A mechanical rename with no behavior change, but the new name carries a rule worth knowing.

`ActivityKind` describes **a span's role in a trace**, not the process emitting it. A single host
emits `Server` for the request it handles, then `Client` for the call it makes downstream, then
`Producer` for the message it publishes — all within one request. A backend pairs your `Client`
span with the downstream service's `Server` span to reconstruct topology, so a wrong kind does not
merely mislabel a span, it draws the wrong graph.

This property answers a different question — *what kind of host is this* — and the two coincide at
exactly one span per request: the one where work arrives.

```csharp
// Correct — the entry-point span
_activitySource.StartActivity("Dispatch Operation", DomainContext.EntryPointActivityKind);

// Wrong — an outbound call is Client regardless of host
_activitySource.StartActivity($"HTTP {method}", DomainContext.EntryPointActivityKind);

// Correct
_activitySource.StartActivity($"HTTP {method}", ActivityKind.Client);
```

When adding telemetry to a track that has none, the question is not "which kind does this host use"
but "does this span receive work, send work, or neither." Pass the kind explicitly even when it is
`Internal` — the default is the same, but stating it records that the choice was made.

---

## Why the Conductor markers were renamed

Cirreum used "notification" for two unrelated concepts. Conductor's `INotification` /
`INotificationHandler<T>` are **in-application communication** — one part of the system telling the
rest that something happened. The notification *state* family (`INotificationState`,
`IScopedNotificationState`, and the WebAssembly state services built on them) is **human-facing** —
what a client binds to in order to show a person something.

They travel in opposite directions and have unrelated lifetimes. Sharing the word meant
"notification handler" resolved to either "reacts to something that happened" or "renders something
for a user" depending on which package you were reading.

| Renamed | To |
|---|---|
| `INotification` | `IDomainEvent` |
| `INotificationHandler<TNotification>` | `IDomainEventHandler<TDomainEvent>` |
| `HandleAsync(notification, …)` parameter | `HandleAsync(domainEvent, …)` |

Behavior, dispatch semantics, and fan-out are unchanged. A handler becomes:

```csharp
// Before
public sealed class OrderPlacedHandler : INotificationHandler<OrderPlaced> {
	public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken) { }
}

// After
public sealed class OrderPlacedHandler : IDomainEventHandler<OrderPlaced> {
	public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken) { }
}
```

**Do not rename the notification state family.** `INotificationState`,
`IScopedNotificationState`, `NotificationState`, and `NotifySubscribers` are the human-facing
concept and keep their names — preserving that separation is the entire point of the change. A
project-wide find/replace of "Notification" will destroy it.

---

## Why `IdentityProviderType` was removed

`IdentityProviderType` is removed. The enum documented itself as identifying *"which identity
provider is configured for authentication"*, but the implementation inferred it on every
`UserProfile` construction by matching the `iss` claim against a built-in table of vendor domains.
A fact declared once at composition time was being re-derived by substring guesswork on every
authorized request, and again independently in the browser.

Nothing in the framework consumed the result. Every `.Provider` past `UserProfile` was a
pass-through getter; `IsFromProvider` had no call sites anywhere, including in consuming
applications. The questions it appeared to answer already had better answers, which is why this is
a removal rather than a replacement.

`UserProfile.Issuer` is added in the same release — the `iss` claim, verbatim. The fact survives;
only the guess is gone.

## Breaking Changes — Find/Replace Table

| Removed | Replace with |
|---|---|
| `IdentityProviderType` | — (no equivalent type; see below) |
| `UserProfile.Provider` | `UserProfile.Issuer` for the provider's identity |
| `IUserState.Provider` | `UserState.Profile.Issuer` |
| `UserStateBase.Provider` | `Profile.Issuer` |
| `ClaimsHelper.ResolveProvider(ClaimsPrincipal)` | `ClaimsHelper.ResolveIssuer(ClaimsPrincipal)` |
| `ClaimsHelper.ResolveProvider(ClaimsIdentity)` | `ClaimsHelper.ResolveIssuer(ClaimsIdentity)` |

`Cirreum.Contracts` drops `OperationContext.Provider` / `.IsFromProvider` and
`AuthorizationContext.Provider` / `.IsFromProvider` in the same wave — see its own
`MIGRATION-v2.md`.

## Migration Walkthrough

Which replacement applies depends on what the check was actually asking. In practice there are
three questions hiding behind provider comparisons, and only one of them wants the issuer.

### 1. "Is this user authenticated?"

```csharp
// Before
if (user.Provider != IdentityProviderType.None) { }

// After
if (user.IsAuthenticated) { }
```

`Provider` returned `None` for the anonymous principal, so it worked as an authentication test by
side effect. The direct check is clearer and does not depend on an identity label.

### 2. "Is some capability available?"

```csharp
// Before — presence comes from Microsoft Graph, so gate on Entra
if (user.Provider == IdentityProviderType.Entra) { ShowPresenceBadge(); }

// After — ask about the capability
if (presenceService.IsEnabled) { ShowPresenceBadge(); }
```

This is the most common shape, and the provider comparison was always a proxy. Gate on the
capability itself: it stays correct when a second identity provider is added, and when the
capability is served by something other than the provider it originally correlated with.

Note that checking whether a service is *registered* is not the same test — a framework default is
often registered as a no-op stand-in, so resolution succeeds whether or not the capability is
composed. Ask the service.

### 3. "Which identity provider issued this token?"

```csharp
// Before
if (profile.Provider == IdentityProviderType.Descope) { }

// After
if (profile.Issuer?.Contains("descope.com", StringComparison.OrdinalIgnoreCase) == true) { }
```

An application knows the issuers it accepts — it configured them as valid issuers — so it can
match with certainty where the framework could only guess. `Issuer` is the raw `iss` claim, so it
never drifts with a vendor rebrand, a new region, or a custom auth domain.

**Server-side, prefer the authenticated scheme.** For "which identity provider authenticated this
request", `AuthenticationContextKeys.AuthenticatedScheme` is the authoritative answer: it is
configuration-tied, survives two-phase auth promotion, is propagated across HTTP, SignalR, and
WebSocket connections, and is what every other per-scheme lookup in the framework dispatches on.

### 4. Displaying the provider

If a profile page rendered `Provider`, derive a label from `Issuer`:

```csharp
var label = profile.Issuer is { } iss && Uri.TryCreate(iss, UriKind.Absolute, out var uri)
	? uri.Host
	: "Unknown";
```

For a friendlier name, map your own known issuers. That mapping is one your application can state
with certainty; the removed table could only approximate it.

## New Capabilities

- **`UserProfile.Issuer`** — the `iss` claim, verbatim, resolved at construction alongside `Id` and
  `Name` and round-tripped through JSON. It reads identically wherever the profile is built, so a
  WebAssembly client and the API it calls cannot disagree about it.
- **`ClaimsHelper.ResolveIssuer(ClaimsPrincipal)` / `(ClaimsIdentity)`.**
- **`IdentityScope`** and an optional `scope` parameter on `ResolveRoles(ClaimsPrincipal)` — read
  every identity the principal carries (the default, unchanged behavior) or only the one it
  presents.

## Behavior Changes Worth Knowing

These are fixes rather than breaking API changes, but they change results:

- **Blank claims are now treated as absent.** `ResolveName`, `ResolveOid`, and `ResolveTid`
  previously returned a whitespace-only claim value as a non-null string, defeating callers'
  `?? default` fallbacks — a whitespace tenant id could reach the value that draws the
  multi-tenant boundary. `ResolveId` additionally let a blank `oid` suppress a valid `sub`.
- **Singular facts now resolve from the primary identity only.** On a multi-scheme principal,
  `ResolveId` / `ResolveName` / `ResolveOid` / `ResolveTid` / `ResolveIssuer` no longer reach
  across identities; they return `null` rather than borrowing a value from another authentication
  context. Roles keep their union, now explicit through `IdentityScope`.

## What Didn't Change

- `UserProfile.Id`, `.Name`, `.Roles`, `.Organization`, and every profile claim mapping
- `IUserState` apart from `Provider`
- Authentication, authorization, and scheme resolution — none of which ever consumed
  `IdentityProviderType`
- Telemetry, state management, and everything outside `Security/`

## Downstream Package Impact

`Cirreum.Kernel` is the Base layer, so this cascades bottom-up. `Cirreum.Contracts` takes its own
major for the two context types. Packages above that reference neither the enum nor `.Provider`
need only a re-pin.
