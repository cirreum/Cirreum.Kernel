# Cirreum.Kernel Changelog

All notable changes to **Cirreum.Kernel** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

### Fixed

- `AuthenticationContextKeys` doc truth-pass for the ADR-0025/0027 wave: `ApplicationUserCache` is connection-scoped as well as request-scoped (the per-invocation contexts seed from the connection's auth slots) and is evicted by Two-Phase Auth promotion *before* `PromotedPrincipal` is stamped; `PromotedPrincipal` is written by the `connection.Promote(principal)` extension (the old `TwoPhaseAuth.Promote` static form is gone) and read through the `Cirreum.Contracts` connection-ownership surface (`PromotedUser` / `EffectiveUser` / `IsUserPromoted`) rather than directly by `UserStateAccessor`. Doc-only.

## [1.0.2] - 2026-07-04

### Fixed

- Consolidated each authentication event's differently-named timestamp (`CredentialRevoked.RevokedAt`, `UserAccountDisabled.DisabledAt`, `SessionTerminationRequested.RequestedAt`, `GrantsInvalidated.InvalidatedAt`) into one common, required `IAuthenticationEvent.OccurredAt` property, and added `CredentialRevoked.ExpiresAt` plus the `IAuthenticationEventTransportBridge` marker needed for the auth-event bus's in-process publisher and cross-replica delivery. No known consumers reference the old per-event timestamp names or depend on the auth-event bus today.

## [1.0.1] - 2026-06-04

### Fixed

- Documentation and XML doc-comments now reference the renamed foundation packages — `Cirreum.Contracts` (formerly `Cirreum.Common`) and `Cirreum.Domain` (formerly `Cirreum.Shared`). The README "Where it fits" section no longer enumerates upper-layer packages — a dependency-free floor cannot keep a consumer list current — and instead states Kernel's layer position and zero-dependency nature.

## [1.0.0] - 2026-06-04

### Added

- Initial release. Cirreum.Kernel is the foundational base of the Cirreum framework, established as part of the **Cirreum 1.0 Foundation Reset** wave.
- **Core abstractions** extracted from `Cirreum.Core 5.x`:
  - User and identity contracts: `IUserState`, `IUserStateAccessor`, `IUserSession`, `UserStateBase`, `IApplicationUser`, `IApplicationUserResolver`, `IOwnedApplicationUser`, `AnonymousUser`
  - User profile types: `UserProfile`, `UserProfileAddress`, `UserProfileMembership`, `UserProfileMembershipType`, `UserProfileOrganization`, `IUserProfileEnricher`
  - Environment and time: `IDomainEnvironment`, `IDateTimeClock`, `Timing`
  - Framework bootstrap: `IDomainApplicationBuilder`, `IDomainContextInitializer`, `DomainContext`, `DomainContextInitializer`, `DomainFeatureResolver`, `DomainRuntimeType`, `DomainServicesBuilder`, `AssemblyScanner`, `IDomainObject`
  - Cross-track enums: `IdentityProviderType` (consumed by Authentication, Identity, and potentially Authorization tracks)
  - State foundation: `IApplicationState` (the marker `IUserSession` extends; rest of state-related types live in `Cirreum.State`)
  - Health: `IStartedStatus`
  - Diagnostics: `CirreumTelemetry`
  - Utilities: `InternetDomainValidator`, `MissingResource`
- **Extensions** for the above abstractions (assembly, environment, cloning, format, string, task, system IO, user-profile, user-state, telemetry, etc.).
- **SmartFormat command sources** for Cirreum.Kernel-flavored token interpolation.
- **Authentication primitives** folded in from the dissolved `Cirreum.Authentication` package: `AuthenticationContextKeys` and the authentication event surface — `IAuthenticationEvent`, `IAuthenticationEventPublisher`, `IAuthenticationEventHandler`, and the `CredentialRevoked` / `SessionTerminationRequested` / `UserAccountDisabled` / `GrantsInvalidated` event records.
- **Security primitives**: `AuthenticationBoundary`, `ClaimsHelper` (alongside `AnonymousUser` above).
- **Conductor notification markers**: `INotification`, `INotificationHandler` — the Result-free notification primitives. The rest of the Conductor surface (`IDispatcher`, `IOperation`, `OperationContext`, intercept contracts, etc.) lives in `Cirreum.Contracts`.
- **Message registry**: `IMessageRegistry`, `MessageDefinition`, `MessageProperty`, `MessageRegistryBase`, `MessageScanner`, `MessageVersionAttribute`.

### Changed

- `IUserStateAccessor.GetUser()` renamed to `GetUserState()` for naming honesty (was a queued backlog item from `Cirreum.Core/docs/BACKLOG.md` 2026-05-07).
- `IUserState.Identity` (the dedicated `ClaimsIdentity` property) removed. Consumers should cast `Principal.Identity as ClaimsIdentity` if they need the typed identity.

### Removed (anticipatory delegation surface that didn't pan out)

- 8 delegation attributes from former `Cirreum.Core/Authorization/`: `RequiresDirectCallerAttribute`, `RequiresDelegationAttribute`, `RequiresDelegationActorAttribute`, `RequiresDelegationCheckAttribute`, `RequiresDelegationEvidenceAttribute`, `RequiresDelegationScopeAttribute`, `RequiresAllDelegationScopesAttribute`, `RequiresAnyDelegationScopeAttribute`, `RequiresDelegationWithinAttribute`, `DelegationCheckAttribute`
- `IActorContext`, `ActorContext`, `DelegationMetadata` from former `Cirreum.Core/Security/`
- `IUserState.Actor` and `SetActor` (replaced by `IUserState.Origin` with `IRequestOrigin` typed shape now in `Cirreum.Contracts`)
- All delegation validators (`DelegatedValidator`, `NotDelegatedValidator`, `HasDelegation*Validator` set)
- `DelegationConstraint`, `DelegationLogContext`

These were anticipatory for the InProcess delegation pattern, which was dropped during the architectural pressure-test.

### Migration

Apps consuming `Cirreum.Core 5.x` migrate to `Cirreum.Kernel 1.0.0` + the companion packages (`Cirreum.Contracts`, `Cirreum.Domain`, `Cirreum.Services.{Host}`, etc.).
