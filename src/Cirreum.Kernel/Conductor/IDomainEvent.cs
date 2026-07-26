namespace Cirreum.Conductor;

/// <summary>
/// Marker for a domain event dispatched through Cirreum's publish/subscribe pipeline. Domain
/// events fan out to every registered handler and return no value.
/// </summary>
/// <remarks>
/// <para>
/// A domain event is <b>in-application communication</b> — one part of the system telling the rest
/// that something happened, so they can react. It is not a message to a person: nothing here
/// reaches a user interface unless an application deliberately writes a handler that puts it
/// there.
/// </para>
/// <para>
/// That distinction is why this is not called a notification. Cirreum reserves "notification" for
/// the human-facing concept — the state family a client binds to in order to show a person
/// something (<c>INotificationState</c>, <c>IScopedNotificationState</c>, and the WebAssembly
/// state services built on them). The two travel in opposite directions and have unrelated
/// lifetimes, and naming both "notification" left a handler's audience ambiguous at a glance.
/// </para>
/// <para>
/// Lives in <c>Cirreum.Kernel</c> (the framework's foundation) so cross-cutting event families —
/// distributed messages in <c>Cirreum.Messaging.Distributed</c>, authentication events, and others
/// — can extend the same marker without references between sibling packages. The concrete
/// publisher and dispatcher machinery (<c>IPublisher</c>, <c>Dispatcher</c>, intercepts) lives in
/// <c>Cirreum.Contracts</c> alongside the rest of the Conductor pipeline.
/// </para>
/// </remarks>
public interface IDomainEvent;
