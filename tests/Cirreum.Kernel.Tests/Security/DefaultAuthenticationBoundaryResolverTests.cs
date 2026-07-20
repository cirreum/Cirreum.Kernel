namespace Cirreum.Kernel.Tests.Security;

using Cirreum.Security;

/// <summary>
/// Unit tests for <see cref="DefaultAuthenticationBoundaryResolver"/> — the blanket
/// default strategy: authenticated → Global, unauthenticated → None, scheme ignored.
/// </summary>
public class DefaultAuthenticationBoundaryResolverTests {

	[Theory]
	[InlineData("entraWorkforce")]
	[InlineData("descope")]
	[InlineData(null)]
	public void Resolve_AuthenticatedUser_IsGlobal_RegardlessOfScheme(string? scheme) {
		var resolver = new DefaultAuthenticationBoundaryResolver();

		var boundary = resolver.Resolve(new TestUserState(isAuthenticated: true), scheme);

		boundary.Should().Be(AuthenticationBoundary.Global);
	}

	[Theory]
	[InlineData("entraWorkforce")]
	[InlineData(null)]
	public void Resolve_UnauthenticatedUser_IsNone(string? scheme) {
		var resolver = new DefaultAuthenticationBoundaryResolver();

		var boundary = resolver.Resolve(new TestUserState(isAuthenticated: false), scheme);

		boundary.Should().Be(AuthenticationBoundary.None);
	}

	private sealed class TestUserState : UserStateBase {

		public TestUserState(bool isAuthenticated) {
			this._isAuthenticated = isAuthenticated;
		}

		public override bool IsAuthenticationComplete => true;

	}

}
