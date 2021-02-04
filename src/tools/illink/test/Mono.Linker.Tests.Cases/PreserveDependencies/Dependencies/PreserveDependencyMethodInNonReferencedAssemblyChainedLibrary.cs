using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies
{
	public class PreserveDependencyMethodInNonReferencedAssemblyChainedLibrary : PreserveDependencyMethodInNonReferencedAssemblyBase
	{
		public override string Method ()
		{
			Dependency ();
			return "Dependency";
		}

		[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.PreserveDependencyMethodInNonReferencedAssemblyBase2", "base2")]
		public static void Dependency ()
		{
		}
	}
}