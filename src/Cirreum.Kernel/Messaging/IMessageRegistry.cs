namespace Cirreum.Messaging;

/// <summary>
/// Provides discovery access to <see cref="MessageDefinition"/> entries for a versioned-
/// message family identified by <typeparamref name="TBase"/>.
/// </summary>
/// <typeparam name="TBase">The base type or marker interface that all members of this
/// message family inherit or implement. Examples: <c>DistributedMessage</c> for the
/// distributed-events channel, <c>IAuthenticationEvent</c> for the auth-events channel.
/// The <typeparamref name="TBase"/> parameter IS the channel identity — each
/// registration produces an independent registry instance.</typeparam>
/// <remarks>
/// <para>
/// Populated at host startup by scanning loaded assemblies for concrete subtypes of
/// <typeparamref name="TBase"/> that carry <see cref="MessageVersionAttribute"/>. Cached
/// thereafter for runtime lookup.
/// </para>
/// <para>
/// Apps register one registry per channel via the runtime composition extensions. The framework
/// supplies <see cref="MessageRegistryBase{TBase}"/> as the standard concrete shape;
/// downstream packages may subclass to add channel-specific lookups (e.g., target lookup
/// for distributed messages, severity lookup for auth events).
/// </para>
/// </remarks>
public interface IMessageRegistry<TBase> where TBase : class {

	/// <summary>
	/// Gets the definition for the strongly-typed message <typeparamref name="T"/>.
	/// </summary>
	/// <exception cref="InvalidOperationException">No definition is registered for the
	/// given type — typically because <see cref="MessageVersionAttribute"/> was not
	/// applied or the type wasn't discovered at startup.</exception>
	MessageDefinition GetDefinitionFor<T>() where T : TBase;

	/// <summary>
	/// Gets the definition for the given runtime <see cref="Type"/>.
	/// </summary>
	/// <exception cref="ArgumentException">The type is not assignable to
	/// <typeparamref name="TBase"/>.</exception>
	/// <exception cref="InvalidOperationException">No definition is registered.</exception>
	MessageDefinition GetDefinitionFor(Type messageType);

	/// <summary>
	/// Gets all discovered definitions in this registry's family.
	/// </summary>
	IReadOnlyCollection<MessageDefinition> GetAll();

	/// <summary>
	/// Resolves the concrete CLR type for a wire <c>(identifier, version)</c> identity,
	/// or <see langword="null"/> when this process carries no matching type.
	/// </summary>
	/// <param name="identifier">The stable logical identifier from the wire.</param>
	/// <param name="version">The schema version from the wire.</param>
	/// <remarks>
	/// <para>
	/// Nullable-returning by design, in deliberate contrast with the throwing
	/// <c>GetDefinitionFor</c> family: an outbound definition miss is a permanent
	/// configuration error on the caller's own type, while an inbound identity miss is a
	/// normal operating condition on a foreign identity — version skew during a rolling
	/// upgrade, or a fan-out subscription delivering family members this consumer
	/// doesn't handle. Callers choose their own disposition.
	/// </para>
	/// <para>
	/// A resolved type is conforming by construction — it entered the registry through
	/// the concrete-<typeparamref name="TBase"/> scan. A null, empty, or whitespace
	/// identifier or version resolves to <see langword="null"/>.
	/// </para>
	/// </remarks>
	Type? ResolveType(string identifier, string version);

	/// <summary>
	/// Resolves the concrete CLR type for a definition's identity — sugar over
	/// <see cref="ResolveType(string, string)"/>.
	/// </summary>
	/// <param name="definition">The definition whose <c>(Identifier, Version)</c>
	/// identity to resolve. A registry-issued definition always resolves; only a
	/// hand-constructed one can miss.</param>
	Type? ResolveType(MessageDefinition definition);

}
