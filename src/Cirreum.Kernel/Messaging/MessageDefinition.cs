namespace Cirreum.Messaging;

/// <summary>
/// The discovered metadata for a versioned-message CLR type.
/// </summary>
/// <param name="Identifier">The stable logical identifier from
/// <see cref="MessageVersionAttribute.Identifier"/>.</param>
/// <param name="Version">The schema version from
/// <see cref="MessageVersionAttribute.Version"/>.</param>
/// <param name="MessageType">The fully qualified CLR type name of the message.</param>
/// <param name="Schema">The public property schema captured at scan time. Used for
/// schema introspection and tooling; the runtime serializer is the authority for wire
/// format.</param>
/// <remarks>
/// Produced by <see cref="MessageScanner{TBase}"/> from each concrete subtype of
/// <c>TBase</c> bearing <see cref="MessageVersionAttribute"/>. Retrieved at runtime via
/// <see cref="IMessageRegistry{TBase}"/>.
/// </remarks>
public record MessageDefinition(
	string Identifier,
	string Version,
	string MessageType,
	IReadOnlyList<MessageProperty> Schema);
