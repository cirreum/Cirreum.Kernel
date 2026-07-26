namespace Cirreum.Conductor;

/// <summary>
/// Handles a specific <see cref="IDomainEvent"/> type. Multiple handlers may register for the same
/// event — the publisher fans out to all of them.
/// </summary>
/// <typeparam name="TDomainEvent">The domain-event type this handler responds to. Constrained to
/// <see cref="IDomainEvent"/>.</typeparam>
/// <remarks>
/// <para>
/// Handling a domain event means <b>reacting inside the application</b> — invalidating a cache,
/// updating a projection, forwarding onward to a broker. Presenting something to a person is one
/// possible reaction, not the default one, and an application that wants it writes that step
/// explicitly against the notification state family.
/// </para>
/// <para>
/// Lives in <c>Cirreum.Kernel</c> alongside <see cref="IDomainEvent"/> so event families
/// (distributed messages, authentication events, and others) can be handled uniformly by
/// Conductor's publisher — which lives in <c>Cirreum.Contracts</c> — without forcing references
/// between sibling packages.
/// </para>
/// </remarks>
public interface IDomainEventHandler<in TDomainEvent>
	where TDomainEvent : IDomainEvent {

	/// <summary>
	/// Handles the domain event asynchronously.
	/// </summary>
	/// <param name="domainEvent">The domain event instance to handle.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	Task HandleAsync(
		TDomainEvent domainEvent,
		CancellationToken cancellationToken = default);

}
