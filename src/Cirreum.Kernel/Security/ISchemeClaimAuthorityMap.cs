namespace Cirreum.Security;

/// <summary>
/// Looks up the claim-authority declaration for an authentication scheme.
/// </summary>
/// <remarks>
/// <para>
/// Built once at composition from the <see cref="SchemeClaimAuthorityRegistration"/> entries
/// the authentication providers contributed. Lookups are pure — no I/O, no per-request
/// policy — because a scheme's declaration is fixed when the host is composed.
/// </para>
/// <para>
/// The contract lives here rather than in the authentication runtime because consumers sit
/// below it: the server's user-state accessor resolves the declaration per invocation, and
/// operation authorizers read the result off <see cref="IUserState"/>.
/// </para>
/// <para>
/// Resolve it optionally. A host that composes no authentication registers no implementation,
/// and every scheme is then <see cref="SchemeClaimAuthority.Undeclared"/>.
/// </para>
/// </remarks>
public interface ISchemeClaimAuthorityMap {

	/// <summary>
	/// Gets the declaration for <paramref name="scheme"/>, or
	/// <see cref="SchemeClaimAuthority.Undeclared"/> when the scheme contributed none.
	/// </summary>
	/// <param name="scheme">The authentication scheme name. A <see langword="null"/> or blank
	/// value returns <see cref="SchemeClaimAuthority.Undeclared"/>.</param>
	SchemeClaimAuthority Get(string? scheme);

}
