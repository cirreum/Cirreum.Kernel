namespace Cirreum.Security;

/// <summary>
/// Default <see cref="IAuthenticationBoundaryResolver"/> — treats every authenticated caller as
/// <see cref="AuthenticationBoundary.Global"/> and every unauthenticated caller as
/// <see cref="AuthenticationBoundary.None"/>.
/// </summary>
/// <remarks>
/// The correct default for single-scheme applications where all authenticated users belong to
/// the operator's own IdP. Multi-tenant applications get a scheme-aware resolver from the
/// Authentication track (primary scheme → Global, other schemes → Tenant), or register their
/// own; registrations use <c>TryAdd</c>, so a custom resolver registered first wins.
/// </remarks>
public sealed class DefaultAuthenticationBoundaryResolver : IAuthenticationBoundaryResolver {

	/// <inheritdoc/>
	public AuthenticationBoundary Resolve(IUserState userState, string? authenticationScheme) =>
		userState.IsAuthenticated ? AuthenticationBoundary.Global : AuthenticationBoundary.None;

}
