# Cirreum.Kernel Changelog

All notable changes to **Cirreum.Kernel** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

### Fixed

- **`IOwnedApplicationUser` documentation no longer claims the framework reads anything from
  this interface.** `2.0.1` corrected the summary's account of `OwnerId` but kept a
  "disabled-user backstop" clause describing the grant evaluator reading `IsEnabled` *from
  `IOwnedApplicationUser`* — a justification retrofitted onto a type test that outlived its
  premise. `IsEnabled` is declared on `IApplicationUser`, and the check now reads it from any
  application user, owned or not. Both members are rewritten around what `OwnerId` is —
  ownership context an app may use for display, UI defaults, assigning ownership to new
  records, or as a lookup key when resolving grant records — with the boundary stated once:
  its presence, absence, or value grants nothing by itself. Documentation only; no behavior or
  surface change.

## [2.0.1] - 2026-07-31

### Fixed

- **`IOwnedApplicationUser` documentation no longer implies `OwnerId` confers access.** The
  summary described the grant evaluator resolving the caller's tenant from the app user — the
  since-removed implicit home-owner merge. `OwnerId` is an identity fact (the caller's home
  company, e.g. a query key for the app's grant provider); owner-scoped access comes exclusively
  from grant records, and the framework itself reads only `IsEnabled` (the disabled-user
  backstop) from this interface. Documentation only; no behavior or surface change.

## [2.0.0] - 2026-07-26

### Changed

- **`DomainContext.CurrentActivityKind` → `DomainContext.EntryPointActivityKind`.** "Current" read
  as ambient or per-span, which is the exact misreading that leads to using it for an outbound
  call. It is neither: it is a host-level constant resolved once at initialization, and it is
  correct only for the span where work *enters* this host.

  `ActivityKind` describes a span's role in a trace, not the process emitting it — one host emits
  `Server` for the request it handles, `Client` for the call it makes downstream, and `Producer`
  for the message it publishes, all in the same request. Using a host-derived kind for an outbound
  call would mark it `Server` on a server host: a span claiming to receive a request it is actually
  making, which draws the wrong graph in any backend. The new name puts the usage rule at every
  call site, and the property's remarks now spell out when to reach for it and when to state the
  intrinsic kind instead.

- **`INotification` → `IDomainEvent`, and `INotificationHandler<T>` → `IDomainEventHandler<T>`.**
  Cirreum used "notification" for two unrelated things: Conductor's in-application publish/subscribe
  primitives, and the human-facing state family a client binds to in order to show a person
  something (`INotificationState`, `IScopedNotificationState`, and the WebAssembly state services
  built on them). They travel in opposite directions and have unrelated lifetimes, so sharing the
  word left a handler's audience ambiguous at a glance — and made "notification handler" mean either
  "reacts to something that happened" or "renders something for a user" depending on which package
  you were in.

  `IDomainEvent` names what it is: one part of the system telling the rest that something happened.
  "Notification" now refers only to the human-facing concept. The `HandleAsync` parameter is renamed
  `notification` → `domainEvent` to match. Behavior, dispatch semantics, and fan-out are unchanged —
  this is a rename. See `MIGRATION-v2.md`.

### Removed

- **`IdentityProviderType`, and everything that produced or exposed it.** The enum documented
  itself as identifying "which identity provider is *configured* for authentication", but the
  implementation inferred it on every `UserProfile` construction by matching the `iss` claim
  against a built-in table of vendor domains — a fact declared at composition time, re-derived by
  substring guesswork on every authorized request and again in the browser.

  A full trace found no framework consumer: every `.Provider` past `UserProfile` was a
  pass-through getter, `IsFromProvider` had zero call sites, Blazor component usage was one
  commented-out line in a non-compiling demo, and the `idp_type` claim stamped by
  `Cirreum.Authentication.External` was never read by anything. The questions it appeared to
  answer already had better answers: `AuthenticationContextKeys.AuthenticatedScheme` for which
  authentication context produced an identity (configuration-tied, propagated across HTTP,
  SignalR, and WebSocket connections, and treated as reserved with anti-spoofing coverage), the
  new `UserProfile.Issuer` for what the token actually asserts, and the application's own
  composition for which provider it uses.

  Removed: `IdentityProviderType`; `UserProfile.Provider`; `IUserState.Provider` and
  `UserStateBase.Provider`; `ClaimsHelper.ResolveProvider` (all overloads). `OperationContext` and
  `AuthorizationContext` drop `Provider` / `IsFromProvider` in `Cirreum.Contracts`. See
  `MIGRATION-v2.md`.

### Added

- `UserProfile.Issuer` — the `iss` claim, verbatim. The one identity-provider signal that cannot
  drift: it comes from the token, so a WebAssembly client and the API it calls cannot disagree
  about it. Match on it when an application needs to distinguish the identity providers it
  accepts. Not an authorization signal — it records what a token asserts, not that the assertion
  was validated.

- `ClaimsHelper.ResolveIssuer(ClaimsPrincipal)` / `(ClaimsIdentity)` — reads that claim without
  rebuilding a principal around it.

- `IdentityScope`, and an optional `scope` parameter on `ClaimsHelper.ResolveRoles(ClaimsPrincipal)`
  — read every identity the principal carries (the default, and the breadth
  `ClaimsPrincipal.IsInRole` spans) or only the identity it presents. Role resolution behavior is
  unchanged; the axis is now expressible at the call site. The parameter exists on roles alone,
  because roles aggregate: reading a singular fact such as an id or issuer across identities is
  not a broader answer but a wrong one, so those resolvers do not offer the choice.

  Note that role resolution is deliberately *broader* than `IsInRole` — this is a universal helper
  that cannot know how a principal was composed, so `role` and `roles` are recognized even when an
  identity's `RoleClaimType` is something else, and it can report a role `IsInRole` denies. It
  describes what a token carries, for display and diagnostics — it is not an authorization
  primitive.

### Fixed

- `ClaimsHelper` no longer returns a blank claim value from `ResolveName`, `ResolveOid`, or
  `ResolveTid`. Each guarded its resolution rungs correctly and then returned the last assigned
  value regardless, so a present-but-whitespace claim escaped as a non-null string. That defeats
  every caller's fallback — `UserProfile.Name`, `.Oid`, and `Organization.OrganizationId` are all
  assigned from a `?? default` over these results, which cannot fire against a non-null value. A
  whitespace name reached logs and audit records verbatim; a whitespace tenant id reached the
  value that draws the multi-tenant boundary. All overloads now return `null` when nothing
  resolves.

- `ClaimsHelper.ResolveId` no longer lets a blank claim shadow a populated one. It short-circuited
  on the first *non-null* claim rather than the first non-blank, so a blank `oid` suppressed a
  valid `sub` and became the resolved user identifier. `ResolveOid`, `ResolveTid`, and
  `ResolveName` now apply the same rule: a blank claim is treated as absent, not as an answer.

- `ClaimsHelper.ResolveRoles` no longer admits blank role claims. An empty string in the resolved
  set reads as a granted role to anything enumerating them, and matches an equally empty policy
  requirement.

- `ClaimsHelper.ResolveName`'s final rung now resolves `ClaimsIdentity.DefaultNameClaimType`
  instead of `identity.NameClaimType`. The configured type is already resolved one rung earlier by
  `Identity.Name`, so the last rung could never contribute an answer; the classic `ClaimTypes.Name`
  URI it now checks catches principals minted outside Cirreum's own composition — WS-Fed, cookie
  authentication, or a handler that left inbound claim mapping enabled.

- `ResolveId`, `ResolveName`, `ResolveOid`, `ResolveTid`, and `ResolveIssuer` now resolve from the
  principal's primary identity or not at all. `ClaimsPrincipal.FindFirst` searches every identity
  in order, so on a multi-scheme principal a singular fact could be answered by a secondary
  identity. `ResolveId` was the worst case: it walks claim *types* in priority order, so a
  secondary identity's `oid` outranked the primary's `sub` and returned an identifier for a
  different subject than the name, tenant, and issuer resolved alongside it — a coherent-looking
  profile assembled from two subjects. There is no principal-wide claim search left to guard
  against; roles keep their identity walk, now explicit through `IdentityScope`.

## [1.3.0] - 2026-07-24

### Added

- `CirreumTelemetry.ActivitySources.IdentityProvisioning` and
  `CirreumTelemetry.Meters.IdentityProvisioning` (`Cirreum.Identity.Provisioning`), now
  registered by `AddCirreum()` alongside the Conductor, remote-services, authentication, and
  authorization names.

### Fixed

- **Identity provisioning telemetry was unobservable.** `Cirreum.IdentityProvider` ships an
  `ActivitySource` and `Meter` named `Cirreum.Identity.Provisioning`, and both provider adapters
  emit through it, but no package ever registered that name with OpenTelemetry. A source or
  meter with no listener attached is inert — it records into the void — so the provisioning
  span and the `cirreum.identity.provision.duration` / `.count` / `.claims` instruments reached
  no exporter regardless of traffic. `AddCirreum()` now registers the name, so an application
  already calling it collects provisioning telemetry with no further configuration.

## [1.2.0] - 2026-07-20

### Added

- `IAuthenticationBoundaryResolver` and `DefaultAuthenticationBoundaryResolver`
  (namespace `Cirreum.Security`) — the authentication-boundary resolution seam,
  relocated from `Cirreum.AuthenticationProvider` to sit beside the
  `AuthenticationBoundary` enum, `IUserState`, and `UserStateBase` it operates on
  (ADR-0032). The seam is spine infrastructure: the server user-state pipeline
  resolves it per invocation and grant providers consume the stamped boundary,
  independent of which (or whether any) authentication track is composed. The
  default is now public so consuming packages can `TryAdd`-register it directly.

## [1.1.0] - 2026-07-07

### Added

- **ADR-0029 — type capture on the versioned-message scan.** `MessageScanner<TBase>.Discover(...)` is the scan surface: it returns each discovery as a `MessageDiscovery` — the live CLR `Type` paired with its scanned `MessageDefinition`. The `Type` deliberately does not land on `MessageDefinition` (a serializable schema DTO whose `MessageType` member already means the FullName string); the pairing record keeps the DTO clean and the capture explicit.
- `MessageRegistryBase<TBase>.OnMessageDiscovered(MessageDiscovery)` — a per-discovery hook, called after the base lookup maps contain the entry, so registry subclasses capture family-specific per-type metadata (e.g. routing attributes) from the single scan instead of hand-rolling a second one. Both existing subclasses (`DistributedMessageRegistry`, `AuthenticationEventRegistry`) shed their private re-scans on their next releases.
- The scanner now **warns at scan time for a concrete `TBase` subtype carrying no `[MessageVersion]` attribute** — such a type is publishable and locally handleable but invisible to the registry, a permanent configuration error better surfaced at startup than at first publish. Previously only the auth-events registry warned, from its private second scan; the diagnostic is now family policy for every message channel.
- The repo's first test suite (`tests/Cirreum.Kernel.Tests.slnx`): the discovery surface, both registry lookup directions, hook invocation ordering, the unversioned and duplicate-identity warnings.

### Changed

- `IMessageRegistry<TBase>` gains identity-based inbound resolution: `Type? ResolveType(string identifier, string version)` and `Type? ResolveType(MessageDefinition)`. Nullable-returning by design, in deliberate contrast with the throwing outbound `GetDefinitionFor` family — an inbound identity miss is a normal operating condition (version skew during rolling upgrade; fan-out family members this consumer doesn't handle), not an error. `MessageRegistryBase` implements both from a `(identifier, version)` → `Type` map populated by the same single scan. There is deliberately no `ResolveType(string typeName)` overload — a CLR type name stops being a resolution input anywhere in the message track (ADR-0029). Interface member addition shipped as a minor per ADR-0029's prerelease convention: nothing outside the framework implements `IMessageRegistry<TBase>` directly.
- `MessageScanner<TBase>.ScanAssemblies(...)` is replaced by `Discover(...)` — after the registry moved to the discovery surface, the definitions-only method had zero callers, and a projection is one `Select` away at any future call site. A member removal shipped as a minor per ADR-0029's prerelease convention (nothing outside `MessageRegistryBase` ever called it); any external caller fails loudly at compile time pointing at the replacement.
- `IMessageRegistry<TBase>.GetDefinitionFor(string messageTypeFullName)` is removed on the same grounds — the only caller was the base class's own `Type` overload, for which the FullName string is now a private index. With it gone, a CLR type name is not a resolution input anywhere on the registry surface: `GetDefinitionFor<T>`/`(Type)` outbound, `ResolveType(identifier, version)`/`(MessageDefinition)` inbound.
- The four framework authentication events now take their `[MessageVersion]` identifiers from the internal `EventMessages` constants — one authoritative definition per wire identity. No wire change; the identifier strings are identical.
- `MessageRegistryBase<TBase>` converted to a primary constructor.

## [1.0.3] - 2026-07-06

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
