namespace Cirreum.Authentication.Events;

/// <summary>
/// Marker interface for authentication-track lifecycle events.
/// </summary>
/// <remarks>
/// <para>
/// All event records published through <see cref="IAuthenticationEventPublisher"/> implement
/// this marker. It serves as the <c>TBase</c> for the auth-track's versioned-message
/// registry (<c>MessageRegistryBase&lt;IAuthenticationEvent&gt;</c>), which resolves wire
/// payloads back to concrete event types for cross-replica delivery over
/// <c>Cirreum.Coordination</c>'s <c>ISignalBroadcaster</c> primitive (see
/// <c>Cirreum.Runtime.Authentication</c>).
/// </para>
/// <para>
/// Auth event records carry <c>[MessageVersion(identifier, version)]</c> from
/// <c>Cirreum.Messaging</c> (in Kernel) to participate in this registry's identifier-to-type
/// resolution.
/// </para>
/// <para>
/// Lives in <c>Cirreum.Kernel</c> so the event bus (<see cref="IAuthenticationEventPublisher"/>),
/// the auth track (<c>Cirreum.AuthenticationProvider</c>), and the server spine can all extend
/// this marker uniformly without cross-package references.
/// </para>
/// </remarks>
public interface IAuthenticationEvent {

	/// <summary>
	/// When this event occurred, in the publishing system's authority.
	/// </summary>
	/// <remarks>
	/// A wall-clock stamp, not a strict monotonic version — sufficient for a consumer
	/// reconciling against a periodic full re-sync, not a substitute for app-owned
	/// versioning where cross-producer ordering must be exact.
	/// </remarks>
	DateTimeOffset OccurredAt { get; }

}
