namespace Cirreum.Security;

/// <summary>
/// Which side is authoritative for a class of a user's attributes on a given scheme — the
/// identity provider, or the application's own store.
/// </summary>
/// <remarks>
/// <para>
/// The identity provider is always the authority for <em>authentication</em>. It is not
/// automatically the authority for <em>attributes</em>: an application backed by its own
/// user store commonly owns display name, profile, and roles while the identity provider
/// owns only the fact that the caller signed in.
/// </para>
/// <para>
/// This is a <b>precedence</b> rule, not an exclusivity rule. The declared side wins when
/// both supply a value; neither is prevented from supplying one. That distinction matters
/// where availability varies per user on a single scheme — a federated account may arrive
/// with full profile claims while a local account on the same scheme arrives thin.
/// </para>
/// </remarks>
public enum ClaimAuthority {

	/// <summary>
	/// No authority was declared for this scheme. Existing behaviour applies: roles resolve
	/// from the application store when a resolver is registered for the scheme, and from the
	/// identity provider otherwise.
	/// </summary>
	Unspecified = 0,

	/// <summary>
	/// The identity provider is authoritative. Claims it issues natively win over any the
	/// application minted into the token.
	/// </summary>
	IdentityProvider,

	/// <summary>
	/// The application's own store is authoritative. For roles this means a live per-request
	/// lookup, so revocation takes effect immediately rather than at the next sign-in. For
	/// profile it means claims the application minted into the token win over native ones.
	/// </summary>
	ApplicationStore
}
