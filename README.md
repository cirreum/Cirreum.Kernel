# Cirreum.Kernel

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Kernel.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Kernel/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Kernel.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Kernel/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Kernel?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Kernel/releases)
[![License](https://img.shields.io/badge/license-MIT-F2F2F2?style=flat-square&labelColor=1F1F1F)](https://github.com/cirreum/Cirreum.Kernel/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Cirreum's foundational abstractions — cross-host primitives every Cirreum app builds on.**

## Overview

**Cirreum.Kernel** is the foundational base of the Cirreum framework. It contains the cross-host abstractions, contracts, value types, and sentinels that every Cirreum package consumes — directly or transitively — regardless of host (Server, WebAssembly, Serverless).

Kernel is dependency-light and deliberately small. It defines:

- **Identity & security primitives** — `IUserState`, `IUserStateAccessor`, `IUserSession`, `UserStateBase`, `IApplicationUser`, `IApplicationUserResolver`, `IOwnedApplicationUser`, `AnonymousUser`, `AuthenticationBoundary`, `ClaimsHelper`
- **User profile model** — `UserProfile`, `UserProfileAddress`, `UserProfileMembership`, `UserProfileOrganization`, `IUserProfileEnricher`
- **Authentication events & keys** — `AuthenticationContextKeys` and the `IAuthenticationEvent` family (`IAuthenticationEventPublisher`, `IAuthenticationEventHandler`, plus the `CredentialRevoked` / `SessionTerminationRequested` / `UserAccountDisabled` / `GrantsInvalidated` records)
- **Conductor markers** — `INotification`, `INotificationHandler` (the Result-free notification primitives; the rest of the Conductor surface lives in `Cirreum.Contracts`)
- **Message registry** — `IMessageRegistry`, `MessageDefinition`, `MessageProperty`, `MessageVersionAttribute`, `MessageRegistryBase`, `MessageScanner`
- **Framework bootstrap** — `IDomainApplicationBuilder`, `DomainContext`, `DomainServicesBuilder`, `AssemblyScanner`, `IDomainContextInitializer`, `DomainRuntimeType`, `DomainFeatureResolver`, `IDomainObject`
- **Environment, time & enums** — `IDomainEnvironment`, `IDateTimeClock`, `Timing`, `IdentityProviderType`
- **State foundation** — `IApplicationState` (the marker interface other state contracts extend)
- **Health, diagnostics & utilities** — `IStartedStatus`, `CirreumTelemetry`, `InternetDomainValidator`, `MissingResource`, plus extension methods and SmartFormat command sources

Every other Cirreum package builds on Kernel.

## Where it fits

Kernel is **L1 — the dependency-free floor** of the Cirreum framework. It references no other Cirreum package — not even its foundation peers `Cirreum.Result` and `Cirreum.Exceptions` (consumers pull those as needed). Everything else — the contract surface, the default implementations, the host infrastructure, and the runtime — builds on Kernel, directly or transitively. That zero-dependency floor is the point: Kernel can be consumed by anyone, anywhere, without dragging in the rest of the framework.

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful — Kernel sits at the foundation; changes ripple through every Cirreum package.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies. Kernel must remain dependency-light to qualify as the framework's base.

3. **Favor additive, non-breaking changes**  
   Breaking changes in Kernel cascade through every dependent package and every Cirreum app. Major version bumps are rare.

4. **Include thorough unit tests**  
   All primitives should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from `Microsoft.Extensions.*` libraries.

## Versioning

Cirreum.Kernel follows [Semantic Versioning](https://semver.org/):

- **Major** — Breaking API changes
- **Minor** — New features, backward compatible
- **Patch** — Bug fixes, backward compatible

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*
