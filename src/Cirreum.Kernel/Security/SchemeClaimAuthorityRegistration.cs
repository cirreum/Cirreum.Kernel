namespace Cirreum.Security;

/// <summary>
/// One scheme's claim-authority declaration, contributed to the service collection at
/// composition time and aggregated into an <see cref="ISchemeClaimAuthorityMap"/>.
/// </summary>
/// <param name="Scheme">The authentication scheme name this declaration applies to.</param>
/// <param name="SubjectKind">Declared by the provider that registers the scheme.</param>
/// <param name="Profile">Declared by the application; <see cref="ClaimAuthority.Unspecified"/> when it declared nothing.</param>
/// <param name="Roles">Declared by the application; <see cref="ClaimAuthority.Unspecified"/> when it declared nothing.</param>
/// <remarks>
/// <para>
/// Contributed once per <em>registered scheme</em> rather than once per configured instance.
/// The two are usually the same, but not always: a provider whose credentials resolve
/// dynamically can register a scheme with no configured instances at all, and anchoring on
/// the scheme keeps that case covered.
/// </para>
/// <para>
/// A scheme that contributes no registration resolves to
/// <see cref="SchemeClaimAuthority.Undeclared"/>, which preserves existing behaviour.
/// </para>
/// </remarks>
public sealed record SchemeClaimAuthorityRegistration(
	string Scheme,
	SubjectKind SubjectKind,
	ClaimAuthority Profile,
	ClaimAuthority Roles);
