namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>
/// Scans loaded assemblies for concrete subtypes of <typeparamref name="TBase"/> bearing
/// <see cref="MessageVersionAttribute"/> and produces a <see cref="MessageDefinition"/>
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
/// </remarks>
public static class MessageScanner<TBase> where TBase : class {

	/// <summary>
	/// Scans for and returns all <typeparamref name="TBase"/> message definitions
	/// discovered in loaded assemblies.
	/// </summary>
	/// <param name="logger">The logger used to record duplicates, scanning errors, and
	/// the discovery summary.</param>
	public static List<MessageDefinition> ScanAssemblies(ILogger logger) {
		ArgumentNullException.ThrowIfNull(logger);
		using var loggingScope = logger.BeginScope(
			"Scanning for {BaseType} messages",
			typeof(TBase).FullName);

		var definitions = new List<MessageDefinition>();
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

			definitions.Add(definition);
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
				definitions.Count,
				typeof(TBase).Name);
		}

		return definitions;
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
