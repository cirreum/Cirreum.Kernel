namespace Cirreum.Kernel.Tests.Security;

using Cirreum.Security;
using System.Security.Claims;

/// <summary>
/// Guards the claim resolvers against blank claim values.
/// </summary>
/// <remarks>
/// Every resolver returns <see langword="null"/> when nothing resolves, and callers coalesce that
/// null to their own default — <c>ResolveName(principal) ?? "unknown"</c> in the
/// <see cref="UserProfile"/> constructor being the canonical case. A present-but-blank claim
/// therefore has to be treated as absent on two counts: it must not escape as a non-null string
/// (which would defeat the caller's default), and it must not shadow a populated claim further
/// down the resolution order. These previously failed both ways — each resolver guarded its rungs
/// with <c>HasValue()</c> and then returned the last assigned value anyway.
/// </remarks>
public class ClaimsHelperTests {

	private const string SchemaUriName = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
	private const string EntraObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";

	private static ClaimsIdentity IdentityWith(
		string nameType = "name",
		string roleType = "roles",
		params Claim[] claims) =>
		new(claims, authenticationType: "test", nameType: nameType, roleType: roleType);

	// -------------------------------------------------------------------------
	// ResolveName
	// -------------------------------------------------------------------------

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t")]
	public void ResolveName_returns_null_for_a_blank_name_claim(string blank) {
		// The regression: all three rungs correctly rejected the blank, then the trailing
		// `return name` handed it back anyway — non-null, so `?? "unknown"` never fired.
		var identity = IdentityWith(claims: new Claim("name", blank));

		ClaimsHelper.ResolveName(identity).Should().BeNull();
	}

	[Fact]
	public void ResolveName_does_not_let_a_blank_name_claim_shadow_the_schema_uri_claim() {
		var identity = IdentityWith(
			nameType: "preferred_username",
			claims: [new Claim("name", "  "), new Claim(SchemaUriName, "Glen Banta")]);

		ClaimsHelper.ResolveName(identity).Should().Be("Glen Banta");
	}

	[Fact]
	public void ResolveName_resolves_the_schema_uri_claim_when_the_name_claim_type_was_renamed() {
		// The last rung is DefaultNameClaimType, not NameClaimType. Repeating the configured type
		// would re-ask what Identity.Name already answered; the classic URI is what still catches
		// principals minted by WS-Fed, cookie auth, or a handler with inbound mapping left on.
		var identity = IdentityWith(
			nameType: "preferred_username",
			claims: new Claim(SchemaUriName, "Glen Banta"));

		ClaimsHelper.ResolveName(identity).Should().Be("Glen Banta");
	}

	[Fact]
	public void ResolveName_prefers_the_standard_name_claim_over_a_renamed_claim_type() {
		// Renaming NameClaimType must not demote OIDC's standard claim.
		var identity = IdentityWith(
			nameType: "preferred_username",
			claims: [new Claim("name", "Glen Banta"), new Claim("preferred_username", "glen@example.com")]);

		ClaimsHelper.ResolveName(identity).Should().Be("Glen Banta");
	}

	[Fact]
	public void ResolveName_falls_back_to_the_configured_name_claim_type() {
		var identity = IdentityWith(
			nameType: "preferred_username",
			claims: new Claim("preferred_username", "glen@example.com"));

		ClaimsHelper.ResolveName(identity).Should().Be("glen@example.com");
	}

	// -------------------------------------------------------------------------
	// ResolveId / ResolveOid / ResolveTid
	// -------------------------------------------------------------------------

	[Fact]
	public void ResolveId_returns_null_when_every_identifier_claim_is_blank() {
		var identity = IdentityWith(claims: [new Claim("oid", " "), new Claim("sub", "")]);

		ClaimsHelper.ResolveId(identity).Should().BeNull();
	}

	[Fact]
	public void ResolveId_does_not_let_a_blank_oid_shadow_a_populated_sub() {
		var identity = IdentityWith(claims: [new Claim("oid", "  "), new Claim("sub", "user-123")]);

		ClaimsHelper.ResolveId(identity).Should().Be("user-123");
	}

