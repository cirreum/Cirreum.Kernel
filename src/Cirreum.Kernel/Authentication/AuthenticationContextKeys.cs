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
/// Two-Phase Auth <c>connection.Promote(...)</c> extension).
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
	/// The resolved <see cref="IApplicationUser"/> for the current identity.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Populated by the role claims transformer's adapter when it loads the application
	/// user during role enrichment. Read by downstream consumers
	/// (e.g. <c>UserAccessor</c>) so they avoid a redundant resolver call.
	/// </para>
	/// <para>
	/// Lives request-scoped on <c>HttpContext.Items</c>, and connection-scoped on
	/// <c>IInvocationConnection.Items</c> for long-lived connections — the per-invocation
	/// contexts seed their own Items from the connection's well-known auth slots. Two-Phase
	/// Auth promotion (<c>connection.Promote(principal)</c>) <em>evicts</em> this slot from
	/// the connection before stamping <see cref="PromotedPrincipal"/>, so a cached user
	/// resolved for the pre-promotion identity can never pair with the promoted principal;
	/// the lazy resolve path repopulates it for the promoted identity.
	/// </para>
	/// </remarks>
	public const string ApplicationUserCache = "__Cirreum_ApplicationUser";

	/// <summary>
	/// The promoted <c>ClaimsPrincipal</c> for a Two-Phase Auth connection that has
	/// transitioned from anonymous-pending-auth to authenticated mid-connection.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Stamped into <c>IInvocationConnection.Items</c> by the
	/// <c>connection.Promote(principal)</c> extension (in
	/// <c>Cirreum.Runtime.AuthenticationProvider</c>) when a long-lived connection that
	/// started anonymous (e.g. cold-IVA, browser AI chat warm session) is upgraded to an
	/// authenticated principal mid-flight. Promotion evicts
	/// <see cref="ApplicationUserCache"/> from the connection <em>before</em> stamping
	/// this slot — ordered so a concurrently-constructed invocation can never observe the
	/// promoted principal with the previous identity's cached application user.
	/// </para>
	/// <para>
	/// Read through the <c>Cirreum.Contracts</c> connection-ownership surface
	/// (<c>PromotedUser</c> / <c>EffectiveUser</c> / <c>IsUserPromoted</c>): the framework's
	/// per-invocation contexts snapshot <c>connection.EffectiveUser</c> at construction, so
	/// the promoted principal takes precedence over the connection's original (anonymous)
	/// principal from the next invocation onward.
	/// </para>
	/// </remarks>
	public const string PromotedPrincipal = "__Cirreum_PromotedPrincipal";

}
