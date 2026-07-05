namespace Cirreum.Authentication.Events;

/// <summary>
/// Marker for an <see cref="IAuthenticationEventHandler{TEvent}"/> that forwards events to a
/// cross-replica transport, rather than acting on the event itself.
/// </summary>
/// <remarks>
/// <para>
/// The in-process publisher dispatches ordinary consumer handlers first (each isolated — a
/// throwing handler doesn't stop the rest), then any handler implementing this marker last,
/// so a bridge only ever ships an event whose local effects have already applied.
/// </para>
/// <para>
/// On the receiving side, the inbound subscriber dispatches to every local
/// <see cref="IAuthenticationEventHandler{TEvent}"/> <em>except</em> handlers implementing
/// this marker — excluding the bridge from inbound dispatch is what makes a publish-receive
/// loop structurally impossible, not a runtime check.
/// </para>
/// <para>
/// Handlers should not publish new events from within
/// <see cref="IAuthenticationEventHandler{TEvent}.HandleAsync"/>; a bridge additionally
/// refuses to forward while dispatch is running inside an inbound-receive scope, as a second
/// line of defense against wire re-entry.
/// </para>
/// </remarks>
public interface IAuthenticationEventTransportBridge;
