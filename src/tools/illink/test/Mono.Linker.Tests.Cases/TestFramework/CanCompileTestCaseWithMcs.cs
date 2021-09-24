using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[TestCaseRequirements (TestRunCharacteristics.TestFrameworkSupportsMcs, "A test framework that supports using mcs is required")]
	[SetupCSharpCompilerToUse ("mcs")]

	// Use all of the compiler setup attributes so that we can verify they all work
	// when roslyn is used
	[SetupCompileArgument ("/debug:pdbonly")]
	[SetupCompileResource ("Dependencies/CanCompileTestCaseWithMcs.txt")]
	[Define ("VERIFY_DEFINE_WORKS")]
	[Reference ("System.dll")]

	[SetupCompileBefore ("library.dll", new[] { "Dependencies/CanCompileTestCaseWithMcs_Lib.cs" }, compilerToUse: "mcs")]

	[KeptResource ("CanCompileTestCaseWithMcs.txt")]
	[KeptMemberInAssembly ("library.dll", typeof (CanCompileTestCaseWithMcs_Lib), "Used()")]
	class CanCompileTestCaseWithMcs
	{
		static void Main ()
		{
#if VERIFY_DEFINE_WORKS
			UsedByDefine ();
#endif
			// Use something from System.dll so that we can verify the reference attribute works
			var timer = new System.Timers.Timer ();

			CanCompileTestCaseWithMcs_Lib.Used ();
		}

		[Kept]
		static void UsedByDefine ()
		{
		}
	}
}
