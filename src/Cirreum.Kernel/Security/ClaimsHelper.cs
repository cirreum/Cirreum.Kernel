namespace Cirreum.Security;

using System.Security.Claims;

/// <summary>
/// Helper class for working with identity claims across different identity providers.
/// </summary>
public static class ClaimsHelper {

	static class EntraClaimTypes {
		public const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";
		public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
		public const string IdentityProvider = "http://schemas.microsoft.com/identity/claims/identityprovider";
	}

	/// <summary>
	/// The value of the first claim in <paramref name="claimTypes"/> that carries a non-blank value,
	/// or <see langword="null"/> when none does.
	/// </summary>
	/// <remarks>
	/// Every resolver routes through this so a present-but-blank claim is treated as absent rather
	/// than as an answer. That matters twice: a blank claim must not shadow a populated one later in
	/// the order, and it must not escape as a non-null string — callers coalesce the null result to
	/// their own default (<c>ResolveName(principal) ?? "unknown"</c>), which a whitespace-only value
	/// would silently defeat.
	/// </remarks>
	private static string? FirstNonBlank(ClaimsIdentity identity, params ReadOnlySpan<string> claimTypes) {
		foreach (var claimType in claimTypes) {
			var value = identity.FindFirst(claimType)?.Value;
			if (value.HasValue()) {
				return value;
			}
		}
		return null;
	}

	// There is deliberately no ClaimsPrincipal counterpart. ClaimsPrincipal.FindFirst searches
	// every identity, which is the one thing the singular-fact resolvers must not do: a value
	// taken from a second authentication context is not a broader answer, it is an answer about
	// someone else. Those resolvers scope to the primary identity and return null otherwise.
	// Roles, the only aggregate, do their own identity walk under an explicit IdentityScope.

	/// <summary>
	/// Detects the identity provider based on the issuer claim from the <see cref="ClaimsPrincipal"/>.
	/// </summary>
	public static IdentityProviderType ResolveProvider(ClaimsPrincipal principal) {
		ArgumentNullException.ThrowIfNull(principal);

		// Handle AnonymousUser specifically
		if (principal == AnonymousUser.Shared) {
			return IdentityProviderType.None;
		}

		// Resolve from the primary identity when there is one, as ResolveOid/ResolveTid/ResolveName
		// do. ClaimsPrincipal.FindFirst walks every identity in order, so on a multi-scheme
		// principal both the anonymity marker and the issuer could otherwise be answered by a
		// secondary identity — describing an identity other than the one being classified.
		if (principal.Identity is ClaimsIdentity identity) {
			return ResolveProvider(identity);
		}

		return IdentityProviderType.Unknown;

	}

	/// <summary>
	/// The <c>iss</c> claim of the principal, or <see langword="null"/> when it carries none.
	/// </summary>
	/// <remarks>
	/// Read from the principal's primary identity when it has one, so a multi-scheme principal
	/// reports the issuer of the identity it presents rather than whichever identity happens to
	/// carry an <c>iss</c> claim first.
	/// </remarks>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	public static string? ResolveIssuer(ClaimsPrincipal principal) =>
		principal.Identity is ClaimsIdentity identity ? ResolveIssuer(identity) : null;

	/// <inheritdoc cref="ResolveIssuer(ClaimsPrincipal)"/>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	public static string? ResolveIssuer(ClaimsIdentity identity) => FirstNonBlank(identity, "iss");
	/// <summary>
	/// Detects the identity provider based on the issuer claim from the <see cref="ClaimsIdentity"/>.
	/// </summary>
	public static IdentityProviderType ResolveProvider(ClaimsIdentity identity) {
		ArgumentNullException.ThrowIfNull(identity);

		// Handle AnonymousUser specifically
		if (identity.FindFirst(ClaimTypes.Anonymous)?.Value == "true") {
			return IdentityProviderType.None;
		}

		return ResolveProvider(ResolveIssuer(identity));

	}

