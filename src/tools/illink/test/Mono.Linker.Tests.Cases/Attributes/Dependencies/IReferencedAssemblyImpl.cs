using System.Runtime.InteropServices;

namespace Mono.Linker.Tests.Cases.Attributes.Dependencies
{
	[DynamicInterfaceCastableImplementation]
	public interface IReferencedAssemblyImpl : IReferencedAssembly
	{
		void Foo () { }
	}
}