namespace Cirreum.Messaging;

/// <summary>
/// Stamps a stable identifier and schema version on a versioned-message type.
/// </summary>
/// <param name="identifier">The stable logical identifier for the message type — preserved
/// across schema versions so consumers can correlate "the same conceptual event" over time
/// (e.g., <c>"race.completed"</c>, <c>"authentication.user-account-disabled"</c>).</param>
/// <param name="version">The schema version of this concrete CLR type. Increment when the
/// shape changes in a way that affects compatibility. Keep the identifier constant; create
/// a new type with the same identifier and an incremented version for each schema change.</param>
/// <remarks>
/// <para>
/// Discovered by <see cref="MessageScanner{TBase}"/> during registry initialization to
/// produce a <see cref="MessageDefinition"/> entry.
/// </para>
/// <para>
/// <b>Important:</b> Once a message version is deployed to production, the shape of the
/// type bearing that <c>(identifier, version)</c> pair must not change. Author a new type
/// with an incremented version for any schema change.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class MessageVersionAttribute(string identifier, string version) : Attribute {

	/// <summary>
	/// The stable logical identifier of the message.
	/// </summary>
	public string Identifier { get; } = identifier;

	/// <summary>
	/// The schema version of this CLR type. Numeric or semantic.
	/// </summary>
	public string Version { get; } = version;

}
