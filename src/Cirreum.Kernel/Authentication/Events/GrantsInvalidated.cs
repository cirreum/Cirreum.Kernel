namespace Cirreum.Authentication.Events;

using Cirreum.Messaging;

/// <summary>
/// Signals that a subject's grant assignments (roles, permissions) have changed and any
/// derived caches must re-fetch from the authoritative grants store. The framework-shipped
/// <c>GrantsInvalidatedCacheHandler</c> (in <c>Cirreum.Domain</c>) calls
/// <c>IOperationGrantCacheInvalidator.InvalidateCallerAsync</c> for the subject.
/// </summary>
/// <param name="Subject">The subject whose grants have changed.</param>
/// <param name="OccurredAt">When the invalidation occurred (in the publishing
/// system's authority).</param>
/// <remarks>
/// <para>
/// Distinct from session/credential events — invalidating grants does not terminate
/// the subject's active sessions. Their existing principals remain valid; the next
/// authorization evaluation for the subject reloads grants from the store rather than
/// reading from cache.
/// </para>
/// <para>
/// The optional <see cref="AffectedRoles"/> and <see cref="AffectedPermissions"/> are
/// informational only for the framework-shipped handler, which always evicts the
/// subject's entire grant cache entry regardless of their value.
/// <c>InvalidateCallerAsync</c> and <c>InvalidateFeatureAsync</c> are independent tag axes
/// on the same cache — the latter evicts across every caller for a feature, not just this
/// subject — so scoping eviction by these fields here would risk over-evicting unrelated
/// callers. Apps with their own handlers may still use these fields for their own targeted
/// invalidation.
/// </para>
/// </remarks>
[MessageVersion("authentication.grants-invalidated", "1")]
public sealed record GrantsInvalidated(
	string Subject,
	DateTimeOffset OccurredAt
) : IAuthenticationEvent {

	/// <summary>
	/// Optional list of role names whose membership changed. Informational for the
	/// framework-shipped handler; available to app-authored handlers doing their own
	/// targeted cache invalidation.
	/// </summary>
	public IReadOnlyList<string>? AffectedRoles { get; init; }

	/// <summary>
	/// Optional list of permission names whose grant state changed. Informational for the
	/// framework-shipped handler; available to app-authored handlers doing their own
	/// targeted cache invalidation.
	/// </summary>
	public IReadOnlyList<string>? AffectedPermissions { get; init; }

	/// <summary>
	/// Optional human-readable reason for the invalidation.
	/// </summary>
	public string? Reason { get; init; }

}
