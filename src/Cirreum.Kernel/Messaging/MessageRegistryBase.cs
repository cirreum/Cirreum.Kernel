namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

/// <summary>
/// Standard concrete shape for an <see cref="IMessageRegistry{TBase}"/> implementation —
/// performs the assembly scan once on first initialization and caches the discovered
/// definitions for subsequent lookups.
/// </summary>
/// <typeparam name="TBase">The base type or marker interface constraining this
/// registry's family. See <see cref="IMessageRegistry{TBase}"/>.</typeparam>
/// <remarks>
/// Downstream packages subclass to add family-specific lookups (e.g., a distributed-
/// messaging registry adds a <c>GetTargetFor(...)</c> method). The base provides the
/// standard <see cref="IMessageRegistry{TBase}"/> surface plus
/// <see cref="DefaultInitializationAsync"/> for use during host startup.
/// </remarks>
public abstract class MessageRegistryBase<TBase> : IMessageRegistry<TBase>
	where TBase : class {

	private readonly ConcurrentDictionary<string, MessageDefinition> _messages = new();
	private readonly ILogger _logger;
	private bool _initialized;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="logger">Logger used during scanning and lookups.</param>
	protected MessageRegistryBase(ILogger logger) {
		ArgumentNullException.ThrowIfNull(logger);
		this._logger = logger;
	}

	/// <summary>
	/// Performs the standard one-time assembly scan and populates the cache.
	/// Subsequent calls log a warning and short-circuit — registries are meant to be
	/// initialized once per process.
	/// </summary>
	protected ValueTask DefaultInitializationAsync() {
		if (this._initialized) {
			this._logger.LogWarning(
				"{Registry}.{Method} was called more than once. Ignoring subsequent call.",
				this.GetType().Name,
				nameof(DefaultInitializationAsync));
			return ValueTask.CompletedTask;
		}
		this._initialized = true;

		var scanned = MessageScanner<TBase>.ScanAssemblies(this._logger);
		foreach (var definition in scanned) {
			this._messages.TryAdd(definition.MessageType, definition);
		}
		return ValueTask.CompletedTask;
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
		return this.GetDefinitionFor(messageType.FullName!);
	}

	/// <inheritdoc/>
	public MessageDefinition GetDefinitionFor(string messageTypeFullName) {
		if (this._messages.TryGetValue(messageTypeFullName, out var definition)) {
			return definition;
		}
		throw new InvalidOperationException(
			$"No message definition registered for {messageTypeFullName} in registry for {typeof(TBase).FullName}. " +
			$"Ensure the type carries [{nameof(MessageVersionAttribute)}] and is discoverable at scan time.");
	}

	/// <inheritdoc/>
	public IReadOnlyCollection<MessageDefinition> GetAll() =>
		this._messages.Values.ToList().AsReadOnly();

}
