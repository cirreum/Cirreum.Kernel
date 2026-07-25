# Cirreum.Kernel Changelog

All notable changes to **Cirreum.Kernel** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

### Added

- `UserProfile.Issuer` — the `iss` claim, verbatim. `Provider` is a best-effort classification
  computed from a table built into this assembly, so two independently deployed sides of an
  application (a WebAssembly client and the API it calls) classify the same token separately and
  can disagree while on different package versions. The issuer comes from the token, so it reads
  identically everywhere. Match on it for anything that must agree across that boundary, for
  disambiguating several identity providers, or for an issuer `IdentityProviderType` does not
  name — `Unknown` is an ordinary answer for a valid token, and `Issuer` still identifies it
  exactly. Neither value is an authorization signal.

- `ClaimsHelper.ResolveIssuer(ClaimsPrincipal)` / `(ClaimsIdentity)`, and
  `ClaimsHelper.ResolveProvider(string?)` — classifies a raw issuer without rebuilding a principal
  around it, the natural pairing with `UserProfile.Issuer`.

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

- Issuer-to-provider classification matched on unanchored substrings, so a host merely *containing*
  a known domain was accepted as that provider — `https://github.com.example.invalid/` classified
  as GitHub. Matching is now anchored to a label boundary: a host is a provider only when it is
  that domain or a subdomain of it.

- Text after `?` or `#` in an issuer is no longer searched for provider markers. The path carries
  the discriminators for Keycloak and legacy B2C, so a query or fragment left in place let
  attacker-chosen text impersonate one — `https://unrelated.example/?redirect=/realms/demo`
  classified as Keycloak.

- `ResolveId`, `ResolveProvider`, and `ResolveIssuer` now resolve from the principal's primary
  identity, matching `ResolveOid` / `ResolveTid` / `ResolveName`. `ClaimsPrincipal.FindFirst`
  searches every identity in order, so on a multi-scheme principal the issuer — or an anonymity
  marker — could be answered by a secondary identity. `ResolveId` was the worst case: it walks
  claim *types* in priority order, so a secondary identity's `oid` outranked the primary's `sub`
  and returned an identifier for a different subject than the name, tenant, and issuer resolved
  alongside it — a coherent-looking profile assembled from two subjects. Every singular-fact
  resolver taking a principal now reads its primary identity or returns `null`; none can reach
  across identities at all, rather than being guarded against doing so.

- Several providers were misclassified or unrecognized:
  - **Entra v1.0 tokens** (`sts.windows.net`) resolved to `Unknown`. Also added the
    `login.windows.net` / `login.microsoft.com` aliases.
  - **Azure AD B2C** was unrecognized despite `EntraExt` documenting it: `b2clogin.com` was absent,
    and the legacy form that shares Entra's host is now told apart by its `/tfp/` policy segment
    instead of being filed under `Entra`.
  - **Keycloak** matched only `/auth/realms/`. Keycloak 17 dropped the `/auth` prefix with the
    Quarkus distribution, so nothing released since 2022 was recognized. Now matches `/realms/`,
    which still covers the legacy path.
  - **AWS Cognito** matched bare `amazonaws.com`, claiming every identity provider that happens to
    be hosted on AWS. Now requires both the `cognito-idp.` subdomain and an `amazonaws.com` suffix
    on a label boundary — either half alone is impersonable.
  - Added `okta-emea.com`, `pingone.com`, and `x.com`. Removed `auth.keycloak.org` (a marketing
    site, never an issuer) and the `graph.facebook.com` / `api.twitter.com` entries already covered
    by their parent domains.

- `ClaimsHelper` no longer returns a blank claim value from `ResolveName`, `ResolveOid`, or

- `ClaimsHelper` no longer returns a blank claim value from `ResolveName`, `ResolveOid`, or
  `ResolveTid`. Each guarded its resolution rungs correctly and then returned the last assigned
  value regardless, so a present-but-whitespace claim escaped as a non-null string. That defeats
  every caller's fallback — `UserProfile.Name`, `.Oid`, and `Organization.OrganizationId` are all
  assigned from a `?? default` over these results, which cannot fire against a non-null value. A
  whitespace name reached logs and audit records verbatim; a whitespace tenant id reached the
  value that draws the multi-tenant boundary. All six overloads now return `null` when nothing
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
  meter with no listener attached is inert â€” it records into the void â€” so the provisioning
  span and the `cirreum.identity.provision.duration` / `.count` / `.claims` instruments reached
  no exporter regardless of traffic. `AddCirreum()` now registers the name, so an application
  already calling it collects provisioning telemetry with no further configuration.

## [1.2.0] - 2026-07-20

### Added

- `IAuthenticationBoundaryResolver` and `DefaultAuthenticationBoundaryResolver`
  (namespace `Cirreum.Security`) â€” the authentication-boundary resolution seam,
  relocated from `Cirreum.AuthenticationProvider` to sit beside the
  `AuthenticationBoundary` enum, `IUserState`, and `UserStateBase` it operates on
  (ADR-0032). The seam is spine infrastructure: the server user-state pipeline
  resolves it per invocation and grant providers consume the stamped boundary,
  independent of which (or whether any) authentication track is composed. The
  default is now public so consuming packages can `TryAdd`-register it directly.

## [1.1.0] - 2026-07-07

### Added

