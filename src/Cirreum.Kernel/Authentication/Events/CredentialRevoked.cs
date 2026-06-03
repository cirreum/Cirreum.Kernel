namespace Cirreum.Authentication.Events;

using Cirreum.Messaging;

/// <summary>
/// Signals that a specific credential (API key, refresh token, session ticket, signed-
/// request keypair entry, etc.) has been revoked. Consumers evict cached resolver
/// results, update denylists, and terminate any active sessions or long-lived
/// connections established by the credential.
/// </summary>
/// <param name="CredentialId">Stable identifier of the revoked credential. The shape is
/// scheme-specific (API key id, JWT jti, keypair fingerprint); consumers correlate
/// against their own credential indexes.</param>
/// <param name="Subject">The user/principal subject the credential was bound to.
/// Allows handlers that index by subject (e.g., the connection-terminator) to act
/// without an extra lookup.</param>
/// <param name="RevokedAt">When the revocation occurred (in the publishing system's
/// authority).</param>
/// <remarks>
/// The framework-shipped connection-terminator (in <c>Cirreum.Runtime.Server</c>) treats
/// <see cref="CredentialRevoked"/> as a stronger form of
/// <see cref="SessionTerminationRequested"/> — it aborts all of the subject's active
/// connections, on the conservative assumption that any of them may have been
/// established by the now-revoked credential.
/// </remarks>
[MessageVersion("authentication.credential-revoked", "1")]
public sealed record CredentialRevoked(
	string CredentialId,
	string Subject,
	DateTimeOffset RevokedAt) : IAuthenticationEvent {

	/// <summary>
	/// Optional credential-type tag (e.g., <c>"apikey"</c>, <c>"signedrequest"</c>,
	/// <c>"sessionticket"</c>). Lets handlers filter or route based on credential class.
	/// </summary>
	public string? CredentialType { get; init; }

	/// <summary>
	/// Optional human-readable reason for the revocation.
	/// </summary>
	public string? Reason { get; init; }

}