	/// <summary>
	/// Classifies a raw <c>iss</c> value into the identity provider whose brand a user would
	/// recognize, or <see cref="IdentityProviderType.Unknown"/> when it matches nothing known.
	/// </summary>
	/// <remarks>
	/// Pairs with <see cref="UserProfile.Issuer"/>: the profile keeps the issuer verbatim, and this
	/// classifies it without needing a principal rebuilt around it. The classification is
	/// best-effort — <see cref="IdentityProviderType.Unknown"/> is an ordinary answer for a valid
	/// token from an issuer this table does not name, never a signal that anything is wrong.
	/// </remarks>
	/// <param name="issuer">The issuer to classify. Blank values resolve to <see cref="IdentityProviderType.Unknown"/>.</param>
	public static IdentityProviderType ResolveProvider(string? issuer) {

		if (!issuer.HasValue()) {
			return IdentityProviderType.Unknown;
		}

		var (host, path) = SplitIssuer(issuer);
		return ResolveProviderFromIssuer(host, path);

	}

	/// <summary>
	/// Splits an issuer into its lower-cased host and path.
	/// </summary>
	/// <remarks>
	/// Hand-parsed rather than routed through <see cref="Uri"/>: a scheme is conventional but not
	/// guaranteed on an <c>iss</c> value (<c>accounts.google.com</c> is a real one), and
	/// <see cref="Uri"/> rejects the scheme-less form outright. Port and userinfo are discarded so
	/// a self-hosted provider on a non-default port still matches its domain. Query and fragment
	/// are discarded because the path is searched for provider discriminators: leaving them in
	/// lets attacker-chosen text impersonate one, as in
	/// <c>https://unrelated.example/?redirect=/realms/demo</c>.
	/// </remarks>
	private static (string Host, string Path) SplitIssuer(string issuer) {

		var value = issuer.Trim();

		var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
		if (schemeEnd >= 0) {
			value = value[(schemeEnd + 3)..];
		}

		// Before splitting host from path, so a query or fragment can neither carry a
		// discriminator nor hide the first path separator.
		var trim = value.IndexOfAny(['?', '#']);
		if (trim >= 0) {
			value = value[..trim];
		}

		var slash = value.IndexOf('/');
		var host = slash < 0 ? value : value[..slash];
		var path = slash < 0 ? string.Empty : value[slash..];

		var at = host.LastIndexOf('@');
		if (at >= 0) {
			host = host[(at + 1)..];
		}

		var colon = host.IndexOf(':');
		if (colon >= 0) {
			host = host[..colon];
		}

		return (host.ToLowerInvariant(), path.ToLowerInvariant());

	}

