using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "Not important for .NET Core build")]
	[SetupLinkerTrimMode ("link")]
	[Reference ("System.dll")]

	[KeptAssembly (PlatformAssemblies.CoreLib)]
	// We can't check everything that should be removed, but we should be able to check a few niche things that
	// we know should be removed which will at least verify that the core library was processed
	[KeptTypeInAssembly (PlatformAssemblies.CoreLib, typeof (System.Collections.Generic.IEnumerable<>))]
	[RemovedTypeInAssembly (PlatformAssemblies.CoreLib, typeof (System.Resources.ResourceWriter))]
	[KeptAssembly ("System.dll")]
	[KeptTypeInAssembly ("System.dll", typeof (System.Collections.Generic.SortedList<,>))]
	[RemovedTypeInAssembly ("System.dll", typeof (System.Collections.Generic.SortedDictionary<,>))]
	class LinkingOfCoreLibrariesRemovesUnusedTypes
	{
		public static void Main ()
		{
			// Use something from system that would normally be removed if we didn't use it
			OtherMethods2 (new SortedList<string, string> ());
		}

		[Kept]
		static void OtherMethods2 (SortedList<string, string> list)
		{
		}
	}
}
