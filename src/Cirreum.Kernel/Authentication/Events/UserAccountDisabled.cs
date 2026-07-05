namespace Cirreum.Authentication.Events;

using Cirreum.Messaging;

/// <summary>
/// Signals that a user account has been disabled at the identity provider or in the
/// app's own user store. Consumers terminate active sessions, evict caches, and
/// reject subsequent authentication attempts for the subject.
/// </summary>
/// <param name="Subject">The disabled user's stable subject identifier (typically the
/// <c>sub</c> claim or app-side user id).</param>
/// <param name="OccurredAt">When the disable action occurred (in the publishing system's
/// authority). Used for ordering and audit.</param>
/// <remarks>
/// Distinct from <see cref="SessionTerminationRequested"/> — disabling an account
/// includes session termination semantics but also signals "do not authenticate this
/// subject again until re-enabled."
/// </remarks>
[MessageVersion("authentication.user-account-disabled", "1")]
public sealed record UserAccountDisabled(
	string Subject,
	DateTimeOffset OccurredAt
) : IAuthenticationEvent {

	/// <summary>
	/// Optional human-readable reason for the disable action (e.g., "compliance review",
	/// "security incident"). Surfaced in audit logs by handlers.
	/// </summary>
	public string? Reason { get; init; }

}
