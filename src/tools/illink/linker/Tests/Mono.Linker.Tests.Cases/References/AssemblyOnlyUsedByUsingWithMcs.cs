using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References {
	[SetupLinkerAction ("copy", "copied")]
	[SetupCompileBefore ("library.dll", new [] {"Dependencies/AssemblyOnlyUsedByUsing_Lib.cs"})]
	
	// When mcs is used, `copied.dll` will not have a reference to `library.dll`
	[SetupCompileBefore ("copied.dll", new [] {"Dependencies/AssemblyOnlyUsedByUsing_Copied.cs"}, new [] {"library.dll"}, compilerToUse: "mcs")]

	// Here to assert that the test is setup correctly to copy the copied assembly.  This is an important aspect of the bug
	[KeptMemberInAssembly ("copied.dll", typeof (AssemblyOnlyUsedByUsing_Copied), "Unused()")]

	[RemovedAssembly ("library.dll")]
	[KeptReferencesInAssembly ("copied.dll", new [] {"mscorlib"})]
	public class AssemblyOnlyUsedByUsingWithMcs {
		public static void Main ()
		{
			// Use something to keep the reference at compile time
			AssemblyOnlyUsedByUsing_Copied.UsedToKeepReference ();
		}
	}
}