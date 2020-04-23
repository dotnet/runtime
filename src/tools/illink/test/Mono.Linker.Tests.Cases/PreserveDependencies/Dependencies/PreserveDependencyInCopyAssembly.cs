using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

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
