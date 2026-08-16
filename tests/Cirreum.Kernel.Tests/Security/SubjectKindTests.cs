namespace Cirreum.Kernel.Tests.Security;

using Cirreum.Security;

/// <summary>
/// Pins the default-value semantics the attribute-authority model depends on.
/// </summary>
/// <remarks>
/// Every one of these guards the same rule: nothing is asserted about a scheme that declared
/// nothing. The zero values have to read as "not stated" — an enum member reordered into
/// position 0 would silently start classifying unregistered schemes, and the failure would be
/// a policy change with no error to observe.
/// </remarks>
public class SubjectKindTests {

	[Fact]
	public void Unknown_is_the_default_subject_kind() {
		default(SubjectKind).Should().Be(SubjectKind.Unknown);
	}

	[Fact]
	public void Unspecified_is_the_default_claim_authority() {
		default(ClaimAuthority).Should().Be(ClaimAuthority.Unspecified);
	}

	[Fact]
	public void An_undeclared_scheme_asserts_nothing_on_any_axis() {
		var undeclared = SchemeClaimAuthority.Undeclared;

		undeclared.SubjectKind.Should().Be(SubjectKind.Unknown);
		undeclared.Profile.Should().Be(ClaimAuthority.Unspecified);
		undeclared.Roles.Should().Be(ClaimAuthority.Unspecified);
	}

	[Fact]
	public void Undeclared_is_the_default_value_of_the_struct() {
		// Callers coalesce a missing map entry to default(SchemeClaimAuthority); it must be
		// indistinguishable from an explicit Undeclared.
		default(SchemeClaimAuthority).Should().Be(SchemeClaimAuthority.Undeclared);
	}

	[Theory]
	[InlineData(SubjectKind.Unknown, false, false)]
	[InlineData(SubjectKind.Human, true, false)]
	[InlineData(SubjectKind.Machine, false, true)]
	public void The_convenience_predicates_both_read_false_for_an_unclassified_caller(
		SubjectKind kind, bool expectedHuman, bool expectedMachine) {
		// Three states, two booleans: !IsHumanSubject and IsMachineSubject are NOT equivalent.
		// A people-only guard must use !IsHumanSubject, which denies Unknown; IsMachineSubject
		// admits it. Both predicates answer "known to be", never "not the other one".
		IUserState user = new TestUserState(kind);

		user.IsHumanSubject.Should().Be(expectedHuman);
		user.IsMachineSubject.Should().Be(expectedMachine);
	}

	[Fact]
	public void The_predicates_are_reachable_through_an_interface_reference() {
		// The consumer that motivated them — an operation authorizer — holds IUserState, never
		// a concrete user-state type.
		IUserState user = new TestUserState(SubjectKind.Machine);

		user.IsMachineSubject.Should().BeTrue();
		user.IsHumanSubject.Should().BeFalse();
	}

	[Fact]
	public void The_predicates_are_reachable_through_a_concrete_reference() {
		// Extension members on the interface reach concrete types too, which a member declared
		// only on IUserState would not. Both call sites resolve to the one definition — this is
		// the property that would be lost if they were moved back onto the interface or restated
		// on UserStateBase.
		var user = new TestUserState(SubjectKind.Human);

		user.IsHumanSubject.Should().BeTrue();
		user.IsMachineSubject.Should().BeFalse();
	}

	private sealed class TestUserState : UserStateBase {

		// The protected setter is the whole stamping surface — ServerUserState uses it per
		// invocation, ClientUser declares Human once. Both routes come through here.
		public TestUserState(SubjectKind kind) {
			this.SubjectKind = kind;
		}

		public override bool IsAuthenticationComplete => true;
	}

	[Fact]
	public void A_declared_scheme_round_trips_its_declaration() {
		var declared = new SchemeClaimAuthority(
			SubjectKind.Human,
			ClaimAuthority.IdentityProvider,
			ClaimAuthority.ApplicationStore);

		declared.SubjectKind.Should().Be(SubjectKind.Human);
		declared.Profile.Should().Be(ClaimAuthority.IdentityProvider);
		declared.Roles.Should().Be(ClaimAuthority.ApplicationStore);
		declared.Should().NotBe(SchemeClaimAuthority.Undeclared);
	}
}
