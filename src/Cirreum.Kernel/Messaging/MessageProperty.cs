namespace Cirreum.Messaging;

/// <summary>
/// A property in a discovered <see cref="MessageDefinition"/>'s schema — the public read
/// surface of the CLR message type as captured by the scanner.
/// </summary>
/// <param name="Name">The property name as declared on the CLR type.</param>
/// <param name="Type">The property's .NET type, expressed as <c>FullName</c> when
/// available (otherwise <c>Name</c>).</param>
public record MessageProperty(string Name, string Type);
