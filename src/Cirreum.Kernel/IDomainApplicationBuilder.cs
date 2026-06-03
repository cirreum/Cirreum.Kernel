namespace Cirreum;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cross-host builder interface for Cirreum application composition.
/// </summary>
/// <remarks>
/// <para>
/// Provides access to the shared <see cref="IServiceCollection"/> and <see cref="ILoggingBuilder"/>
/// the application's setup pipeline composes against. Higher-layer feature packages
/// (e.g., <c>Cirreum.Conductor</c>, <c>Cirreum.Runtime.Authentication</c>, etc.) attach their
/// own <c>Add*</c> / <c>Configure*</c> extension methods on this interface so apps can build
/// fluent setup chains:
/// </para>
/// <code>
/// var builder = DomainApplication.CreateBuilder(args);
/// builder
///     .ConfigureConductor(opt =&gt; opt.AddIntercept&lt;MyIntercept&gt;())  // extension in Cirreum.Conductor
///     .AddIdentity(id =&gt; id.AddProvisioner&lt;...&gt;("clientA"))         // extension in Cirreum.Runtime.Identity
///     .AddAuthentication(auth =&gt; auth.AddApiKey(opt =&gt; ...))         // extension in Cirreum.Runtime.Authentication
///     .AddAuthorization(authz =&gt; authz.AddPolicy(...));               // extension in Cirreum.Runtime.Authorization
/// </code>
/// <para>
/// The interface itself stays in <c>Cirreum.Kernel</c> to remain a stable, dependency-light
/// foundation. Feature-specific configurators live in their owning packages and target this
/// interface via extension methods.
/// </para>
/// </remarks>
public interface IDomainApplicationBuilder {

	/// <summary>
	/// Gets a collection of logging providers for the application to compose. This is useful for adding new logging providers.
	/// </summary>
	ILoggingBuilder Logging { get; }

	/// <summary>
	/// Gets a collection of services for the application to compose. This is useful for adding user provided or framework provided services.
	/// </summary>
	IServiceCollection Services { get; }

}
