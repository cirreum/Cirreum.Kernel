namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;

public class MessageRegistryBaseTests {

	private sealed class TestRegistry(ILogger logger)
		: MessageRegistryBase<IScannerTestMessage>(logger) {

		public List<MessageDiscovery> HookCalls { get; } = [];
		public List<Type?> ResolvedDuringHook { get; } = [];

		public ValueTask InitializeAsync() => this.DefaultInitializationAsync();

		protected override void OnMessageDiscovered(MessageDiscovery discovery) {
			this.HookCalls.Add(discovery);
			// The contract: the base lookup maps already contain this entry.
			this.ResolvedDuringHook.Add(this.ResolveType(discovery.Definition));
		}

	}

	private static async Task<(TestRegistry Registry, ListLogger Logger)> InitializedAsync() {
		var logger = new ListLogger();
		var registry = new TestRegistry(logger);
		await registry.InitializeAsync();
		return (registry, logger);
	}

	[Fact]
	public async Task GetDefinitionFor_resolves_by_generic_and_runtime_type() {
		var (registry, _) = await InitializedAsync();

		var byGeneric = registry.GetDefinitionFor<AlphaMessage>();
#pragma warning disable CA2263 // Prefer generic overload when type is known
		var byType = registry.GetDefinitionFor(typeof(AlphaMessage));
#pragma warning restore CA2263 // Prefer generic overload when type is known

		byGeneric.Identifier.Should().Be("kernel-tests.alpha");
		byGeneric.Version.Should().Be("1");
		byType.Should().Be(byGeneric);
	}

	[Fact]
	public async Task GetDefinitionFor_throws_for_an_undiscovered_family_member() {
		var (registry, _) = await InitializedAsync();

		// Concrete family member without [MessageVersion] — assignable, never discovered.
		var act = () => registry.GetDefinitionFor(typeof(UnversionedTestMessage));

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*MessageVersion*");
	}

	[Fact]
	public async Task GetDefinitionFor_throws_for_a_type_outside_the_family() {
		var (registry, _) = await InitializedAsync();

		var act = () => registry.GetDefinitionFor(typeof(string));

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public async Task ResolveType_resolves_each_identity_to_its_concrete_type() {
		var (registry, _) = await InitializedAsync();

		registry.ResolveType("kernel-tests.alpha", "1").Should().Be<AlphaMessage>();
		registry.ResolveType("kernel-tests.alpha", "2").Should().Be<AlphaMessageV2>();
		registry.ResolveType("kernel-tests.beta", "1").Should().Be<BetaMessage>();
	}

	[Fact]
	public async Task ResolveType_returns_null_for_an_unknown_identity() {
		var (registry, _) = await InitializedAsync();

		registry.ResolveType("kernel-tests.alpha", "99").Should().BeNull();
		registry.ResolveType("kernel-tests.unknown", "1").Should().BeNull();
	}

	[Theory]
	[InlineData(null, "1")]
	[InlineData("", "1")]
	[InlineData("  ", "1")]
	[InlineData("kernel-tests.alpha", null)]
	[InlineData("kernel-tests.alpha", "")]
	[InlineData("kernel-tests.alpha", "  ")]
	public async Task ResolveType_returns_null_for_an_absent_identity_component(
		string? identifier, string? version) {
		var (registry, _) = await InitializedAsync();

		registry.ResolveType(identifier!, version!).Should().BeNull();
	}

	[Fact]
	public async Task ResolveType_definition_is_sugar_over_the_identity_pair() {
		var (registry, _) = await InitializedAsync();

		var definition = registry.GetDefinitionFor<BetaMessage>();

		registry.ResolveType(definition).Should().Be<BetaMessage>();
	}

	[Fact]
	public async Task ResolveType_throws_for_a_null_definition() {
		var (registry, _) = await InitializedAsync();

		var act = () => registry.ResolveType(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task OnMessageDiscovered_fires_once_per_discovery_with_populated_maps() {
		var (registry, _) = await InitializedAsync();

		registry.HookCalls.Should().HaveCount(3);
		registry.HookCalls.Select(d => d.ClrType).Should().BeEquivalentTo([
			typeof(AlphaMessage), typeof(AlphaMessageV2), typeof(BetaMessage),
		]);
		// Each hook invocation could already resolve its own entry through the base maps.
		registry.ResolvedDuringHook.Should().Equal(
			registry.HookCalls.Select(d => (Type?)d.ClrType));
	}

	[Fact]
	public async Task Second_initialization_warns_and_does_not_rescan() {
		var (registry, logger) = await InitializedAsync();

		await registry.InitializeAsync();

		logger.Warnings.Should().Contain(w => w.Contains("more than once"));
		registry.HookCalls.Should().HaveCount(3);
		registry.GetAll().Should().HaveCount(3);
	}

	[Fact]
	public async Task GetAll_returns_every_discovered_definition() {
		var (registry, _) = await InitializedAsync();

		registry.GetAll().Select(d => (d.Identifier, d.Version)).Should().BeEquivalentTo([
			("kernel-tests.alpha", "1"),
			("kernel-tests.alpha", "2"),
			("kernel-tests.beta", "1"),
		]);
	}

}
