namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
#if INCLUDE_REFERENCE_IMPL
	public class ImplementationLibrary
	{
		public string GetSomeValue () => null;
	}

	public class UnusedImplementationLibrary
	{
	}
#endif
}