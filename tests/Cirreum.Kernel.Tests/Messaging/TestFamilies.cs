namespace Cirreum.Messaging;

// The scanner enumerates exported types from loaded assemblies, so these families must
// be public. Each TBase is its own isolated family — the duplicate-identity pair lives
// in a separate family so the primary family's assertions stay deterministic.

/// <summary>The primary test message family.</summary>
public interface IScannerTestMessage {
}

/// <summary>A versioned member of the primary family.</summary>
[MessageVersion("kernel-tests.alpha", "1")]
public sealed record AlphaMessage(string Name, int Count) : IScannerTestMessage;

/// <summary>A second schema version co-existing with <see cref="AlphaMessage"/>.</summary>
[MessageVersion("kernel-tests.alpha", "2")]
public sealed record AlphaMessageV2(string Name) : IScannerTestMessage;

/// <summary>A second identifier in the primary family.</summary>
[MessageVersion("kernel-tests.beta", "1")]
public sealed record BetaMessage(string Payload) : IScannerTestMessage;

/// <summary>Concrete family member with no <c>[MessageVersion]</c> — the scanner must
/// warn and exclude it.</summary>
public sealed record UnversionedTestMessage(string Value) : IScannerTestMessage;

/// <summary>Abstract family member — excluded by the scan predicate, no warning.</summary>
public abstract record AbstractTestMessage : IScannerTestMessage;

/// <summary>The duplicate-identity test family.</summary>
public interface IDuplicateTestMessage {
}

/// <summary>One of two types claiming the same <c>(identifier, version)</c>.</summary>
[MessageVersion("kernel-tests.dup", "1")]
public sealed record DupFirstMessage(string Value) : IDuplicateTestMessage;

/// <summary>The other claimant — the scanner keeps whichever it sees first and warns.</summary>
[MessageVersion("kernel-tests.dup", "1")]
public sealed record DupSecondMessage(string Value) : IDuplicateTestMessage;
