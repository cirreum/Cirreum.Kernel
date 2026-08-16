# Cirreum.Kernel 2.1.0 — who owns a caller's attributes, declared not inferred

## Why this release exists

An authorization audit line in a production app read:

```
User 'Contoso.SubscriberPortal' was ALLOWED access to 'GetAuthenticatedAnnouncements'
```

The "user" is the calling application. The caller was a signed-in person, on a token that
carried their name — under a provisioned `customName` claim the server never aliased, because
claim canonicalization existed only in the WebAssembly client. With no native `name` claim, a
server-side fallback concluded "no human here" and wrote an unauthenticated
`X-Cirreum-App-Name` header value in as the identity's name.

That single line exposed one defect with three faces, all the same mistake: **the framework was
inferring who a caller is, and who owns their attributes, from what their token happened to
contain.** A token is thin because the application is the authority for that scheme, not because
the caller is a machine — and every presence-check read it the other way.

This release ships the contracts that let those questions be *declared* instead. It is the
foundation rung; the behavior changes arrive in the packages above it.

## What's new

**Two enums and a value that carry the declaration.** `SubjectKind` records whether a scheme
authenticates a person or a machine — declared by the provider that registers the scheme, since
a provider knows what it authenticates. `ClaimAuthority` records which side owns a class of
attributes. `SchemeClaimAuthority` is a scheme's resolved declaration, and
`ISchemeClaimAuthorityMap` looks it up:

```csharp
var declared = services.GetService<ISchemeClaimAuthorityMap>()?.Get(scheme)
    ?? SchemeClaimAuthority.Undeclared;
```

Both enums put "not stated" at zero — `SubjectKind.Unknown`, `ClaimAuthority.Unspecified` — so a
scheme that declares nothing asserts nothing, and existing behavior is preserved by default.

**`IUserState.SubjectKind`,** exposed where operation authorizers already read user state, with
two convenience predicates as extension members:

```csharp
if (!userState.IsHumanSubject) {
    return Result.Fail(new ForbiddenAccessException("This operation requires a person."));
}
```

Both predicates answer *known to be*, so each is `false` for `Unknown`. With three states and
two booleans they are deliberately not inverses: `!IsHumanSubject` denies an unclassified
caller, while `IsMachineSubject` admits one. Guard with the former; use the latter only to add
machine-specific behavior. The property ships as a default interface implementation — the same
delivery `AuthenticationBoundary` already uses — so nothing implementing `IUserState` breaks,
and `UserStateBase` gains a `protected` setter for concrete user-state types to stamp.

Unlike `AuthenticationBoundary`, there is no companion "is resolved" flag. `None` is ambiguous
there because a resolver may legitimately return it; `Unknown` is never a resolved answer, so
the value speaks for itself.

**`CustomClaimCanonicalizer`,** which aliases provisioned `custom*` token claims to their native
names — `customRoles` → `roles`, `customName` → `name`, `customTenant` → `tenant` — splitting
JSON-array values into individual claims so `IsInRole` resolves. Additive, idempotent, and inert
when a token carries no `custom*` claims.

**`AuthenticationContextKeys.PromotedSubjectKind`,** the connection slot a Two-Phase Auth
promotion stamps alongside `PromotedPrincipal`. `AuthenticatedScheme` deliberately survives
promotion because it describes how the *connection* was authenticated; a subject kind derived
from the scheme would therefore keep describing the transport after a person has taken
occupancy. This slot lets a promoted connection report its occupant instead.

## Why the canonicalizer lives here

It previously lived in `Cirreum.Runtime.Wasm`, internal to the browser client's principal
factory. That placement was the bug: the same signed token reached the API with its `custom*`
claims un-aliased, so a server-side principal never saw an application-minted attribute under
the name the framework reads.

It moved to Kernel rather than to the provisioning track that is its most common producer,
because the `custom*` namespace is not that track's property. The prefix exists to be
collision-safe against any identity provider's native claim names — general claims hygiene — and
a hand-authored identity-provider flow emits the same wire with no Cirreum provisioning composed
at all. It is a convention both runtimes must agree on to interoperate, which makes it a floor
concern. Kernel takes no new dependency to host it.

## Coordinated downstream work

Nothing in this release reads these contracts yet. The packages above it will, in dependency
order: the provider registrar base gains an abstract `SubjectKind` and contributes one
registration per registered scheme; the server's user-state accessor resolves subject kind ahead
of its claims enrichment and stops overwriting token-supplied claims; the authentication
runtime's claims transformation runs canonicalization and reads the declaration instead of
guessing from a roles claim; and the WebAssembly client re-points to the relocated canonicalizer
and deletes its own copy.

Until that last step, the canonicalizer exists in both packages. They are the same
implementation and the same test suite; the client's copy is `internal` and unreachable.

## Compatibility

- **Purely additive, and behavior-neutral on its own.** Upgrading to 2.1.0 changes nothing an
  application observes. Every new type is unread until a higher-layer package consumes it, and
  both enums default to "not stated".
- **No `IUserState` implementer breaks.** `SubjectKind` is a default interface implementation,
  the pattern `AuthenticationBoundary` established. The two predicates are extension members and
  are not part of the contract at all, so they cannot be — or need to be — implemented.
- **No new dependencies.** Kernel remains the dependency-free floor.

## See also

- `Cirreum.Runtime.Wasm` — currently owns the canonicalizer copy that this release supersedes;
  re-points and deletes it on its own release.
- `Cirreum.IdentityProvider` — mints the `custom*` claims this canonicalizes, though it is not
  the only producer of that wire.
