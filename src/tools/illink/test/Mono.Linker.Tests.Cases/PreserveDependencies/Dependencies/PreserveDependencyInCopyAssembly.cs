using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies
{
	public class PreserveDependencyInCopyAssembly
	{
		[PreserveDependency ("ExtraMethod1")]
		public PreserveDependencyInCopyAssembly ()
		{
		}

		static void ExtraMethod1 ()
		{
		}
	}
}
