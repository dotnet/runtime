namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies
{
	public class InterfaceWithInterfaceFromOtherAssemblyWhenExplicitMethodUsed_Lib
	{
		public interface IFoo
		{
			bool ExplicitMethod ();
		}
	}
}