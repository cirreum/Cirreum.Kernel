namespace Cirreum.Authentication;

/// <summary>
/// Well-known keys used to coordinate authentication context across pipeline stages
/// via <c>HttpContext.Items</c> and <c>IInvocationConnection.Items</c>.
/// </summary>
/// <remarks>
/// <para>
/// Centralizes the string keys read and written by multiple subsystems (the dynamic
/// scheme forward selector, the role claims transformer, <c>IAuthenticationBoundaryResolver</c>
/// consumers, the application user resolver dispatcher, downstream user accessors, and the
/// Two-Phase Auth <c>Promote</c> helper).
/// </para>
/// <para>
/// Lives in <c>Cirreum.Kernel</c> so cross-cutting consumers in any package can
/// reference the canonical keys without taking an additional package dependency. The
/// string values are stable contracts; treat changes as breaking.
/// </para>
/// </remarks>
public static class AuthenticationContextKeys {

	/// <summary>
	/// The authentication scheme that authenticated the current request.
	/// </summary>
	/// <remarks>
	/// Stamped during dynamic scheme dispatch by the forward selector in
	/// <c>Cirreum.Runtime.AuthenticationProvider</c>, and defensively by role-claims
	/// transformers for routes wired to an explicit scheme. Read by
	/// <c>IAuthenticationBoundaryResolver</c> consumers and by the per-scheme
	/// <c>IApplicationUserResolver</c> dispatcher.
	/// </remarks>
	public const string AuthenticatedScheme = "__Cirreum_AuthenticatedScheme";

	/// <summary>
	/// The resolved <see cref="IApplicationUser"/> for the current request.
	/// </summary>
	/// <remarks>
	/// Populated by the role claims transformer's adapter when it loads the application
	/// user during role enrichment. Read by request-scoped consumers
	/// (e.g. <c>UserAccessor</c>) so they avoid a redundant resolver call.
	/// </remarks>
	public const string ApplicationUserCache = "__Cirreum_ApplicationUser";

	/// <summary>
	/// The promoted <c>ClaimsPrincipal</c> for a Two-Phase Auth connection that has
	/// transitioned from anonymous-pending-auth to authenticated mid-connection.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Stamped into <c>IInvocationConnection.Items</c> by the spine-shipped
	/// <c>TwoPhaseAuth.Promote</c> helper when a long-lived connection that started
	/// anonymous (e.g. cold-IVA, browser AI chat warm session) is upgraded to an
	/// authenticated principal mid-flight.
	/// </para>
	/// <para>
	/// Read by the per-invocation <c>UserStateAccessor</c> when materializing
	/// <c>IUserState</c> for messages flowing through a promoted connection — the
	/// promoted principal takes precedence over the connection's original (anonymous)
	/// principal.
	/// </para>
	/// </remarks>
	public const string PromotedPrincipal = "__Cirreum_PromotedPrincipal";

}
