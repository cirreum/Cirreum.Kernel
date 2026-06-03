namespace Cirreum.Authentication.Events;

/// <summary>
/// Handles a specific authentication-track lifecycle event type.
/// </summary>
/// <typeparam name="TEvent">The event type this handler responds to. Must implement
/// <see cref="IAuthenticationEvent"/> — typically one of the framework-supplied records
/// (<see cref="UserAccountDisabled"/>, <see cref="CredentialRevoked"/>,
/// <see cref="SessionTerminationRequested"/>, <see cref="GrantsInvalidated"/>) or an
/// app-defined event implementing the marker.</typeparam>
/// <remarks>
/// <para>
/// One handler class may implement <c>IAuthenticationEventHandler&lt;TEvent&gt;</c> for
/// multiple event types — useful when a single concern (e.g., connection termination)
/// reacts to several related events.
/// </para>
/// <para>
/// Handlers are registered with DI by the package that owns the concern. The
/// framework's <see cref="IAuthenticationEventPublisher"/> dispatches to all registered
/// handlers for the event type.
/// </para>
/// </remarks>
public interface IAuthenticationEventHandler<in TEvent>
	where TEvent : IAuthenticationEvent {

	/// <summary>
	/// Handles the event. Implementations should be idempotent — the same event may be
	/// delivered multiple times in distributed scenarios.
	/// </summary>
	/// <param name="evt">The event payload.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask HandleAsync(TEvent evt, CancellationToken cancellationToken = default);

}
