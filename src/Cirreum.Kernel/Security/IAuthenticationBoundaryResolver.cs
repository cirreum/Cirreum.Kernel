namespace Cirreum.Security;

/// <summary>
/// Resolves the <see cref="AuthenticationBoundary"/> for an authenticated user state —
/// the classification (Global operator, Tenant, or None) that authorization consumers
/// such as grant providers read from <c>IUserState.AuthenticationBoundary</c>.
/// </summary>
/// <remarks>
/// <para>
/// The server user-state pipeline resolves this service per invocation and stamps the
/// result onto the user state. Registrations use <c>TryAdd</c> throughout, so an
/// application-registered resolver always wins:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="DefaultAuthenticationBoundaryResolver"/> — every
///   authenticated caller is <see cref="AuthenticationBoundary.Global"/>; registered by
///   the server services package alongside its user-state accessor.</description></item>
///   <item><description>The primary-scheme resolver in <c>Cirreum.Runtime.Authentication</c> —
///   callers on the configured <c>PrimaryScheme</c> are Global, other authenticated
///   schemes are Tenant; registered when the Authentication track is composed.</description></item>
///   <item><description>A custom implementation registered by the application before
///   composition.</description></item>
/// </list>
/// </remarks>
/// <example>
/// A minimal custom resolver that treats any caller whose scheme starts with
/// <c>"internal-"</c> as <see cref="AuthenticationBoundary.Global"/>:
/// <code>
/// internal sealed class PrefixAuthenticationBoundaryResolver : IAuthenticationBoundaryResolver {
///     public AuthenticationBoundary Resolve(IUserState userState, string? authenticationScheme) {
///         if (!userState.IsAuthenticated) return AuthenticationBoundary.None;
///         return authenticationScheme?.StartsWith("internal-") == true
///             ? AuthenticationBoundary.Global
///             : AuthenticationBoundary.Tenant;
///     }
/// }
/// </code>
/// </example>
public interface IAuthenticationBoundaryResolver {

	/// <summary>
	/// Computes the authentication boundary for the given user state and authentication scheme.
	/// </summary>
	/// <param name="userState">The authenticated user state.</param>
	/// <param name="authenticationScheme">
	/// The authentication scheme name that authenticated the caller, or <see langword="null"/>
	/// when scheme is not applicable (e.g., Blazor WASM, Azure Functions binding context).
	/// </param>
	AuthenticationBoundary Resolve(IUserState userState, string? authenticationScheme);
}
