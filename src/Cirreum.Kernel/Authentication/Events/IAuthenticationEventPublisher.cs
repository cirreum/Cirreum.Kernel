namespace Cirreum.Authentication.Events;

/// <summary>
/// Publishes authentication-track lifecycle events to the cross-cutting auth bus.
/// </summary>
/// <remarks>
/// <para>
/// Apps publish events from admin commands (e.g., "disable user", "revoke API key",
/// "force logout") via this contract. The framework consumes via registered
/// <see cref="IAuthenticationEventHandler{TEvent}"/> instances — typically cache
/// invalidators in <c>Cirreum.Runtime.AuthenticationProvider</c> and the
/// connection-terminator handler in <c>Cirreum.Runtime.Server</c>.
/// </para>
/// <para>
/// Implementations register in the runtime composition packages. The default
/// in-process implementation dispatches synchronously to all registered handlers;
/// distributed implementations (typically backed by
/// <c>IDistributedTransportPublisher&lt;IAuthenticationEvent&gt;</c> from
/// <c>Cirreum.Messaging.Distributed</c>) fan out to handlers across heads on a dedicated
/// auth-events channel.
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