	/// <summary>
	/// Whether <paramref name="host"/> is <paramref name="domain"/> or a subdomain of it.
	/// </summary>
	/// <remarks>
	/// Anchored on a label boundary rather than a substring scan: a bare
	/// <c>Contains("github.com")</c> also matches <c>github.com.example.invalid</c>, which lets an
	/// unrelated issuer present itself as a known provider.
	/// </remarks>
	private static bool IsHost(string host, string domain) =>
		host.Length >= domain.Length
		&& host.EndsWith(domain, StringComparison.Ordinal)
		&& (host.Length == domain.Length || host[host.Length - domain.Length - 1] == '.');
	private static IdentityProviderType ResolveProviderFromIssuer(string host, string path) {

		// Path-shaped discriminators first. Keycloak is self-hosted on arbitrary domains, so its
		// realm segment is the only reliable signal — and since Keycloak 17 that segment is
		// /realms/, the /auth prefix having been dropped with the Quarkus distribution. Legacy
		// Azure AD B2C issues from the same host as Entra and is told apart only by its /tfp/
		// policy segment, so it has to be caught before the host table classifies it as Entra.
		if (path.Contains("/realms/", StringComparison.Ordinal)) {
			return IdentityProviderType.Keycloak;
		}

		if (path.Contains("/tfp/", StringComparison.Ordinal) && IsHost(host, "login.microsoftonline.com")) {
			return IdentityProviderType.EntraExt;
		}

		// Cognito issues from cognito-idp.{region}.amazonaws.com. Both ends are required:
		// amazonaws.com alone is a hosting domain and would claim every identity provider that
		// happens to run on AWS, while the prefix alone would accept any host anyone chooses to
		// name cognito-idp.something.
		if (host.StartsWith("cognito-idp.", StringComparison.Ordinal) && IsHost(host, "amazonaws.com")) {
			return IdentityProviderType.AWS_Cognito;
		}

		return host switch {

			// Microsoft Entra ID (formerly Azure AD). sts.windows.net issues v1.0 tokens, and the
			// login.windows.net / login.microsoft.com aliases still appear on older tenants.
			_ when IsHost(host, "login.microsoftonline.com")
				|| IsHost(host, "sts.windows.net")
				|| IsHost(host, "login.windows.net")
				|| IsHost(host, "login.microsoft.com") => IdentityProviderType.Entra,

			// Microsoft Entra External ID (formerly Azure AD B2C)
			_ when IsHost(host, "ciamlogin.com")
				|| IsHost(host, "b2clogin.com") => IdentityProviderType.EntraExt,

			// Auth0
			_ when IsHost(host, "auth0.com") => IdentityProviderType.Auth0,

			// Okta
			_ when IsHost(host, "okta.com")
				|| IsHost(host, "oktapreview.com")
				|| IsHost(host, "okta-emea.com") => IdentityProviderType.Okta,

			// Ping Identity
			_ when IsHost(host, "pingidentity.com")
				|| IsHost(host, "ping-eng.com")
				|| IsHost(host, "pingone.com") => IdentityProviderType.PingIdentity,

			// Descope
			_ when IsHost(host, "descope.com")
				|| IsHost(host, "descope.io") => IdentityProviderType.Descope,

			// Akamai Identity Cloud
			_ when IsHost(host, "secureidentity.akamai.com")
				|| IsHost(host, "identity.akamai.com") => IdentityProviderType.Akamai,

			// Authlete
			_ when IsHost(host, "authlete.com")
				|| IsHost(host, "authlete.net") => IdentityProviderType.Authlete,

			// IBM Security Verify (formerly IBM Cloud Identity)
			_ when IsHost(host, "verify.ibm.com")
				|| IsHost(host, "cloudidentity.com") => IdentityProviderType.IBM,

			// Duende IdentityServer
			_ when IsHost(host, "duendesoftware.com")
				|| IsHost(host, "identityserver.io")
				|| IsHost(host, "identityserver4.io") => IdentityProviderType.Duende,

			// Google
			_ when IsHost(host, "accounts.google.com") => IdentityProviderType.Google,

			// Facebook
			_ when IsHost(host, "facebook.com") => IdentityProviderType.Facebook,

			// Apple
			_ when IsHost(host, "appleid.apple.com") => IdentityProviderType.Apple,

			// GitHub
			_ when IsHost(host, "github.com") => IdentityProviderType.GitHub,

			// LinkedIn
			_ when IsHost(host, "linkedin.com") => IdentityProviderType.LinkedIn,

			// Salesforce
			_ when IsHost(host, "salesforce.com")
				|| IsHost(host, "force.com") => IdentityProviderType.Salesforce,

			// X (formerly Twitter). The member keeps its name — it is a stable identifier, not a
			// display string, so a rebrand costs a domain here and nothing else.
			_ when IsHost(host, "twitter.com")
				|| IsHost(host, "x.com") => IdentityProviderType.Twitter,

			// Slack
			_ when IsHost(host, "slack.com") => IdentityProviderType.Slack,

			// Default case for unknown providers
			_ => IdentityProviderType.Unknown
		};
	}

