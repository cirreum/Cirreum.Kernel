namespace Cirreum.Security;

/// <summary>
/// Convenience reads over <see cref="IUserState"/>.
/// </summary>
/// <remarks>
/// Extension members rather than interface or base-class members: they are pure derivations of
/// values the contract already exposes, so defining them once here keeps a single rule while
/// leaving them visible through an interface reference and a concrete one alike. Nothing
/// implementing <see cref="IUserState"/> can drift from — or has to restate — them.
/// </remarks>
public static class UserStateExtensions {

	extension(IUserState user) {

		/// <summary>
		/// Whether the caller is known to be a person.
		/// </summary>
		/// <remarks>
		/// <see langword="false"/> for <see cref="SubjectKind.Unknown"/> as well as
		/// <see cref="SubjectKind.Machine"/> — this asks whether the caller is <em>known</em> to be
		/// a person, not whether they are merely not a machine. Guard a people-only operation with
		/// <c>!IsHumanSubject</c>, which denies an unclassified caller; <c>IsMachineSubject</c>
		/// admits one.
		/// </remarks>
		public bool IsHumanSubject => user.SubjectKind is SubjectKind.Human;

		/// <summary>
		/// Whether the caller is known to be an application or service acting on its own behalf.
		/// </summary>
		/// <remarks>
		/// <see langword="false"/> for <see cref="SubjectKind.Unknown"/> as well as
		/// <see cref="SubjectKind.Human"/>. Use it to <em>add</em> machine-specific behavior —
		/// naming the calling client in a log line, say — never to gate access, since an
		/// unclassified caller reads as not-a-machine here.
		/// </remarks>
		public bool IsMachineSubject => user.SubjectKind is SubjectKind.Machine;

	}

}
