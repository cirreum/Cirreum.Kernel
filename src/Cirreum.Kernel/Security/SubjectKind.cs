namespace Cirreum.Security;

/// <summary>
/// Whether the caller authenticated on a scheme is a person or a machine.
/// </summary>
/// <remarks>
/// <para>
/// Declared by the authentication provider that registers the scheme — a provider knows
/// whether it authenticates people (OIDC, Entra, session tickets) or machines (API keys,
/// signed requests). It is never inferred from what a token happens to contain: a missing
/// name claim means the token is thin, not that the caller is a machine.
/// </para>
/// <para>
/// Exposed on <see cref="IUserState"/>. A server resolves it per invocation from the caller's
/// scheme; a browser client declares <see cref="Human"/> outright, since a WASM session always
/// has a person at it. <see cref="Unknown"/> therefore means the framework genuinely could not
/// tell, not merely that nobody looked.
/// </para>
/// </remarks>
public enum SubjectKind {

	/// <summary>
	/// The subject kind has not been determined — the scheme was registered outside the
	/// framework's provider registrars, or the host composes no authentication.
	/// </summary>
	/// <remarks>
	/// Never a resolved answer: a scheme authenticates people or machines, so this value
	/// only ever means "not determined". Consumers test positively for <see cref="Human"/>
	/// or <see cref="Machine"/> rather than treating it as either.
	/// </remarks>
	Unknown = 0,

	/// <summary>
	/// The caller is a person. Profile claims describe that person, and a display name is
	/// expected to be resolvable from the token or the application store.
	/// </summary>
	Human,

	/// <summary>
	/// The caller is an application or service acting on its own behalf. There is no human
	/// subject, so profile claims do not apply and the caller is named after the client
	/// itself.
	/// </summary>
	Machine
}
