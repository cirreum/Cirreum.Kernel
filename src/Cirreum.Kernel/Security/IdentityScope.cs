namespace Cirreum.Security;

using System.Security.Claims;

/// <summary>
/// Which of a <see cref="ClaimsPrincipal"/>'s identities a resolver reads.
/// </summary>
/// <remarks>
/// Offered only where breadth is a legitimate choice — roles, which aggregate. It is deliberately
/// absent from the resolvers for singular facts (id, name, issuer, tenant): reading those across
/// identities does not produce a broader answer, it produces a wrong one, describing a subject
/// other than the one the principal presents.
/// </remarks>
public enum IdentityScope {

	/// <summary>
	/// Every identity the principal carries — the breadth
	/// <see cref="ClaimsPrincipal.IsInRole(string)"/> spans.
	/// </summary>
	AllIdentities = 0,

	/// <summary>
	/// Only the identity the principal presents as <see cref="ClaimsPrincipal.Identity"/>.
	/// </summary>
	PrimaryIdentity = 1

}
