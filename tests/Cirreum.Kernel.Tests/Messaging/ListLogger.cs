namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;

/// <summary>
/// Minimal recording logger — captures each entry's level and rendered message for
/// assertion.
/// </summary>
internal sealed class ListLogger : ILogger {

	public List<(LogLevel Level, string Message)> Entries { get; } = [];

	public IEnumerable<string> Warnings =>
		this.Entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message);

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter) =>
		this.Entries.Add((logLevel, formatter(state, exception)));

}
