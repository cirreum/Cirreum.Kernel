# Cirreum.Kernel 2.0.0 — Facts, Not Guesses

## Why this release exists

This release started as a narrow bug fix and ended as a removal.

While hardening `ClaimsHelper` against blank claim values, an audit of the issuer-to-provider table
turned up a long list of its own problems — unanchored host matching, Entra v1.0 tokens
unrecognized, Azure AD B2C unrecognized despite an enum member documenting it, Keycloak recognized
only on a path prefix dropped in 2022, and a bare `amazonaws.com` match claiming every identity
provider that happened to run on AWS. Each was fixable, and each was fixed.

Fixing them raised the prior question: **what consumes this, and why is it inferred at all?**

`IdentityProviderType` documented itself as identifying *"which identity provider is **configured**
for authentication"* — a fact declared once at composition time. The implementation re-derived it by
substring guesswork on every authorized request, and again independently in the browser. A full
trace found no framework consumer at all.

That turned out to be the theme running through everything here. Each change in this release fixes a
value or a name that **looked authoritative and wasn't**: a blank claim escaping as an answer, a
classification presented as a fact, a host-level constant named as if it were ambient, and one word
serving two opposite concepts.

## What's removed

`IdentityProviderType`, and everything that produced or exposed it — `UserProfile.Provider`,
`IUserState.Provider`, `ClaimsHelper.ResolveProvider`, and the vendor table behind them.
`Cirreum.Contracts` drops `Provider` / `IsFromProvider` from `OperationContext` and
`AuthorizationContext` in the same wave.

Nothing replaces it, because the framework already carried better answers to every question it was
asked:

| Question | Answered by |
|---|---|
| Which authentication context produced this identity? | `AuthenticationContextKeys.AuthenticatedScheme` — configuration-tied, propagated across HTTP, SignalR, and WebSocket connections, treated as a reserved claim with anti-spoofing coverage |
| What did the token actually say? | **`UserProfile.Issuer`** — new in this release |
| Which identity provider does this application use? | Its own composition — a compile-time constant for a single-IdP client |

The removal also retires a class of problem rather than a bug: custom auth domains are unguessable
by construction, and both Auth0 and Okta rewrite the issuer to a vanity domain by default. No table
can be correct about them. And a value that returned `Unknown` for a perfectly valid token no longer
sits on the authorization surface, where `IsFromProvider` invited gating access on a best-effort
string match.

## What's new

**`UserProfile.Issuer`** — the `iss` claim, verbatim, resolved at construction alongside `Id` and
`Name`. The fact survives; only the guess is gone. Because it comes from the token rather than a
local table, a WebAssembly client and the API it calls cannot disagree about it, and an issuer no
table ever named is still identified exactly.

```csharp
// Was: a classification that could be Unknown for a valid token
if (profile.Provider == IdentityProviderType.Descope) { }

// Now: the fact, which an application can match with certainty about its own IdP
if (profile.Issuer?.Contains("descope.com", StringComparison.OrdinalIgnoreCase) == true) { }
```

**`IdentityScope`**, and an optional `scope` on `ClaimsHelper.ResolveRoles(ClaimsPrincipal)` — read
every identity the principal carries (the default, and the breadth `ClaimsPrincipal.IsInRole` spans)
or only the identity it presents. Behavior is unchanged; the axis is now expressible at the call
site. It exists on roles alone, because roles aggregate — reading a singular fact across identities
is not a broader answer but a wrong one.

## What's renamed

**`DomainContext.CurrentActivityKind` → `EntryPointActivityKind`.** "Current" read as ambient or
per-span, which is the exact misreading that leads to using it for an outbound call. `ActivityKind`
describes a span's role in a trace, not the process emitting it: one host emits `Server` for the
request it handles, `Client` for the call it makes downstream, and `Producer` for the message it
publishes — all in the same request. A host-derived kind on an outbound call marks it `Server`, a
span claiming to receive a request it is actually making, and any backend then draws the wrong
graph. The new name puts the usage rule at every call site.

