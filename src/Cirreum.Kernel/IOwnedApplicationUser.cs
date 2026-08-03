namespace Cirreum;

/// <summary>
/// Represents an application user associated with an owning tenant or company.
/// </summary>
public interface IOwnedApplicationUser : IApplicationUser {

	/// <summary>
	/// Gets the identifier of the tenant or company considered the user's home owner.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Applications may use this value for display, UI defaults, assigning ownership to
	/// new records, or as a lookup key when resolving grant records for the user.
	/// </para>
	/// <para>
	/// This value is ownership context, not an authorization decision. Its presence,
	/// absence, or value does not by itself grant, deny, imply, or disqualify access.
	/// Authorization remains the responsibility of the application's authorization
	/// implementation, such as one that resolves and evaluates grant records.
	/// </para>
	/// <para>
	/// The value may be <see langword="null"/> for globally scoped users or while
	/// application-user enrichment is incomplete.
	/// </para>
	/// </remarks>
	string? OwnerId { get; }
}