	/// <summary>
	/// Resolves the most stable user identifier from the principal's claims.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Resolution order (first non-blank wins — a present-but-blank claim is treated as absent
	/// and does not shadow a populated claim further down the order):
	/// </para>
	/// <list type="number">
	///   <item><description><c>oid</c> — Entra ID object identifier (tenant-wide, stable across apps).</description></item>
	///   <item><description><c>http://schemas.microsoft.com/identity/claims/objectidentifier</c> — long-form Entra OID claim.</description></item>
	///   <item><description><c>sub</c> — OIDC subject identifier. May be pairwise (per-app) with some providers.</description></item>
	///   <item><description><c>http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier</c> —
	///     ASP.NET's <see cref="ClaimTypes.NameIdentifier"/>. The OIDC middleware maps <c>sub</c>
	///     to this URI by default when <c>MapInboundClaims</c> is enabled.</description></item>
	///   <item><description><c>user_id</c> — Firebase / custom IdP fallback.</description></item>
	/// </list>
	/// <para>
	/// For Entra-backed applications, <c>oid</c> is preferred over <c>sub</c> because Entra's
	/// subject claim can vary per application (pairwise), while <c>oid</c> is consistent across
	/// all applications within the same tenant.
	/// </para>
	/// </remarks>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The resolved identifier, or <see langword="null"/> if no matching claim carries a value.</returns>
	public static string? ResolveId(ClaimsPrincipal principal) =>
		// Resolved from the primary identity or not at all. A principal-wide search walks claim
		// *types* in priority order across *identities*, so a secondary identity's higher-priority
		// claim would outrank the primary's lower-priority one — returning an identifier for a
		// different subject than the name, tenant, and issuer resolved alongside it. A missing id
		// is safer than a coherent-looking profile assembled from two subjects.
		principal.Identity is ClaimsIdentity identity ? ResolveId(identity) : null;

	/// <inheritdoc cref="ResolveId(ClaimsPrincipal)"/>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	public static string? ResolveId(ClaimsIdentity identity) =>
		FirstNonBlank(identity, "oid", EntraClaimTypes.ObjectId, "sub", ClaimTypes.NameIdentifier, "user_id");

	/// <summary>
	/// Attempt to determine the User Name of the principal.
	/// </summary>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The Name if resolved otherwise null.</returns>
	public static string? ResolveName(ClaimsPrincipal principal) {

		if (principal.Identity is ClaimsIdentity identity) {
			return ResolveName(identity);
		}

		return principal.Identity?.Name.NullIfWhiteSpace();

	}
	/// <summary>
	/// Attempt to determine the User Name of the identity.
	/// </summary>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	/// <returns>The Name if resolved otherwise null.</returns>
	public static string? ResolveName(ClaimsIdentity identity) {

		// The literal "name" leads: it is the standard OIDC claim, so renaming NameClaimType must
		// not demote it. Identity.Name follows — it is virtual, so a derived identity (WindowsIdentity
		// and friends) can answer with a name that is backed by no claim at all, and for every other
		// identity it already resolves the configured NameClaimType. The last rung is therefore
		// DefaultNameClaimType, not NameClaimType: repeating the configured type would re-ask the
		// question Identity.Name just answered, whereas the classic ClaimTypes.Name URI still catches
		// principals minted elsewhere — WS-Fed, cookie auth, or a handler that left inbound claim
		// mapping enabled.
		return FirstNonBlank(identity, "name")
			?? identity.Name.NullIfWhiteSpace()
			?? FirstNonBlank(identity, ClaimsIdentity.DefaultNameClaimType);

	}


