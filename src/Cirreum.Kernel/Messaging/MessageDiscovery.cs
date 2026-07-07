namespace Cirreum.Messaging;

/// <summary>
/// A single scan discovery: the live CLR type paired with its scanned
/// <see cref="MessageDefinition"/>.
/// </summary>
/// <param name="ClrType">The concrete message type as loaded in this process.</param>
/// <param name="Definition">The definition produced from the type's
/// <see cref="MessageVersionAttribute"/> and public property schema.</param>
/// <remarks>
/// Produced by <see cref="MessageScanner{TBase}.Discover"/>. The pairing exists because
/// <see cref="MessageDefinition"/> is a serializable schema DTO — a live
/// <see cref="Type"/> does not belong on it — while registries need the type for
/// identity-based inbound resolution and per-type metadata capture.
/// </remarks>
public sealed record MessageDiscovery(Type ClrType, MessageDefinition Definition);
