namespace Cirreum.Authentication.Events;

/// <summary>
/// The stable <c>[MessageVersion]</c> identifiers for the framework authentication
/// events — one constant per event, referenced by each event record's attribute so
/// every wire identity has a single authoritative definition.
/// </summary>
internal static class EventMessages {

	/// <summary>The stable message identifier for the <c>CredentialRevoked</c> event.</summary>
	public const string CredentialRevoked = "authentication.credential-revoked";

	/// <summary>The stable message identifier for the <c>GrantsInvalidated</c> event.</summary>
	public const string GrantsInvalidated = "authentication.grants-invalidated";

	/// <summary>The stable message identifier for the <c>SessionTerminationRequested</c> event.</summary>
	public const string SessionTerminationRequested = "authentication.session-termination-requested";

	/// <summary>The stable message identifier for the <c>UserAccountDisabled</c> event.</summary>
	public const string UserAccountDisabled = "authentication.user-account-disabled";

}
