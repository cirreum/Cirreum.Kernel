# Cirreum.Kernel 2.2.0 — canonicalization states its posture

## Why this release exists

`CustomClaimCanonicalizer` shipped in 2.1.0 as the relocated client mechanism, verbatim: alias
every `custom*` claim to its native name, `customRoles` included. That is correct for the one
caller that existed — the browser principal factory, where the token is the only source of
roles and role claims gate nothing but rendering.

The server is the mechanism's second consumer, and it must not alias roles. Server role claims
are an authorization surface: `IsInRole` treats presence as grant, and for a scheme whose
application store owns roles, the framework resolves them per request precisely so revocation
is immediate. The token's `customRoles` is a snapshot of that same store, frozen at minting for
the lifetime of the JWT. Aliasing it server-side would materialize the snapshot as live role
claims beside the fresh ones — `union(stale, fresh)` — and a role revoked in the store would
keep answering `true` until token refresh. Profile claims tolerate that ambiguity because
precedence can be applied when they are read; roles cannot, because the read is
presence-based. The decision has to happen at materialization time, so the mechanism has to
know which posture is calling.

## What's new

**`Canonicalize(ClaimsIdentity identity, bool excludeRoles)`** — the posture is a required
parameter:

```csharp
// Client principal construction — the token is the only source; alias everything.
CustomClaimCanonicalizer.Canonicalize(identity, excludeRoles: false);

// Server claims transformation — role claims come from the scheme's authority, per request.
CustomClaimCanonicalizer.Canonicalize(identity, excludeRoles: true);
```

Required rather than defaulted, deliberately: a default silently supplies the answer, and the
wrong default is invisible — a client that stopped aliasing roles would simply render as if
the user had none. Every call site states its posture, checked at compile time.

Exclusion keys on the wire name (`customRoles`), not the identity's configured
`RoleClaimType` — the wire contract belongs to the mint, independent of provider claim-type
configuration. Everything else is unchanged: additive, idempotent, array-splitting, provenance
preserved, and the excluded wire claim itself survives untouched — it is simply never
materialized as an evaluable role claim.

## Compatibility

The signature change is breaking on paper and shipped in a Minor deliberately: the Kernel
member's only in-framework caller is the WebAssembly runtime's still-local copy (re-pointed to
this member in its own upcoming release), so there are no callers to break. Early adopters:
`Canonicalize(identity)` → `Canonicalize(identity, excludeRoles: false)` preserves prior
behavior exactly.

## See also

- `Cirreum.Runtime.AuthenticationProvider` (upcoming) — the server consumer: canonicalization
  at claims transformation with `excludeRoles: true`, alongside the declaration-driven roles
  stage.
- `Cirreum.Runtime.Wasm` (upcoming) — re-points its local canonicalizer to this member with
  `excludeRoles: false` and deletes the copy.
- `Cirreum.Contracts 4.4.0` — `OriginScheme` / `EffectiveScheme`, the schemes whose
  declarations decide the server's roles source.
