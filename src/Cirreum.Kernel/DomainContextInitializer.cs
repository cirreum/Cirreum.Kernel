namespace Cirreum;

public sealed class DomainContextInitializer(
	IDomainEnvironment domainEnvironment
) : IDomainContextInitializer {
	public void Initialize() {
		DomainContext.Initialize(domainEnvironment);
	}
}