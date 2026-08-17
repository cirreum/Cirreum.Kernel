# Cirreum.Kernel 2.1.1 — a continuation carries its origin, not a verdict

## Why this release exists

2.1.0 shipped `AuthenticationContextKeys.PromotedSubjectKind`: when a Two-Phase Auth connection
promoted mid-flight, the promotion would stamp the occupant's *subject kind* so downstream
consumers stopped describing the transport. Before anything consumed it, the continuation design
pass concluded the slot carried the wrong thing.

Consider the flow promotion exists for: an anonymous browser chat answers public questions, the
user signs in mid-session to reach private data, the connection promotes. Carrying only the
subject kind means the framework then knows a human is present — but resolves **no claim
authority**, so that user's roles fall back to defaults rather than the declaration their scheme
made, at the exact moment authority matters most.

And promotion is not alone. A session ticket re-presents a subject another scheme established; a
delegated token will too. All three are *continuations*, and what a continuation should carry is
not a derived verdict but a reference: the **origin scheme's name**, from which both subject kind
and claim authority re-resolve on every use — so a configuration change reaches live connections
and unexpired tickets instead of being frozen into them.

## What's new

**`AuthenticationContextKeys.OriginScheme`** — the authentication scheme that established the
subject a continuation re-presents:

```csharp
// Stamped by Two-Phase Auth promotion:
connection.Promote(principal, originScheme: "descope");

// And by continuation scheme handlers whose validated credential carries its origin
// (session tickets), into the request items.
```

User-state accessors resolve the origin scheme's declaration in place of the continuation's own.
Absent the slot, the authenticated scheme established the subject itself — the ordinary case.

**`AuthenticationContextKeys.PromotedSubjectKind` is removed**, replaced by the slot above. One
slot now serves session tickets, promotion, and future delegation alike.

## Why this ships as a patch

Removing a public constant is breaking by the letter of SemVer. It ships as a patch deliberately:
2.1.0 released on 2026-08-16, nothing consumes the removed constant — verified framework-wide —
and the framework does not stage dormant surface it knows is the wrong shape. This is a
post-release, pre-adoption correction, released with the escape hatch that exists for exactly
that case.

## Compatibility

- **Breaking on paper, a no-op in practice.** The removed constant had zero consumers; the added
  constant is unread until the packages above stamp and resolve it.
- **No other changes.** No dependencies, no behavior, no other surface.

## See also

- `Cirreum.AuthenticationProvider 3.0.1` — the `SessionTicket` contracts gain the `Scheme` field
  an origin is carried in.
- `Cirreum.Runtime.AuthenticationProvider` — ships the `Promote(principal, originScheme)`
  overload that stamps this slot.
- `Cirreum.Authentication.SessionTicket` — its validator stamps the slot from the ticket's
  origin.
