using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[SetupCSharpCompilerToUse ("csc")]

	// Use all of the compiler setup attributes so that we can verify they all work
	// when roslyn is used
	[SetupCompileArgument ("/debug:portable")]
	[SetupCompileResource ("Dependencies/CanCompileTestCaseWithCsc.txt")]
	[Define ("VERIFY_DEFINE_WORKS")]
	[Reference ("System.dll")]

	[SetupCompileBefore ("library.dll", new[] { "Dependencies/CanCompileTestCaseWithCsc_Lib.cs" }, compilerToUse: "csc")]

	[KeptResource ("CanCompileTestCaseWithCsc.txt")]
	[KeptMemberInAssembly ("library.dll", typeof (CanCompileTestCaseWithCsc_Lib), "Used()")]
	class CanCompileTestCaseWithCsc
	{
		static void Main ()
		{
#if VERIFY_DEFINE_WORKS
			UsedByDefine ();
#endif
			// Use something from System.dll so that we can verify the reference attribute works
			var timer = new System.Timers.Timer ();

			CanCompileTestCaseWithCsc_Lib.Used ();
		}

		[Kept]
		static void UsedByDefine ()
		{
		}
	}
}
