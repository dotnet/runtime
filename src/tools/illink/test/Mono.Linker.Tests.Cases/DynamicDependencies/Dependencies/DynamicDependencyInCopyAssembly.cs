using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
	public class DynamicDependencyInCopyAssembly
	{
		[DynamicDependency ("ExtraMethod1")]
		public DynamicDependencyInCopyAssembly ()
		{
		}

		static void ExtraMethod1 ()
		{
		}
	}
}
