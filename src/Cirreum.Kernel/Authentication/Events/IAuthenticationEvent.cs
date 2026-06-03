namespace Cirreum.Authentication.Events;

/// <summary>
/// Marker interface for authentication-track lifecycle events.
/// </summary>
/// <remarks>
/// <para>
/// All event records published through <see cref="IAuthenticationEventPublisher"/> implement
/// this marker. It serves as the <c>TBase</c> for the auth-track's versioned-message
/// registry (<c>IMessageRegistry&lt;IAuthenticationEvent&gt;</c>) and as the channel
/// discriminator for the auth-track's distributed transport publisher
/// (<c>IDistributedTransportPublisher&lt;IAuthenticationEvent&gt;</c>) — apps configure
/// the auth-event distribution channel independently of the generic
/// <c>DistributedMessage</c> channel.
/// </para>
/// <para>
/// Auth event records carry <c>[MessageVersion(identifier, version)]</c> from
/// <c>Cirreum.Messaging</c> (in Kernel) to participate in versioning + schema discovery.
/// </para>
/// <para>
/// Lives in <c>Cirreum.Kernel</c> so the event bus (<see cref="IAuthenticationEventPublisher"/>),
/// the distributed transport (<c>Cirreum.Messaging.Distributed</c>), the contracts
/// (<c>Cirreum.AuthenticationProvider</c>), and the spine (<c>Cirreum.Core.Server</c>)
/// can all extend this marker uniformly without cross-package references.
/// </para>
/// </remarks>
public interface IAuthenticationEvent;