**`INotification` → `IDomainEvent`, `INotificationHandler<T>` → `IDomainEventHandler<T>`.** Cirreum
used "notification" for two unrelated concepts: Conductor's in-application publish/subscribe, and
the human-facing state family a client binds to in order to show a person something. They travel in
opposite directions and have unrelated lifetimes, so "notification handler" resolved to either
*reacts to something that happened* or *renders something for a user* depending on which package you
were reading.

`INotificationState` and `IScopedNotificationState` deliberately keep their names — they are the
human-facing concept, and that separation is the entire point. **A project-wide find/replace of
"Notification" will destroy it.**

## What's fixed

The defects that started this release, and the most consequential part of it for anything already in
production:

- **Blank claims escaped as answers.** `ResolveName`, `ResolveOid`, and `ResolveTid` each guarded
  their resolution rungs correctly and then returned the last assigned value regardless — so a
  whitespace-only claim came back as a non-null string. That defeats every caller's fallback:
  `UserProfile.Name`, `.Oid`, and `Organization.OrganizationId` are all assigned from a
  `?? default`, which cannot fire against a non-null value. A whitespace name reached logs and audit
  records verbatim; a whitespace tenant id reached the value that draws the multi-tenant boundary.

- **`ResolveId` let a blank claim shadow a populated one.** It short-circuited on the first
  *non-null* claim rather than the first non-blank, so a blank `oid` suppressed a valid `sub` and
  became the resolved user identifier.

- **Singular facts could be answered by the wrong identity.** `ClaimsPrincipal.FindFirst` searches
  every identity in order, so on a multi-scheme principal an id, name, tenant, or issuer could come
  from a secondary identity — producing a coherent-looking profile assembled from two subjects.
  `ResolveId` was the worst case, walking claim *types* in priority order so a secondary identity's
  `oid` outranked the primary's `sub`. Every singular-fact resolver now reads the primary identity or
  returns `null`; there is no principal-wide search left to guard against.

- **`ResolveName`'s last rung could never contribute.** It resolved `identity.NameClaimType`, which
  `Identity.Name` already answers one rung earlier. It now resolves
  `ClaimsIdentity.DefaultNameClaimType`, catching principals minted by WS-Fed, cookie
  authentication, or a handler that left inbound claim mapping enabled.

Every resolver now routes through one helper, so the shape that produced these no longer exists and
the rule is stated once: **a blank claim is absent, not an answer.**

## Compatibility

Breaking. See [`MIGRATION-v2.md`](MIGRATION-v2.md), which covers all three changes and includes the
find/replace warning above.

Migration is usually smaller than the version bump suggests. In practice, provider comparisons hide
three different questions, and only one of them wants the issuer:

| The check was asking | Replace with |
|---|---|
| "Is this user authenticated?" (`Provider != None`) | `IsAuthenticated` |
| "Is some capability available?" (`Provider == Entra`) | the capability itself — it survives adding a second identity provider |
| "Which provider issued this token?" | `UserProfile.Issuer`, or the authenticated scheme server-side |

Every removal is a compile error rather than a silent behavior change.

## Coordinated downstream work

`Cirreum.Kernel` is the Base layer, so this cascades bottom-up. `Cirreum.Contracts`,
`Cirreum.Messaging.Distributed`, `Cirreum.AuthenticationProvider`, `Cirreum.Domain`,
`Cirreum.Runtime.AuthenticationProvider`, and `Cirreum.Runtime.Messaging` each take a major in the
same wave, with `Cirreum.Authentication.External` and `Cirreum.Services.Wasm` taking patches. Each
ships its own migration guide.

## See also

- [`MIGRATION-v2.md`](MIGRATION-v2.md) — the full find/replace tables and walkthroughs
- [`CHANGELOG.md`](CHANGELOG.md) — the itemized record