	/// <summary>
	/// Attempt to determine the Roles of the principal.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Roles are the one aggregate among these resolvers, so breadth is a real choice rather than a
	/// mistake: unlike an id or an issuer — which describe a single subject and must never be
	/// borrowed from another authentication context — a union across identities is meaningful, and
	/// it mirrors <see cref="ClaimsPrincipal.IsInRole(string)"/>, which returns true when
	/// <em>any</em> identity holds the role. That is why <paramref name="scope"/> exists here and
	/// nowhere else.
	/// </para>
	/// <para>
	/// Not equivalent to <see cref="ClaimsPrincipal.IsInRole(string)"/>, and deliberately broader:
	/// this is a universal helper that cannot know how the principal it was handed was composed, so
	/// it recognizes <c>role</c> and <c>roles</c> even when an identity's
	/// <see cref="ClaimsIdentity.RoleClaimType"/> is something else. It can therefore report a role
	/// <see cref="ClaimsPrincipal.IsInRole(string)"/> denies. It describes what a token carries, for
	/// display and diagnostics. Authorize with the authorization stack, not with this.
	/// </para>
	/// </remarks>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <param name="scope">Which identities to read. Defaults to <see cref="IdentityScope.AllIdentities"/>.</param>
	/// <returns>The Roles.</returns>
	public static List<string> ResolveRoles(
		ClaimsPrincipal principal,
		IdentityScope scope = IdentityScope.AllIdentities) {

		if (scope == IdentityScope.PrimaryIdentity && principal.Identity is ClaimsIdentity primary) {
			return ResolveRoles(primary);
		}

		// Collect into a HashSet for uniqueness, then return a List
		var set = new HashSet<string>(RoleComparer);

		if (principal.Identities.Any()) {
			foreach (var id in principal.Identities.OfType<ClaimsIdentity>()) {
				AddRolesFromIdentity(id, set);
			}
		} else {
			// Fallback: look for common role claim names on the principal directly
			foreach (var c in principal.Claims) {
				if (IsRoleClaim(c, roleClaimType: null) && c.Value.HasValue()) {
					set.Add(c.Value);
				}
			}
		}

		return [.. set];

	}
	/// <summary>
	/// Attempt to determine the Roles of the identity.
	/// </summary>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	/// <returns>The Roles.</returns>
	public static List<string> ResolveRoles(ClaimsIdentity identity) {
		var set = new HashSet<string>(RoleComparer);
		AddRolesFromIdentity(identity, set);
		return [.. set];
	}

	// Use OrdinalIgnoreCase if that's your policy across the app
	private static readonly StringComparer RoleComparer = StringComparer.OrdinalIgnoreCase;
	private static void AddRolesFromIdentity(ClaimsIdentity identity, HashSet<string> sink) {
		var roleClaimType = identity.RoleClaimType; // often ClaimTypes.Role
		foreach (var c in identity.Claims) {
			// A blank role claim is not a role. Admitting one would put an empty string in the set,
			// where it reads as a granted role to anything enumerating them and matches an equally
			// empty policy requirement.
			if (IsRoleClaim(c, roleClaimType) && c.Value.HasValue()) {
				sink.Add(c.Value);
			}
		}
	}
	private static bool IsRoleClaim(Claim c, string? roleClaimType)
		=> c.Type == roleClaimType
		|| c.Type == "roles"
		|| c.Type == "role";

	/// <summary>
	/// Attempt to determine the Oid of the principal.
	/// </summary>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The Oid if resolved otheriwise null.</returns>
	public static string? ResolveOid(ClaimsPrincipal principal) {

		if (principal.Identity is ClaimsIdentity identity) {
			return ResolveOid(identity);
		}

		return null;

	}
	/// <summary>
	/// Attempt to determine the Oid of the identity.
	/// </summary>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	/// <returns>The Oid if resolved otheriwise null.</returns>
	public static string? ResolveOid(ClaimsIdentity identity) =>
		FirstNonBlank(identity, "oid", EntraClaimTypes.ObjectId);

	/// <summary>
	/// Attempt to determine the Tid of the principal.
	/// </summary>
	/// <param name="principal">The <see cref="ClaimsPrincipal"/> to evaluate.</param>
	/// <returns>The Tid if resolved otheriwise null.</returns>
	public static string? ResolveTid(ClaimsPrincipal principal) {

		if (principal.Identity is ClaimsIdentity identity) {
			return ResolveTid(identity);
		}

		return null;

	}
	/// <summary>
	/// Attempt to determine the Tid of the identity.
	/// </summary>
	/// <param name="identity">The <see cref="ClaimsIdentity"/> to evaluate.</param>
	/// <returns>The Tid if resolved otheriwise null.</returns>
	public static string? ResolveTid(ClaimsIdentity identity) =>
		FirstNonBlank(identity, "tid", EntraClaimTypes.TenantId, "org", "org_id");

}