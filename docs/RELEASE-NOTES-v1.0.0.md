# Cirreum.Kernel 1.0.0 — The foundational base of the Cirreum framework

`Cirreum.Kernel` is the small, dependency-light foundation every Cirreum package builds on: the cross-host abstractions, contracts, value types, and sentinels shared by Server, WebAssembly, and Serverless hosts alike. 1.0.0 establishes that foundation as its own independently-versioned package.

**Strictly additive — initial release.** No predecessor `Cirreum.Kernel` package; nothing to break. Targets .NET 10.0.

---

## Why this release exists

The **Cirreum 1.0 Foundation Reset** factored the framework's previously-monolithic core into a small, layered set of packages over a single, dependency-light base. Kernel *is* that base — the primitives every other Cirreum package (Common, Shared, the host infrastructure, the runtime, and every application) consumes directly or transitively.

Giving those primitives one stable, conservatively-versioned home means the whole framework shares a single definition of identity, the authentication-event surface, the Conductor notification markers, and the bootstrap contracts — and that the foundation can evolve without change rippling upward. Kernel deliberately stays small and references no other Cirreum package, so depending on it never drags in implementation or track-specific weight.

---

## What's new

### Identity & security primitives

```csharp
// The current request's user — resolved the same way on every host.
IUserState user = userStateAccessor.GetUserState();   // (was GetUser() pre-1.0)
bool signedIn = user.Principal.Identity?.IsAuthenticated == true;
```

`IUserState` / `IUserStateAccessor` / `IUserSession` / `UserStateBase`, the application-user contracts (`IApplicationUser`, `IOwnedApplicationUser`, `IApplicationUserResolver`), `AnonymousUser`, `AuthenticationBoundary`, and `ClaimsHelper` — the shared shape of "who is calling" that the rest of the framework reads.

### Authentication events & keys

```csharp
public interface IAuthenticationEventPublisher {
    ValueTask PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default)
        where TEvent : IAuthenticationEvent;
}
```

The `IAuthenticationEvent` family (`IAuthenticationEventPublisher`, `IAuthenticationEventHandler`) plus the lifecycle records — `CredentialRevoked`, `SessionTerminationRequested`, `UserAccountDisabled`, `GrantsInvalidated` — and `AuthenticationContextKeys`. Apps publish on an admin action; framework handlers (cache invalidators, connection terminators) react. This is the cross-cutting bus the Authentication and Authorization tracks build on.

### Conductor notification markers

`INotification` and `INotificationHandler` — the Result-free notification primitives. The rest of the Conductor surface (`IDispatcher`, `IOperation`, intercepts) lives in `Cirreum.Common`; only the markers every package needs sit here.

### Message registry

`IMessageRegistry`, `MessageDefinition`, `MessageProperty`, `MessageRegistryBase`, `MessageScanner`, and `MessageVersionAttribute` — the versioned-message primitives used for contract discovery and evolution.

### Framework bootstrap, environment & utilities

`IDomainApplicationBuilder`, `DomainContext`, `DomainServicesBuilder`, `AssemblyScanner`, `IDomainContextInitializer`, `DomainRuntimeType`, `DomainFeatureResolver`; the user-profile model (`UserProfile` and friends, `IUserProfileEnricher`); `IDomainEnvironment` / `IDateTimeClock` / `Timing`; the `IApplicationState` marker; and health/diagnostics/utility helpers (`IStartedStatus`, `CirreumTelemetry`, `InternetDomainValidator`, plus the extension and token-interpolation helpers).

---

## Why this lives in Cirreum.Kernel

Kernel is the one package every host and every track shares, so it holds only contracts that must be reachable everywhere — never implementations or track-specific dependencies. It also does **not** reference its foundation peers (`Cirreum.Result`, `Cirreum.Exceptions`); consumers compose those as needed. That discipline is what keeps the base minimal and its version stable.

---

## Coordinated downstream work

Kernel is the bottom of the 1.0 foundation. `Cirreum.Common` and `Cirreum.Shared`, the `Cirreum.Services.{Host}` infrastructure, and the `Cirreum.Runtime.{Host}` composition all build on it — so it publishes first, bottom-up.

---

## Compatibility

- **Additive.** Initial release; no prior surface to break.
- **.NET 10.0.**
- **Dependency-light.** References no other Cirreum package.
- **Migration from `Cirreum.Core 5.x`:** install `Cirreum.Kernel` alongside the companion foundation packages. Two refinements carried in from the prior core: `IUserStateAccessor.GetUser()` → `GetUserState()`, and `IUserState.Identity` removed (cast `Principal.Identity as ClaimsIdentity` if you need the typed identity).

---

## See also

- `CHANGELOG.md` — condensed change list for `1.0.0`.
