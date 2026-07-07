namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;

public class MessageScannerTests {

	[Fact]
	public void Discover_pairs_each_clr_type_with_its_definition() {
		var logger = new ListLogger();

		var discoveries = MessageScanner<IScannerTestMessage>.Discover(logger);

		discoveries.Should().HaveCount(3);
		discoveries.Select(d => (d.Definition.Identifier, d.Definition.Version, d.ClrType))
			.Should().BeEquivalentTo([
				("kernel-tests.alpha", "1", typeof(AlphaMessage)),
				("kernel-tests.alpha", "2", typeof(AlphaMessageV2)),
				("kernel-tests.beta", "1", typeof(BetaMessage)),
			]);
	}

	[Fact]
	public void Discover_stamps_the_definition_with_the_clr_full_name() {
		var discoveries = MessageScanner<IScannerTestMessage>.Discover(new ListLogger());

		foreach (var discovery in discoveries) {
			discovery.Definition.MessageType.Should().Be(discovery.ClrType.FullName);
		}
	}

	[Fact]
	public void Discover_captures_the_public_property_schema() {
		var discoveries = MessageScanner<IScannerTestMessage>.Discover(new ListLogger());

		var alpha = discoveries.Single(d => d.ClrType == typeof(AlphaMessage));
		alpha.Definition.Schema.Select(p => p.Name)
			.Should().Contain([nameof(AlphaMessage.Name), nameof(AlphaMessage.Count)]);
	}

	[Fact]
	public void Discover_warns_about_and_excludes_an_unversioned_concrete_member() {
		var logger = new ListLogger();

		var discoveries = MessageScanner<IScannerTestMessage>.Discover(logger);

		discoveries.Should().NotContain(d => d.ClrType == typeof(UnversionedTestMessage));
		logger.Warnings.Should().ContainSingle(w =>
			w.Contains(typeof(UnversionedTestMessage).FullName!) &&
			w.Contains("[MessageVersion]"));
	}

	[Fact]
	public void Discover_excludes_an_abstract_member_without_warning() {
		var logger = new ListLogger();

		var discoveries = MessageScanner<IScannerTestMessage>.Discover(logger);

		discoveries.Should().NotContain(d => d.ClrType == typeof(AbstractTestMessage));
		logger.Warnings.Should().NotContain(w =>
			w.Contains(typeof(AbstractTestMessage).FullName!));
	}

	[Fact]
	public void Discover_keeps_the_first_of_a_duplicate_identity_and_warns() {
		var logger = new ListLogger();

		var discoveries = MessageScanner<IDuplicateTestMessage>.Discover(logger);

		discoveries.Should().ContainSingle().Which.Definition.Identifier
			.Should().Be("kernel-tests.dup");
		discoveries.Single().ClrType.Should().Match(t =>
			t == typeof(DupFirstMessage) || t == typeof(DupSecondMessage));
		logger.Warnings.Should().Contain(w => w.Contains("Duplicate message"));
	}

	[Fact]
	public void Discover_throws_for_a_null_logger() {
		var act = () => MessageScanner<IScannerTestMessage>.Discover(null!);

		act.Should().Throw<ArgumentNullException>();
	}

}
