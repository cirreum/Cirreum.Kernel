namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

/// <summary>
/// Standard concrete shape for an <see cref="IMessageRegistry{TBase}"/> implementation —
/// performs the assembly scan once on first initialization and caches the discoveries
/// for subsequent lookups in both directions: CLR type → wire identity
/// (<see cref="GetDefinitionFor(Type)"/>) and wire identity → CLR type
/// (<see cref="ResolveType(string, string)"/>).
/// </summary>
/// <typeparam name="TBase">The base type or marker interface constraining this
/// registry's family. See <see cref="IMessageRegistry{TBase}"/>.</typeparam>
/// <param name="logger">Logger used during scanning and lookups.</param>
/// <remarks>
/// Downstream packages subclass to add family-specific lookups (e.g., a distributed-
/// messaging registry adds a <c>GetTargetFor(...)</c> method) — override
/// <see cref="OnMessageDiscovered"/> to capture per-type metadata from the single scan
/// instead of scanning again. The base provides the standard
/// <see cref="IMessageRegistry{TBase}"/> surface plus
/// <see cref="DefaultInitializationAsync"/> for use during host startup.
/// </remarks>
public abstract class MessageRegistryBase<TBase>(ILogger logger) : IMessageRegistry<TBase>
	where TBase : class {

	private readonly ConcurrentDictionary<string, MessageDefinition> _messages = new();
	private readonly ConcurrentDictionary<string, Type> _typesByIdentity = new(StringComparer.Ordinal);
	private bool _initialized;

	/// <summary>
	/// Performs the standard one-time assembly scan and populates the lookup maps.
	/// Subsequent calls log a warning and short-circuit — registries are meant to be
	/// initialized once per process.
	/// </summary>
	protected ValueTask DefaultInitializationAsync() {
		if (this._initialized) {
			logger.LogWarning(
				"{Registry}.{Method} was called more than once. Ignoring subsequent call.",
				this.GetType().Name,
				nameof(DefaultInitializationAsync));
			return ValueTask.CompletedTask;
		}
		this._initialized = true;

		foreach (var discovery in MessageScanner<TBase>.Discover(logger)) {
			this._messages.TryAdd(discovery.Definition.MessageType, discovery.Definition);
			this._typesByIdentity.TryAdd(
				KeyFor(discovery.Definition.Identifier, discovery.Definition.Version),
				discovery.ClrType);
			this.OnMessageDiscovered(discovery);
		}
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// Called once per scan discovery, after the base lookup maps contain the entry.
	/// Override to capture family-specific per-type metadata (e.g., a routing attribute
	/// read off <see cref="MessageDiscovery.ClrType"/>) without a second assembly scan.
	/// </summary>
	/// <param name="discovery">The discovery — the live CLR type paired with its
	/// scanned definition.</param>
	protected virtual void OnMessageDiscovered(MessageDiscovery discovery) {
	}

	/// <inheritdoc/>
	public MessageDefinition GetDefinitionFor<T>() where T : TBase =>
		this.GetDefinitionFor(typeof(T));

	/// <inheritdoc/>
	public MessageDefinition GetDefinitionFor(Type messageType) {
		ArgumentNullException.ThrowIfNull(messageType);
		if (!typeof(TBase).IsAssignableFrom(messageType)) {
			throw new ArgumentException(
				$"Type {messageType.FullName} is not assignable to {typeof(TBase).FullName}.",
				nameof(messageType));
		}
		if (this._messages.TryGetValue(messageType.FullName!, out var definition)) {
			return definition;
		}
		throw new InvalidOperationException(
			$"No message definition registered for {messageType.FullName} in registry for {typeof(TBase).FullName}. " +
			$"Ensure the type carries [{nameof(MessageVersionAttribute)}] and is discoverable at scan time.");
	}

	/// <inheritdoc/>
	public IReadOnlyCollection<MessageDefinition> GetAll() =>
		this._messages.Values.ToList().AsReadOnly();

	/// <inheritdoc/>
	public Type? ResolveType(string identifier, string version) {
		if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(version)) {
			return null;
		}
		return this._typesByIdentity.TryGetValue(KeyFor(identifier, version), out var messageType)
			? messageType
			: null;
	}

	/// <inheritdoc/>
	public Type? ResolveType(MessageDefinition definition) {
		ArgumentNullException.ThrowIfNull(definition);
		return this.ResolveType(definition.Identifier, definition.Version);
	}

	private static string KeyFor(string identifier, string version) =>
		$"{identifier}|{version}";

}
