namespace Cirreum.Authentication.Events;

/// <summary>
/// Publishes authentication-track lifecycle events to the cross-cutting auth bus.
/// </summary>
/// <remarks>
/// <para>
/// Apps publish events from admin commands (e.g., "disable user", "revoke API key",
/// "force logout") via this contract. The framework consumes via registered
/// <see cref="IAuthenticationEventHandler{TEvent}"/> instances — the grant-cache
/// invalidator in <c>Cirreum.Domain</c> and the connection-terminator handler in
/// <c>Cirreum.Services.Server</c>.
/// </para>
/// <para>
/// The default implementation, <c>InProcessAuthenticationEventPublisher</c> (in
/// <c>Cirreum.Runtime.Authentication</c>), dispatches synchronously to all registered
/// handlers, then to any handler implementing <see cref="IAuthenticationEventTransportBridge"/>
/// last. Registering a bridge is what turns on cross-replica delivery — the umbrella's
/// bridge forwards through <c>Cirreum.Coordination</c>'s <c>ISignalBroadcaster</c> primitive
/// (typically Redis-backed) to fan events out across replicas; absent a bridge, delivery
/// stays in-process only.
/// </para>
/// </remarks>
public interface IAuthenticationEventPublisher {

	/// <summary>
	/// Publishes an authentication-track event to all registered handlers for
	/// <typeparamref name="TEvent"/>.
	/// </summary>
	/// <typeparam name="TEvent">The event type. Must implement
	/// <see cref="IAuthenticationEvent"/>.</typeparam>
	/// <param name="evt">The event to publish.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask PublishAsync<TEvent>(TEvent evt, CancellationToken cancellationToken = default)
		where TEvent : IAuthenticationEvent;

}
