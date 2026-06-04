namespace Cirreum.Conductor;

/// <summary>
/// Handles a specific notification type. Multiple handlers may register for the same
/// notification — the publisher fans out to all of them.
/// </summary>
/// <typeparam name="TNotification">The notification type this handler responds to.
/// Constrained to <see cref="INotification"/>.</typeparam>
/// <remarks>
/// Lives in <c>Cirreum.Kernel</c> with <see cref="INotification"/> so notification
/// families (distributed messages, authentication events, etc.) can be handled
/// uniformly by Conductor's publisher (in <c>Cirreum.Contracts</c>) without forcing
/// references between sibling packages.
/// </remarks>
public interface INotificationHandler<in TNotification>
	where TNotification : INotification {

	/// <summary>
	/// Handles the notification asynchronously.
	/// </summary>
	/// <param name="notification">The notification instance to handle.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	Task HandleAsync(
		TNotification notification,
		CancellationToken cancellationToken = default);

}
