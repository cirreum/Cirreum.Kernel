namespace Cirreum.Conductor;

/// <summary>
/// Marker for notifications dispatched through Cirreum's publish/subscribe pipeline.
/// Notifications fan out to multiple handlers and do not return values.
/// </summary>
/// <remarks>
/// <para>
/// Lives in <c>Cirreum.Kernel</c> (the framework's foundation) so cross-cutting
/// notification families (distributed messages in <c>Cirreum.Messaging.Distributed</c>,
/// authentication events, etc.) can all extend the same
/// marker without references between sibling packages.
/// </para>
/// <para>
/// Concrete publisher and dispatcher machinery (e.g., <c>IPublisher</c>,
/// <c>Dispatcher</c>, intercepts) live in <c>Cirreum.Contracts</c> alongside the rest of
/// the Conductor pipeline.
/// </para>
/// </remarks>
public interface INotification;
