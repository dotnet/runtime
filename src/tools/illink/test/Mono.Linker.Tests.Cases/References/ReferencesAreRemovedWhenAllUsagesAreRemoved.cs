using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.References
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetFramework, "Asserts are specific to .NET Framework")]
	[SetupLinkerTrimMode ("link")]
	// Il8n & the blacklist step pollute the results with extra stuff that didn't need to be
	// preserved for this test case so we need to disable them
	[Il8n ("none")]
	// Used to give consistent test behavior when linking against .NET Framework class libs
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[Reference ("System.dll")]
	[RemovedAssembly ("System.dll")]
	[KeptReference (PlatformAssemblies.CoreLib)]
	class ReferencesAreRemovedWhenAllUsagesAreRemoved
	{
		public static void Main ()
		{
		}

		private static void Unused ()
		{
			// Use something from System.dll so that we know the input assembly was compiled with the reference
			var uri = new Uri ("w/e");
		}
	}
}