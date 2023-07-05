using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupLinkerAction ("copy", "lib")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/DynamicDependencyInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	public class DynamicDependencyFromCopiedAssembly
	{
		public static void Main ()
		{
			Test ();
		}

		[Kept]
		static void Test ()
		{
			var b = new DynamicDependencyInCopyAssembly ();
		}
	}
}
