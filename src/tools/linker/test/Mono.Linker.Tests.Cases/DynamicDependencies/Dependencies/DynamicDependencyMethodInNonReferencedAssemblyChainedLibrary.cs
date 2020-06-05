using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
	public class DynamicDependencyMethodInNonReferencedAssemblyChainedLibrary : DynamicDependencyMethodInNonReferencedAssemblyBase
	{
		public override string Method ()
		{
			Dependency ();
			return "Dependency";
		}

		[DynamicDependency (".ctor()", "Mono.Linker.Tests.Cases.Advanced.Dependencies.DynamicDependencyMethodInNonReferencedAssemblyBase2", "base2")]
		public static void Dependency ()
		{
		}
	}
}