	[Fact]
	public void ResolveOid_returns_null_when_every_object_id_claim_is_blank() {
		// Both rungs blank on purpose: the fallthrough only escaped when the *last* rung held a
		// blank value, so a case where only the first is blank passes either way and proves nothing.
		var identity = IdentityWith(claims: [new Claim("oid", "  "), new Claim(EntraObjectId, " ")]);

		ClaimsHelper.ResolveOid(identity).Should().BeNull();
	}

	[Fact]
	public void ResolveOid_does_not_let_a_blank_oid_shadow_the_long_form_claim() {
		var identity = IdentityWith(claims: [new Claim("oid", "  "), new Claim(EntraObjectId, "oid-123")]);

		ClaimsHelper.ResolveOid(identity).Should().Be("oid-123");
	}

	[Fact]
	public void ResolveTid_returns_null_when_every_tenant_claim_is_blank() {
		var identity = IdentityWith(claims: [new Claim("tid", "  "), new Claim("org_id", " ")]);

		ClaimsHelper.ResolveTid(identity).Should().BeNull();
	}

	[Fact]
	public void ResolveTid_does_not_let_a_blank_tid_shadow_a_populated_org_id() {
		// The tenant identifier draws the multi-tenant boundary — a blank one must never win, and
		// must never masquerade as a resolved value to a caller coalescing it to a default.
		var identity = IdentityWith(claims: [new Claim("tid", "  "), new Claim("org_id", "tenant-a")]);

		ClaimsHelper.ResolveTid(identity).Should().Be("tenant-a");
	}

	// -------------------------------------------------------------------------
	// ResolveRoles
	// -------------------------------------------------------------------------

	[Fact]
	public void ResolveId_takes_the_primary_identity_over_a_higher_priority_claim_elsewhere() {
		// The resolver walks claim *types* in priority order while FindFirst walks identities, so
		// an unscoped lookup returns the secondary's oid (higher priority) over the primary's sub —
		// an identifier for a different subject than the name and issuer resolved beside it.
		var primary = new ClaimsIdentity(
			[new Claim("sub", "user-A")], authenticationType: "primary", nameType: "name", roleType: "roles");
		var secondary = new ClaimsIdentity(
			[new Claim("oid", "user-B")], authenticationType: "secondary", nameType: "name", roleType: "roles");

		var principal = new ClaimsPrincipal([primary, secondary]);

		ClaimsHelper.ResolveId(principal).Should().Be("user-A");
	}

	[Fact]
	public void ResolveId_returns_null_rather_than_borrowing_from_a_secondary_identity() {
		// A missing id is safer than one taken from another authentication context.
		var primary = new ClaimsIdentity(
			[new Claim("name", "Glen Banta")], authenticationType: "primary", nameType: "name", roleType: "roles");
		var secondary = new ClaimsIdentity(
			[new Claim("sub", "user-B")], authenticationType: "secondary", nameType: "name", roleType: "roles");

		var principal = new ClaimsPrincipal([primary, secondary]);

		ClaimsHelper.ResolveId(principal).Should().BeNull();
	}

	// -------------------------------------------------------------------------
	// ResolveIssuer
	// -------------------------------------------------------------------------

	[Fact]
	public void ResolveIssuer_returns_the_iss_claim_verbatim() {
		// Verbatim matters: the issuer is not normalized, parsed, or classified. It is the one
		// identity-provider signal that reads identically wherever the profile is built.
		const string issuer = "https://console.descope.com/v1/apps/P3Cm1mP1ZDf2VoHXXYtPfFdgawbc";
		var identity = IdentityWith(claims: new Claim("iss", issuer));

		ClaimsHelper.ResolveIssuer(identity).Should().Be(issuer);
	}

	[Fact]
	public void ResolveIssuer_returns_null_when_the_claim_is_blank() {
		var identity = IdentityWith(claims: new Claim("iss", "  "));

		ClaimsHelper.ResolveIssuer(identity).Should().BeNull();
	}