- **ADR-0029 â€” type capture on the versioned-message scan.** `MessageScanner<TBase>.Discover(...)` is the scan surface: it returns each discovery as a `MessageDiscovery` â€” the live CLR `Type` paired with its scanned `MessageDefinition`. The `Type` deliberately does not land on `MessageDefinition` (a serializable schema DTO whose `MessageType` member already means the FullName string); the pairing record keeps the DTO clean and the capture explicit.
- `MessageRegistryBase<TBase>.OnMessageDiscovered(MessageDiscovery)` â€” a per-discovery hook, called after the base lookup maps contain the entry, so registry subclasses capture family-specific per-type metadata (e.g. routing attributes) from the single scan instead of hand-rolling a second one. Both existing subclasses (`DistributedMessageRegistry`, `AuthenticationEventRegistry`) shed their private re-scans on their next releases.
- The scanner now **warns at scan time for a concrete `TBase` subtype carrying no `[MessageVersion]` attribute** â€” such a type is publishable and locally handleable but invisible to the registry, a permanent configuration error better surfaced at startup than at first publish. Previously only the auth-events registry warned, from its private second scan; the diagnostic is now family policy for every message channel.
- The repo's first test suite (`tests/Cirreum.Kernel.Tests.slnx`): the discovery surface, both registry lookup directions, hook invocation ordering, the unversioned and duplicate-identity warnings.

### Changed

- `IMessageRegistry<TBase>` gains identity-based inbound resolution: `Type? ResolveType(string identifier, string version)` and `Type? ResolveType(MessageDefinition)`. Nullable-returning by design, in deliberate contrast with the throwing outbound `GetDefinitionFor` family â€” an inbound identity miss is a normal operating condition (version skew during rolling upgrade; fan-out family members this consumer doesn't handle), not an error. `MessageRegistryBase` implements both from a `(identifier, version)` â†’ `Type` map populated by the same single scan. There is deliberately no `ResolveType(string typeName)` overload â€” a CLR type name stops being a resolution input anywhere in the message track (ADR-0029). Interface member addition shipped as a minor per ADR-0029's prerelease convention: nothing outside the framework implements `IMessageRegistry<TBase>` directly.
- `MessageScanner<TBase>.ScanAssemblies(...)` is replaced by `Discover(...)` â€” after the registry moved to the discovery surface, the definitions-only method had zero callers, and a projection is one `Select` away at any future call site. A member removal shipped as a minor per ADR-0029's prerelease convention (nothing outside `MessageRegistryBase` ever called it); any external caller fails loudly at compile time pointing at the replacement.
- `IMessageRegistry<TBase>.GetDefinitionFor(string messageTypeFullName)` is removed on the same grounds â€” the only caller was the base class's own `Type` overload, for which the FullName string is now a private index. With it gone, a CLR type name is not a resolution input anywhere on the registry surface: `GetDefinitionFor<T>`/`(Type)` outbound, `ResolveType(identifier, version)`/`(MessageDefinition)` inbound.
- The four framework authentication events now take their `[MessageVersion]` identifiers from the internal `EventMessages` constants â€” one authoritative definition per wire identity. No wire change; the identifier strings are identical.
- `MessageRegistryBase<TBase>` converted to a primary constructor.

## [1.0.3] - 2026-07-06

### Fixed

- `AuthenticationContextKeys` doc truth-pass for the ADR-0025/0027 wave: `ApplicationUserCache` is connection-scoped as well as request-scoped (the per-invocation contexts seed from the connection's auth slots) and is evicted by Two-Phase Auth promotion *before* `PromotedPrincipal` is stamped; `PromotedPrincipal` is written by the `connection.Promote(principal)` extension (the old `TwoPhaseAuth.Promote` static form is gone) and read through the `Cirreum.Contracts` connection-ownership surface (`PromotedUser` / `EffectiveUser` / `IsUserPromoted`) rather than directly by `UserStateAccessor`. Doc-only.

## [1.0.2] - 2026-07-04

### Fixed

- Consolidated each authentication event's differently-named timestamp (`CredentialRevoked.RevokedAt`, `UserAccountDisabled.DisabledAt`, `SessionTerminationRequested.RequestedAt`, `GrantsInvalidated.InvalidatedAt`) into one common, required `IAuthenticationEvent.OccurredAt` property, and added `CredentialRevoked.ExpiresAt` plus the `IAuthenticationEventTransportBridge` marker needed for the auth-event bus's in-process publisher and cross-replica delivery. No known consumers reference the old per-event timestamp names or depend on the auth-event bus today.

## [1.0.1] - 2026-06-04

### Fixed

- Documentation and XML doc-comments now reference the renamed foundation packages â€” `Cirreum.Contracts` (formerly `Cirreum.Common`) and `Cirreum.Domain` (formerly `Cirreum.Shared`). The README "Where it fits" section no longer enumerates upper-layer packages â€” a dependency-free floor cannot keep a consumer list current â€” and instead states Kernel's layer position and zero-dependency nature.

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
- **Authentication primitives** folded in from the dissolved `Cirreum.Authentication` package: `AuthenticationContextKeys` and the authentication event surface â€” `IAuthenticationEvent`, `IAuthenticationEventPublisher`, `IAuthenticationEventHandler`, and the `CredentialRevoked` / `SessionTerminationRequested` / `UserAccountDisabled` / `GrantsInvalidated` event records.
- **Security primitives**: `AuthenticationBoundary`, `ClaimsHelper` (alongside `AnonymousUser` above).
- **Conductor notification markers**: `INotification`, `INotificationHandler` â€” the Result-free notification primitives. The rest of the Conductor surface (`IDispatcher`, `IOperation`, `OperationContext`, intercept contracts, etc.) lives in `Cirreum.Contracts`.
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
