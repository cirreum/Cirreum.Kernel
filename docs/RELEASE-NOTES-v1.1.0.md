# Cirreum.Kernel 1.1.0 — the versioned-message registry resolves both directions

## Why this release exists

The Kernel's versioned-message machinery (`IMessageRegistry<TBase>` /
`MessageRegistryBase<TBase>` / `MessageScanner<TBase>` / `[MessageVersion]`) is the
template every Cirreum message channel builds on — the distributed-messaging channel and
the auth-events channel today. Its scan had a structural gap: the scanner held the live
CLR `Type` at discovery time and threw it away, capturing only the type's FullName
string into the definition. The registry could therefore answer only one direction —
CLR type → wire identity — and anything type-level beyond that forced a consumer to
scan again.

Both existing registry subclasses were paying for it. `DistributedMessageRegistry`
re-scanned every assembly to read routing attributes; `AuthenticationEventRegistry`
re-scanned to build an inbound identity → `Type` map. Different needs, identical
duplicated mechanics — and the underlying trap (`Type.GetType` over a stored FullName
resolves nothing outside the calling assembly) had already shipped one real routing bug.
A third message family would have copy-pasted the workaround again.

## What's new

**The scan yields discoveries.** `MessageScanner<TBase>.Discover(logger)` returns each
discovery as a `MessageDiscovery` — the live CLR `Type` paired with its scanned
`MessageDefinition`. The definition record itself is untouched: it stays a serializable
schema DTO; the pairing record carries the type capture explicitly.

**The registry resolves both directions.** `IMessageRegistry<TBase>` now ends up
symmetric and free of type-name keys:

```csharp
MessageDefinition GetDefinitionFor<T>();               // outbound — yours; miss throws
MessageDefinition GetDefinitionFor(Type messageType);
Type? ResolveType(string identifier, string version);  // inbound — foreign; miss is null
Type? ResolveType(MessageDefinition definition);
IReadOnlyCollection<MessageDefinition> GetAll();       // the inspection seam
```

The verb split is deliberate and is now the track's vocabulary: `Get` means *this is
your own type — a miss is your configuration error, so it throws*; `Resolve` means
*turn a wire identity into the type it denotes in this process* — and like any
resolution, the answer is process-relative, so a miss is a normal operating condition
(version skew during a rolling upgrade; fan-out family members this consumer doesn't
handle) and returns `null` for the caller to disposition.

**Subclasses extend the scan through a hook, never a second scan.**

```csharp
protected override void OnMessageDiscovered(MessageDiscovery discovery) {
	// called once per discovery, after the base maps contain the entry —
	// read routing/severity/etc. attributes off discovery.ClrType here
}
```

**Unversioned family members are surfaced at startup.** A concrete `TBase` subtype
without `[MessageVersion]` is publishable and locally handleable but invisible to the
registry — it can never cross a transport. The scanner now warns about it at scan time,
where the fix is obvious, instead of leaving it to fail at first publish.

**Housekeeping.** The four framework authentication events take their identifiers from
one set of internal constants (identifier strings unchanged), and the repo gains its
first test suite (23 tests over the discovery surface, both lookup directions, hook
ordering, and the new diagnostics).

## What was removed, and why this is still a minor

Two members with zero callers outside this package were deleted rather than kept as
dormant surface: `MessageScanner<TBase>.ScanAssemblies(...)` (a definitions-only view —
one `Select` away from `Discover` at any future call site) and
`IMessageRegistry<TBase>.GetDefinitionFor(string messageTypeFullName)` (the FullName is
now a private index; a CLR type name is no longer a resolution input anywhere on the
registry surface). Every published consumer of this package uses the generic or
`Type`-based lookups, which are unchanged. The track is prerelease-in-practice — its
umbrellas have no public adoption yet — so the removals ship in a minor by explicit
decision rather than forcing a major for members nobody calls. An out-of-tree caller of
either member (none is known) fails loudly at compile time, pointed at the replacement.

## Coordinated downstream work

- `Cirreum.Messaging.Distributed` (minor, upcoming): `DistributedMessageRegistry` sheds
  its second scan via the hook; the envelope stops resolving CLR type names and gains a
  pluggable payload serializer.
- `Cirreum.Runtime.Messaging` (minor, upcoming): the receiver resolves inbound types by
  identity against the registry's vetted set, with per-source failure disposition.
- `Cirreum.Runtime.Authentication` 1.1.0 (upcoming): `AuthenticationEventRegistry`
  reduces to the base class — its identity map, resolver, and scan predicate all came
  from the workaround this release deletes.

## Compatibility

Additive for every real consumer. Registries subclassing `MessageRegistryBase` inherit
the new surface; a custom `IMessageRegistry<TBase>` implementation that bypassed the
base class (none known) must add the two `ResolveType` members and drop the string
`GetDefinitionFor` overload. Apps carrying unversioned concrete message types will see
new startup warnings — each one is a latent cannot-cross-transport misconfiguration
surfacing at the only time it is cheap to fix.

## See also

- ADR-0029 — Message Track: Type Capture and Identity-Based Inbound Resolution
- `docs/CHANGELOG.md` — the enumerated change list
