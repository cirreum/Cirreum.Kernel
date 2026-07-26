namespace Cirreum;

using System.Diagnostics;

/// <summary>
/// Provides shared context and configuration keys for the Cirreum domain framework.
/// </summary>
public static class DomainContext {

	/// <summary>
	/// The key used to store the resolved <see cref="DomainRuntimeType"/> in the
	/// <see cref="Microsoft.Extensions.Hosting.IHostApplicationBuilder.Properties"/> dictionary
	/// during application configuration and setup.
	/// </summary>
	public const string RuntimeTypeKey = "Cirreum:DomainRuntimeType";

	private static bool _initialized = false;

	internal static void Initialize(IDomainEnvironment domainEnvironment) {
		if (!_initialized) {
			_initialized = true;
			Environment = domainEnvironment.EnvironmentName;
			RuntimeType = domainEnvironment.RuntimeType;
			EntryPointActivityKind = ResolveActivityKind(domainEnvironment.RuntimeType);
		}
	}

	/// <summary>
	/// The environment name resolved at framework initialization (e.g., "Development",
	/// "Production"). Read by higher layers (e.g., <c>OperationContext</c> in
	/// Cirreum.Contracts) for diagnostic context. Set internally via <see cref="Initialize"/>.
	/// </summary>
	public static string Environment { get; private set; } = "Development";

	/// <summary>
	/// The <see cref="DomainRuntimeType"/> the host bootstrapped under (WebApi, BlazorWasm,
	/// Function, etc.). Read by higher layers for runtime-aware behavior. Set internally
	/// via <see cref="Initialize"/>.
	/// </summary>
	public static DomainRuntimeType RuntimeType { get; private set; } = DomainRuntimeType.WebApi;

	/// <summary>
	/// The OpenTelemetry <see cref="ActivityKind"/> derived from <see cref="RuntimeType"/> at
	/// framework initialization — for spans representing the point where work <em>enters</em> this
	/// host, and only those.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Use this only for an entry-point span.</b> <see cref="ActivityKind"/> describes the role a
	/// single span plays in a trace, not the role of the process emitting it: one host emits
	/// <see cref="ActivityKind.Server"/> for the request it handles, then
	/// <see cref="ActivityKind.Client"/> for the call it makes downstream, then
	/// <see cref="ActivityKind.Producer"/> for the message it publishes — all in the same request.
	/// A backend reconstructs topology by pairing a <see cref="ActivityKind.Client"/> span with the
	/// <see cref="ActivityKind.Server"/> span it called, so a wrong kind does not merely mislabel a
	/// span, it draws the wrong graph.
	/// </para>
	/// <para>
	/// This property answers a different question — <em>what kind of host is this</em> — and the two
	/// coincide at exactly one span per request: the one where work arrives. Conductor's operation
	/// dispatch and notification publish are those spans, which is why they are its only consumers.
	/// </para>
	/// <para>
	/// <b>Everywhere else, state the intrinsic kind.</b> A span's role does not change with the host:
	/// an outbound HTTP call is <see cref="ActivityKind.Client"/> whether it originates from a web
	/// API, a function, or a WebAssembly client; publishing to a broker is
	/// <see cref="ActivityKind.Producer"/>; consuming from one is
	/// <see cref="ActivityKind.Consumer"/>; startup and in-process work is
	/// <see cref="ActivityKind.Internal"/>. Using this property for those would, on a server host,
	/// mark an outbound call as <see cref="ActivityKind.Server"/> — a span claiming to receive a
	/// request it is actually making.
	/// </para>
	/// <para>
	/// When adding telemetry to a track that has none, the question to ask is not "which kind does
	/// this host use" but "does this span receive work, send work, or neither." Pass the kind
	/// explicitly even when it is <see cref="ActivityKind.Internal"/> — the default is the same, but
	/// stating it records that the choice was made.
	/// </para>
	/// </remarks>
	public static ActivityKind EntryPointActivityKind { get; private set; } = ActivityKind.Internal;

	private static ActivityKind ResolveActivityKind(DomainRuntimeType runtimeType) {
		return runtimeType switch {

			// Client applications - user-facing interfaces
			DomainRuntimeType.BlazorWasm => ActivityKind.Client,
			DomainRuntimeType.MauiHybrid => ActivityKind.Client,
			DomainRuntimeType.Console => ActivityKind.Client,

			// Server applications - handle incoming requests
			DomainRuntimeType.WebApi => ActivityKind.Server,
			DomainRuntimeType.WebApp => ActivityKind.Server,

			// Internal/background processing
			DomainRuntimeType.Function => ActivityKind.Internal,
			DomainRuntimeType.UnitTest => ActivityKind.Internal,

			_ => ActivityKind.Internal
		};
	}

}