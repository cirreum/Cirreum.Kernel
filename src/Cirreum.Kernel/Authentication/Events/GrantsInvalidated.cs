namespace Cirreum.Authentication.Events;

using Cirreum.Messaging;

/// <summary>
/// Signals that a subject's grant assignments (roles, permissions) have changed and any
/// derived caches must re-fetch from the authoritative grants store. Consumers in
/// <c>Cirreum.Runtime.AuthorizationProvider</c> evict
/// <c>RequiredGrantCache</c> entries for the subject.
/// </summary>
/// <param name="Subject">The subject whose grants have changed.</param>
/// <param name="InvalidatedAt">When the invalidation occurred (in the publishing
/// system's authority).</param>
/// <remarks>
/// <para>
/// Distinct from session/credential events — invalidating grants does not terminate
/// the subject's active sessions. Their existing principals remain valid; the next
/// authorization evaluation for the subject reloads grants from the store rather than
/// reading from cache.
/// </para>
/// <para>
/// The optional <see cref="AffectedRoles"/> and <see cref="AffectedPermissions"/>
/// metadata allows fine-grained cache invalidation when the publisher knows what
/// specifically changed. When both are <see langword="null"/>, consumers fall back to
/// invalidating the subject's entire grant cache entry.
/// </para>
/// </remarks>
[MessageVersion("authentication.grants-invalidated", "1")]
public sealed record GrantsInvalidated(string Subject, DateTimeOffset InvalidatedAt) : IAuthenticationEvent {

	/// <summary>
	/// Optional list of role names whose membership changed. Lets consumers do
	/// targeted cache invalidation when only specific roles were added or removed.
	/// </summary>
	public IReadOnlyList<string>? AffectedRoles { get; init; }

	/// <summary>
	/// Optional list of permission names whose grant state changed. Lets consumers do
	/// targeted cache invalidation when only specific permissions were affected.
	/// </summary>
	public IReadOnlyList<string>? AffectedPermissions { get; init; }

	/// <summary>
	/// Optional human-readable reason for the invalidation.
	/// </summary>
	public string? Reason { get; init; }

}