	[Fact]
	public void ResolveIssuer_does_not_read_a_secondary_identity() {
		var primary = new ClaimsIdentity(
			[new Claim("sub", "user-1")], authenticationType: "primary", nameType: "name", roleType: "roles");
		var secondary = new ClaimsIdentity(
			[new Claim("iss", "https://accounts.google.com")],
			authenticationType: "secondary", nameType: "name", roleType: "roles");

		var principal = new ClaimsPrincipal([primary, secondary]);

		ClaimsHelper.ResolveIssuer(principal).Should().BeNull();
	}

	[Fact]
	public void UserProfile_keeps_the_issuer() {
		const string issuer = "https://console.descope.com/v1/apps/P3Cm1mP1ZDf2VoHXXYtPfFdgawbc";
		var identity = IdentityWith(claims: [new Claim("sub", "user-1"), new Claim("iss", issuer)]);

		var profile = new UserProfile(new ClaimsPrincipal(identity), TimeZoneInfo.Utc.Id);

		profile.Issuer.Should().Be(issuer);
	}

	// -------------------------------------------------------------------------
	// Roles — scoped versus effective
	// -------------------------------------------------------------------------

	private static ClaimsPrincipal TwoIdentityPrincipal() {
		var primary = new ClaimsIdentity(
			[new Claim("roles", "admin")], authenticationType: "primary", nameType: "name", roleType: "roles");
		var secondary = new ClaimsIdentity(
			[new Claim("roles", "auditor")], authenticationType: "secondary", nameType: "name", roleType: "roles");
		return new ClaimsPrincipal([primary, secondary]);
	}

	[Fact]
	public void ResolveRoles_unions_every_identity_by_default() {
		// The default matches ClaimsPrincipal.IsInRole, which returns true when any identity holds
		// the role — the least surprising answer for a helper that cannot know how the principal it
		// was handed was composed.
		ClaimsHelper.ResolveRoles(TwoIdentityPrincipal()).Should().BeEquivalentTo(["admin", "auditor"]);
	}

	[Fact]
	public void ResolveRoles_can_be_scoped_to_the_primary_identity() {
		ClaimsHelper.ResolveRoles(TwoIdentityPrincipal(), IdentityScope.PrimaryIdentity)
			.Should().BeEquivalentTo(["admin"]);
	}

	[Fact]
	public void Role_resolution_is_broader_than_IsInRole_by_design() {
		// "roles" is recognized even though this identity's RoleClaimType is ClaimTypes.Role, so
		// the resolved set can contain a role IsInRole denies. Fine for reporting what a token
		// carries; not a substitute for the authorization stack.
		var identity = new ClaimsIdentity(
			[new Claim("roles", "admin")],
			authenticationType: "test", nameType: "name", roleType: ClaimTypes.Role);
		var principal = new ClaimsPrincipal(identity);

		principal.IsInRole("admin").Should().BeFalse();
		ClaimsHelper.ResolveRoles(principal).Should().BeEquivalentTo(["admin"]);
	}

	[Fact]
	public void ResolveRoles_excludes_blank_role_claims() {
		var identity = IdentityWith(claims: [
			new Claim("roles", "admin"),
			new Claim("roles", "  "),
			new Claim("roles", ""),
			new Claim("roles", "member")
		]);

		ClaimsHelper.ResolveRoles(identity).Should().BeEquivalentTo(["admin", "member"]);
	}

	// -------------------------------------------------------------------------
	// The payoff at the caller
	// -------------------------------------------------------------------------

	[Fact]
	public void UserProfile_Name_falls_back_to_its_default_when_the_name_claim_is_blank() {
		// End to end: the resolver returning null is what lets the constructor's `?? "unknown"`
		// fire. Previously the blank escaped and became UserProfile.Name verbatim, reaching logs
		// and audit records as whitespace.
		var identity = IdentityWith(claims: [new Claim("sub", "user-123"), new Claim("name", "   ")]);

		var profile = new UserProfile(new ClaimsPrincipal(identity), TimeZoneInfo.Utc.Id);

		profile.Name.Should().Be("unknown");
	}

}
