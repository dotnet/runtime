using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{
	[SetupLinkerAction ("copy", "lib")]
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "Dependencies/PreserveDependencyAttribute.cs" })]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/PreserveDependencyInCopyAssembly.cs" }, new[] { "FakeSystemAssembly.dll" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	public class PreserveDependencyFromCopiedAssembly
	{
		public static void Main ()
		{
			Test ();
		}

		[Kept]
		static void Test ()
		{
			var b = new PreserveDependencyInCopyAssembly ();
		}
	}
}
