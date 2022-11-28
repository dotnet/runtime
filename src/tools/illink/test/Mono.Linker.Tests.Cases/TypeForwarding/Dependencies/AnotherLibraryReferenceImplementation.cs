namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
#if INCLUDE_REFERENCE_IMPL
	public class AnotherLibrary<T>
	{
		public string Prop { get; set; }
	}	
#endif
}
