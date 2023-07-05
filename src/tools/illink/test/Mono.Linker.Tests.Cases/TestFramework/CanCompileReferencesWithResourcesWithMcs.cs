using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[TestCaseRequirements (TestRunCharacteristics.TestFrameworkSupportsMcs, "A test framework that supports using mcs is required")]
	[SetupCompileBefore ("library.dll",
		new[] { "Dependencies/CanCompileReferencesWithResources_Lib1.cs" },
		resources: new object[] { "Dependencies/CanCompileReferencesWithResources_Lib1.txt" },
		compilerToUse: "mcs")]

	// Compile the same assembly again with another resource to get coverage on SetupCompileAfter
	[SetupCompileAfter ("library.dll",
		new[] { "Dependencies/CanCompileReferencesWithResources_Lib1.cs" },
		resources: new object[] { "Dependencies/CanCompileReferencesWithResources_Lib1.txt", "Dependencies/CanCompileReferencesWithResources_Lib1.log" },
		compilerToUse: "mcs")]

	[KeptResourceInAssembly ("library.dll", "CanCompileReferencesWithResources_Lib1.txt")]
	[KeptResourceInAssembly ("library.dll", "CanCompileReferencesWithResources_Lib1.log")]
	public class CanCompileReferencesWithResourcesWithMcs
	{
		public static void Main ()
		{
			// Use something so that reference isn't removed at compile time
			CanCompileReferencesWithResources_Lib1.Used ();
		}
	}
}