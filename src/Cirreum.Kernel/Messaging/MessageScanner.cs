namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>
/// Scans loaded assemblies for concrete subtypes of <typeparamref name="TBase"/> bearing
/// <see cref="MessageVersionAttribute"/> and produces a <see cref="MessageDiscovery"/>
/// for each discovery.
/// </summary>
/// <typeparam name="TBase">The base type or marker interface that constrains the scan
/// to a single message family. See <see cref="IMessageRegistry{TBase}"/>.</typeparam>
/// <remarks>
/// <para>
/// Built on <see cref="AssemblyScanner"/>'s assembly enumeration. Duplicate
/// <c>(identifier, version)</c> pairs within the same family are logged and skipped — the
/// first discovery wins. Use distinct version values when intentionally co-existing
/// schemas during migration windows.
/// </para>
/// <para>
/// A concrete <typeparamref name="TBase"/> subtype carrying no
/// <see cref="MessageVersionAttribute"/> is warned about at scan time: such a type is
/// publishable and locally handleable, but it is invisible to the registry and can never
/// be resolved for transport — a permanent configuration error better surfaced at
/// startup, where the fix is obvious, than at first publish.
/// </para>
/// </remarks>
public static class MessageScanner<TBase> where TBase : class {

	/// <summary>
	/// Scans loaded assemblies and returns each discovery — the live CLR type paired
	/// with its <see cref="MessageDefinition"/>.
	/// </summary>
	/// <param name="logger">The logger used to record duplicates, unversioned concrete
	/// subtypes, scanning errors, and the discovery summary.</param>
	public static IReadOnlyList<MessageDiscovery> Discover(ILogger logger) {
		ArgumentNullException.ThrowIfNull(logger);
		using var loggingScope = logger.BeginScope(
			"Scanning for {BaseType} messages",
			typeof(TBase).FullName);

		var discoveries = new List<MessageDiscovery>();
		var seenIdentifiers = new Dictionary<string, HashSet<string>>();
		var duplicateCount = 0;

		foreach (var type in AssemblyScanner.ScanExportedTypes(IsMessageType)) {
			if (!TryScanType(type, logger, out var definition)) {
				continue;
			}

			if (!seenIdentifiers.TryGetValue(definition.Identifier, out var versions)) {
				versions = [];
				seenIdentifiers[definition.Identifier] = versions;
			}
			if (!versions.Add(definition.Version)) {
				duplicateCount++;
				logger.LogWarning(
					"Duplicate message [{Identifier}|v{Version}] detected on type {Type}; ignoring later occurrence.",
					definition.Identifier,
					definition.Version,
					type.FullName ?? type.Name);
				continue;
			}

			discoveries.Add(new MessageDiscovery(type, definition));
		}

		if (duplicateCount > 0) {
			logger.LogWarning(
				"{Count} duplicate {BaseType} message definition(s) were detected and skipped during scanning.",
				duplicateCount,
				typeof(TBase).Name);
		}
		if (logger.IsEnabled(LogLevel.Information)) {
			logger.LogInformation(
				"Discovered {Count} {BaseType} message definition(s) in scanned assemblies.",
				discoveries.Count,
				typeof(TBase).Name);
		}

		return discoveries;
	}

	private static bool IsMessageType(Type type) =>
		type != null
		&& type.IsClass
		&& !type.IsAbstract
		&& typeof(TBase).IsAssignableFrom(type);

	private static bool TryScanType(
		Type type,
		ILogger logger,
		[NotNullWhen(true)] out MessageDefinition? messageDefinition) {

		messageDefinition = null;
		try {
			var attr = type.GetCustomAttribute<MessageVersionAttribute>();
			if (attr == null) {
				// Publishable and locally handleable, but invisible to the registry —
				// it can never be resolved for transport. Surface the misconfiguration
				// at startup, where the fix is obvious, instead of at first publish.
				logger.LogWarning(
					"Concrete {BaseType} subtype {Type} carries no [MessageVersion] attribute. " +
					"It can be published and handled locally, but it is invisible to the " +
					"registry and cannot be resolved for transport.",
					typeof(TBase).Name,
					type.FullName ?? type.Name);
				return false;
			}

			var schema = new List<MessageProperty>();
			var properties = type
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanRead);
			foreach (var property in properties) {
				schema.Add(new MessageProperty(
					property.Name,
					property.PropertyType.FullName ?? property.PropertyType.Name));
			}

			messageDefinition = new MessageDefinition(
				attr.Identifier,
				attr.Version,
				type.FullName!,
				schema);
			return true;
		} catch (TargetInvocationException ex) {
			logger.LogError(
				ex,
				"Failed to scan {Type} for {Attr} (TargetInvocationException): {Inner}",
				type.FullName ?? type.Name,
				nameof(MessageVersionAttribute),
				ex.InnerException?.Message);
			return false;
		} catch (Exception ex) {
			logger.LogError(
				ex,
				"Unexpected error scanning {Type} for {Attr}.",
				type.FullName ?? type.Name,
				nameof(MessageVersionAttribute));
			return false;
		}
	}

}
