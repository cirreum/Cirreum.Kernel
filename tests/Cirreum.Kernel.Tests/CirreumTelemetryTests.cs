namespace Cirreum.Kernel.Tests;

using Cirreum.Diagnostics;

/// <summary>
/// Pins the telemetry names this package registers with OpenTelemetry.
/// </summary>
/// <remarks>
/// These constants are the *registration* half of a cross-package contract: the emitting
/// package creates its <c>ActivitySource</c> / <c>Meter</c> with a matching literal, and a
/// source or meter whose name is never registered is silently inert — it records into the void
/// with no listener attached, so nothing reaches an exporter no matter how much traffic flows.
/// That is exactly how identity-provisioning telemetry shipped unobservable. Renaming a value
/// here without renaming it in the emitting package reintroduces that failure, so these assert
/// the literals rather than the constants.
/// </remarks>
public class CirreumTelemetryTests {

	[Theory]
	[InlineData(CirreumTelemetry.ActivitySources.ConductorDispatcher, "Cirreum.Conductor.Dispatcher")]
	[InlineData(CirreumTelemetry.ActivitySources.ConductorPublisher, "Cirreum.Conductor.Publisher")]
	[InlineData(CirreumTelemetry.ActivitySources.RemoteServicesClient, "Cirreum.RemoteServices.Client")]
	[InlineData(CirreumTelemetry.ActivitySources.Authentication, "Cirreum.Authentication")]
	[InlineData(CirreumTelemetry.ActivitySources.Authorization, "Cirreum.Authorization")]
	[InlineData(CirreumTelemetry.ActivitySources.IdentityProvisioning, "Cirreum.Identity.Provisioning")]
	public void Activity_source_names_match_the_emitting_packages(string actual, string expected) {
		actual.Should().Be(expected);
	}

	[Theory]
	[InlineData(CirreumTelemetry.Meters.ConductorDispatcher, "Cirreum.Conductor.Dispatcher")]
	[InlineData(CirreumTelemetry.Meters.ConductorPublisher, "Cirreum.Conductor.Publisher")]
	[InlineData(CirreumTelemetry.Meters.ConductorCache, "Cirreum.Conductor.QueryCache")]
	[InlineData(CirreumTelemetry.Meters.RemoteServicesClient, "Cirreum.RemoteServices.Client")]
	[InlineData(CirreumTelemetry.Meters.Authentication, "Cirreum.Authentication")]
	[InlineData(CirreumTelemetry.Meters.Authorization, "Cirreum.Authorization")]
	[InlineData(CirreumTelemetry.Meters.IdentityProvisioning, "Cirreum.Identity.Provisioning")]
	public void Meter_names_match_the_emitting_packages(string actual, string expected) {
		actual.Should().Be(expected);
	}

	[Fact]
	public void Identity_provisioning_shares_one_name_across_its_source_and_meter() {
		// The provisioning surface is a single named source + meter pair, so a consumer
		// enables one name to observe all provisioning regardless of which adapter drove it.
		CirreumTelemetry.Meters.IdentityProvisioning
			.Should().Be(CirreumTelemetry.ActivitySources.IdentityProvisioning);
	}
}
