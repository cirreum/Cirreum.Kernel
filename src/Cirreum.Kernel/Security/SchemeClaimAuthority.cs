namespace Cirreum.Security;

/// <summary>
/// The resolved claim-authority declaration for a single authentication scheme — what kind
/// of subject it authenticates, and which side owns the subject's profile and roles.
/// </summary>
/// <param name="SubjectKind">Whether the scheme authenticates a person or a machine.</param>
/// <param name="Profile">Which side is authoritative for identity and profile claims.</param>
/// <param name="Roles">Which side is authoritative for roles.</param>
/// <remarks>
/// <see cref="Undeclared"/> is what an unregistered scheme resolves to, and is deliberately
/// the default value: every member reads as "not stated", so nothing is asserted about a
/// scheme the framework never saw.
/// </remarks>
public readonly record struct SchemeClaimAuthority(
	SubjectKind SubjectKind,
	ClaimAuthority Profile,
	ClaimAuthority Roles) {

	/// <summary>
	/// The value for a scheme that declared nothing — unknown subject, unspecified authority
	/// on both axes. Existing behaviour applies.
	/// </summary>
	public static SchemeClaimAuthority Undeclared => default;

}
