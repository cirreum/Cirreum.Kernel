namespace Cirreum.Authentication.Events;

using Cirreum.Messaging;

/// <summary>
/// Signals that a subject's active sessions should be terminated immediately — typically
/// from a user-initiated logout, admin-issued "force sign-out", or a security workflow
/// that detected anomalous activity short of full account disablement.
/// </summary>
/// <param name="Subject">The subject whose sessions should terminate. Consumers (e.g.,
/// the connection-terminator) enumerate active connections indexed by subject and
/// call <c>Abort()</c> on each.</param>
/// <param name="OccurredAt">When the termination was requested (in the publishing
/// system's authority).</param>
/// <remarks>
/// Distinct from <see cref="CredentialRevoked"/> — terminating sessions does NOT
/// invalidate the underlying credential. A subject whose sessions are terminated can
/// authenticate again immediately with the same credential. Use
/// <see cref="CredentialRevoked"/> or <see cref="UserAccountDisabled"/> for stronger
/// semantics.
/// </remarks>
[MessageVersion(EventMessages.SessionTerminationRequested, "1")]
public sealed record SessionTerminationRequested(
	string Subject,
	DateTimeOffset OccurredAt
) : IAuthenticationEvent {

	/// <summary>
	/// Optional session identifier to scope the termination to a single session rather
	/// than all of the subject's active sessions. Useful for "sign out this device"
	/// flows. When <see langword="null"/>, all of the subject's sessions terminate.
	/// </summary>
	public string? SessionId { get; init; }

	/// <summary>
	/// Optional human-readable reason for the termination request.
	/// </summary>
	public string? Reason { get; init; }

}
