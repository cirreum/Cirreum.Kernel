namespace Cirreum;

using Cirreum.Diagnostics;
using OpenTelemetry;

/// <summary>
/// Extension methods for configuring Cirreum telemetry with OpenTelemetry.
/// </summary>
public static class CirreumTelemetryExtensions {

	/// <summary>
	/// Adds Cirreum instrumentation to OpenTelemetry for distributed tracing and metrics.
	/// </summary>
	/// <param name="builder">The OpenTelemetry builder.</param>
	/// <returns>The builder for chaining.</returns>
	/// <remarks>
	/// <para>
	/// This method registers all Cirreum activity sources and meters for comprehensive observability:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// <term>Conductor Operations</term>
	/// <description>Traces and metrics for request handlers, validation, authorization, and intercepts</description>
	/// </item>
	/// <item>
	/// <term>Cache Operations</term>
	/// <description>Metrics for cache hits, misses, evictions, and operation durations</description>
	/// </item>
	/// <item>
	/// <term>Remote Services</term>
	/// <description>Traces and metrics for HTTP client operations, including status codes and durations</description>
	/// </item>
	/// <item>
	/// <term>Authentication and Authorization</term>
	/// <description>Traces and metrics for the authentication pipeline and the authorization stages</description>
	/// </item>
	/// <item>
	/// <term>Identity Provisioning</term>
	/// <description>Traces and metrics for the pre-token provisioning callback — duration, outcome, and claims minted</description>
	/// </item>
	/// </list>
	/// <para>
	/// Names are registered unconditionally: a track the application does not compose simply
	/// never emits, so there is nothing to collect and no cost.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// builder.Services.AddOpenTelemetry()
	///     .ConfigureResource(resource => resource.AddService("MyApp"))
	///     .AddCirreumInstrumentation()
	///     .UseOtlpExporter();
	/// </code>
	/// </example>
	public static OpenTelemetryBuilder AddCirreum(
		this OpenTelemetryBuilder builder) {

		return builder
			.WithTracing(tracing => tracing
				.AddSource(CirreumTelemetry.ActivitySources.ConductorDispatcher)
				.AddSource(CirreumTelemetry.ActivitySources.ConductorPublisher)
				.AddSource(CirreumTelemetry.ActivitySources.RemoteServicesClient)
				.AddSource(CirreumTelemetry.ActivitySources.Authentication)
				.AddSource(CirreumTelemetry.ActivitySources.Authorization)
				.AddSource(CirreumTelemetry.ActivitySources.IdentityProvisioning))
			.WithMetrics(metrics => metrics
				.AddMeter(CirreumTelemetry.Meters.ConductorDispatcher)
				.AddMeter(CirreumTelemetry.Meters.ConductorPublisher)
				.AddMeter(CirreumTelemetry.Meters.ConductorCache)
				.AddMeter(CirreumTelemetry.Meters.RemoteServicesClient)
				.AddMeter(CirreumTelemetry.Meters.Authentication)
				.AddMeter(CirreumTelemetry.Meters.Authorization)
				.AddMeter(CirreumTelemetry.Meters.IdentityProvisioning));
	}
}