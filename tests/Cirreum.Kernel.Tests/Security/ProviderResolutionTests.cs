namespace Cirreum.Kernel.Tests.Security;

using Cirreum.Security;
using System.Security.Claims;

/// <summary>
/// Pins the issuer-to-provider classification.
/// </summary>
/// <remarks>
/// The table is drifting third-party data, so these read as a specification of what each provider
/// actually issues rather than as coverage of the code. Most assert a case the previous substring
/// implementation got wrong: Entra v1.0 and Azure AD B2C went unrecognized, modern Keycloak went
/// unrecognized, every AWS-hosted issuer was claimed as Cognito, and an unrelated host ending in a
/// known domain was accepted as that provider.
/// </remarks>
public class ProviderResolutionTests {

	private static IdentityProviderType Resolve(string issuer) => ClaimsHelper.ResolveProvider(issuer);

	// -------------------------------------------------------------------------
	// Microsoft
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData("https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0")]
	[InlineData("https://sts.windows.net/9188040d-6c67-4c5b-b112-36a304b66dad/")]
	[InlineData("https://login.windows.net/9188040d-6c67-4c5b-b112-36a304b66dad/")]
	[InlineData("https://login.microsoft.com/9188040d-6c67-4c5b-b112-36a304b66dad/")]
	public void Entra_is_recognized_including_its_v1_and_legacy_issuers(string issuer) {
		// sts.windows.net is the v1.0 issuer — every v1 access token carried it and resolved to
		// Unknown, which is the most common real token this table used to miss.
		Resolve(issuer).Should().Be(IdentityProviderType.Entra);
	}

	[Theory]
	[InlineData("https://contoso.ciamlogin.com/contoso.onmicrosoft.com/v2.0")]
	[InlineData("https://contoso.b2clogin.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0/")]
	public void External_id_is_recognized_including_b2c(string issuer) {
		Resolve(issuer).Should().Be(IdentityProviderType.EntraExt);
	}

	[Fact]
	public void Legacy_b2c_on_the_shared_entra_host_is_told_apart_by_its_policy_segment() {
		// Same host as plain Entra; only the /tfp/ segment distinguishes it, so a host-only table
		// silently files this under Entra.
		Resolve("https://login.microsoftonline.com/tfp/contoso.onmicrosoft.com/b2c_1_signup/v2.0/")
			.Should().Be(IdentityProviderType.EntraExt);
	}

	// -------------------------------------------------------------------------
	// Keycloak — path-shaped, self-hosted on arbitrary domains
	// -------------------------------------------------------------------------

	[Fact]
	public void Keycloak_is_recognized_on_the_modern_realm_path() {
		// Keycloak 17 dropped the /auth prefix with the Quarkus distribution; matching only
		// /auth/realms/ recognized nothing released since.
		Resolve("https://id.example.com/realms/production").Should().Be(IdentityProviderType.Keycloak);
	}

	[Fact]
	public void Keycloak_is_still_recognized_on_the_legacy_auth_realm_path() {
		Resolve("https://id.example.com/auth/realms/production").Should().Be(IdentityProviderType.Keycloak);
	}

	// -------------------------------------------------------------------------
	// AWS
	// -------------------------------------------------------------------------

	[Fact]
	public void Cognito_is_recognized_by_its_subdomain() {
		Resolve("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_abc123")
			.Should().Be(IdentityProviderType.AWS_Cognito);
	}

	[Fact]
	public void An_unrelated_provider_hosted_on_aws_is_not_claimed_as_cognito() {
		// amazonaws.com is a hosting domain, not an identity provider. Matching it made every
		// self-hosted IdP behind an AWS load balancer report as Cognito.
		Resolve("https://auth.example.us-east-1.elb.amazonaws.com/oauth2")
			.Should().Be(IdentityProviderType.Unknown);
	}

	[Fact]
	public void A_host_merely_named_after_cognito_is_not_cognito() {
		// The mirror of the case above: keying on the subdomain alone accepts any host someone
		// chooses to name cognito-idp.something. Both ends of the name are required.
		Resolve("https://cognito-idp.attacker.example/us-east-1_abc123")
			.Should().Be(IdentityProviderType.Unknown);
	}

	// -------------------------------------------------------------------------
	// Query and fragment cannot carry a discriminator
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData("https://unrelated.example/?redirect=/realms/demo")]
	[InlineData("https://unrelated.example/#/realms/demo")]
	[InlineData("https://unrelated.example/?next=/tfp/contoso/policy")]
	public void Query_and_fragment_text_cannot_impersonate_a_path_discriminator(string issuer) {
		// The path is searched for provider markers, so anything after ? or # has to be discarded
		// before that search — otherwise attacker-chosen text decides the classification.
		Resolve(issuer).Should().Be(IdentityProviderType.Unknown);
	}

	[Fact]
	public void A_query_string_does_not_prevent_a_genuine_path_match() {
		Resolve("https://id.example.com/realms/production?foo=bar")
			.Should().Be(IdentityProviderType.Keycloak);
	}

	// -------------------------------------------------------------------------
	// Anchoring
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData("https://github.com.example.invalid/login")]
	[InlineData("https://notreallyokta.com/oauth2/default")]
	[InlineData("https://evil-descope.com/p/abc")]
	public void A_host_that_merely_contains_a_known_domain_is_not_that_provider(string issuer) {
		Resolve(issuer).Should().Be(IdentityProviderType.Unknown);
	}

	[Theory]
	[InlineData("https://github.com/login/oauth", IdentityProviderType.GitHub)]
	[InlineData("https://dev-12345.okta.com/oauth2/default", IdentityProviderType.Okta)]
	[InlineData("https://api.descope.com/P2abc123", IdentityProviderType.Descope)]
	public void A_host_that_is_or_is_under_a_known_domain_is_that_provider(string issuer, IdentityProviderType expected) {
		Resolve(issuer).Should().Be(expected);
	}

	// -------------------------------------------------------------------------
	// Issuer shapes
	// -------------------------------------------------------------------------

	[Fact]
	public void An_issuer_without_a_scheme_is_still_classified() {
		// Google's issuer appears both with and without the scheme depending on the token.
		Resolve("accounts.google.com").Should().Be(IdentityProviderType.Google);
	}

	[Fact]
	public void A_port_does_not_prevent_a_match() {
		Resolve("https://id.example.com:8443/realms/production").Should().Be(IdentityProviderType.Keycloak);
	}

	[Fact]
	public void Case_does_not_prevent_a_match() {
		Resolve("HTTPS://Login.MicrosoftOnline.com/tenant/v2.0").Should().Be(IdentityProviderType.Entra);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void A_blank_issuer_resolves_to_unknown(string? issuer) {
		ClaimsHelper.ResolveProvider(issuer).Should().Be(IdentityProviderType.Unknown);
	}

	[Fact]
	public void X_is_recognized_alongside_the_twitter_domain() {
		// The enum member keeps its name — it is an identifier, not a display string — so a
		// rebrand only ever adds a domain here.
		Resolve("https://x.com").Should().Be(IdentityProviderType.Twitter);
		Resolve("https://twitter.com").Should().Be(IdentityProviderType.Twitter);
	}

	// -------------------------------------------------------------------------
	// UserProfile carries the fact
	// -------------------------------------------------------------------------

	[Fact]
	public void UserProfile_keeps_the_issuer_verbatim() {
		// The classification can drift with the table and differ between independently deployed
		// assemblies; the issuer is the value that cannot.
		const string issuer = "https://api.descope.com/P2abc123";
		var identity = new ClaimsIdentity(
			[new Claim("sub", "user-1"), new Claim("iss", issuer)],
			authenticationType: "test", nameType: "name", roleType: "roles");

		var profile = new UserProfile(new ClaimsPrincipal(identity), TimeZoneInfo.Utc.Id);

		profile.Issuer.Should().Be(issuer);
		profile.Provider.Should().Be(IdentityProviderType.Descope);
	}

	// -------------------------------------------------------------------------
	// Multi-identity principals
	// -------------------------------------------------------------------------

	[Fact]
	public void A_secondary_identity_does_not_supply_the_issuer() {
		// ClaimsPrincipal.FindFirst walks every identity in order. Resolution has to name the
		// identity being classified, or a principal carrying a second identity reports the other
		// one's provider.
		var primary = new ClaimsIdentity(
			[new Claim("iss", "https://api.descope.com/P2abc123")],
			authenticationType: "primary", nameType: "name", roleType: "roles");
		var secondary = new ClaimsIdentity(
			[new Claim("iss", "https://accounts.google.com")],
			authenticationType: "secondary", nameType: "name", roleType: "roles");

		var principal = new ClaimsPrincipal([primary, secondary]);

		ClaimsHelper.ResolveIssuer(principal).Should().Be("https://api.descope.com/P2abc123");
		ClaimsHelper.ResolveProvider(principal).Should().Be(IdentityProviderType.Descope);
	}

	[Fact]
	public void An_anonymous_marker_on_a_secondary_identity_does_not_make_the_principal_anonymous() {
		var primary = new ClaimsIdentity(
			[new Claim("iss", "https://api.descope.com/P2abc123")],
			authenticationType: "primary", nameType: "name", roleType: "roles");
		var secondary = new ClaimsIdentity(
			[new Claim(ClaimTypes.Anonymous, "true")],
			authenticationType: "secondary", nameType: "name", roleType: "roles");

		var principal = new ClaimsPrincipal([primary, secondary]);

		ClaimsHelper.ResolveProvider(principal).Should().Be(IdentityProviderType.Descope);
	}

	[Fact]
	public void UserProfile_keeps_the_issuer_even_when_the_provider_is_unrecognized() {
		const string issuer = "https://sso.internal.example.invalid/oidc";
		var identity = new ClaimsIdentity(
			[new Claim("sub", "user-1"), new Claim("iss", issuer)],
			authenticationType: "test", nameType: "name", roleType: "roles");

		var profile = new UserProfile(new ClaimsPrincipal(identity), TimeZoneInfo.Utc.Id);

		profile.Issuer.Should().Be(issuer);
		profile.Provider.Should().Be(IdentityProviderType.Unknown);
	}

}
