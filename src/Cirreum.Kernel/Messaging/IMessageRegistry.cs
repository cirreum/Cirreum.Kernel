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
	/// Gets the definition for the given fully-qualified CLR type name.
	/// </summary>
	/// <exception cref="InvalidOperationException">No definition is registered.</exception>
	MessageDefinition GetDefinitionFor(string messageTypeFullName);

	/// <summary>
	/// Gets all discovered definitions in this registry's family.
	/// </summary>
	IReadOnlyCollection<MessageDefinition> GetAll();

}
