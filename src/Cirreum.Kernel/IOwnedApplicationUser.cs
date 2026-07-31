namespace Cirreum;

/// <summary>
/// An application user that belongs to a coarse-grained owner scope (tenant/company).
/// The grant evaluator reads <see cref="IApplicationUser.IsEnabled"/> from it as the
/// disabled-user backstop; <see cref="OwnerId"/> is an identity fact for the app's own use.
/// </summary>
public interface IOwnedApplicationUser : IApplicationUser {

	/// <summary>
	/// The identifier of the tenant/company this user belongs to. May be null for
	/// globally-scoped users or during enrichment handoff.
	/// </summary>
	/// <remarks>
	/// This is an identity fact, not an access fact: it identifies the caller's home
	/// company (e.g., as a query key for the app's grant provider when resolving
	/// membership grant records), but confers no access by itself. Owner-scoped access
	/// comes exclusively from grant records.
	/// </remarks>
	string? OwnerId { get; }
}
