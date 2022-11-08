using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	/// <summary>
	/// We can't detect the using usage in the assembly.  As a result, nothing in `library` is going to be marked and that assembly will be deleted.
	/// Because of that, `copied` needs to have it's reference to `library` removed even though we specified an assembly action of `copy`
	/// </summary>
	[SetupLinkerAction ("copy", "copied")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyOnlyUsedByUsing_Lib.cs" })]

	// When csc is used, `copied.dll` will have a reference to `library.dll`
	[SetupCompileBefore ("copied.dll", new[] { "Dependencies/AssemblyOnlyUsedByUsing_Copied.cs" }, new[] { "library.dll" }, compilerToUse: "csc")]

	// Here to assert that the test is setup correctly to copy the copied assembly.  This is an important aspect of the bug
	[KeptMemberInAssembly ("copied.dll", typeof (AssemblyOnlyUsedByUsing_Copied), "Unused()")]

	// We library should be gone.  The `using` statement leaves no traces in the IL so nothing in `library` will be marked
	[RemovedAssembly ("library.dll")]
	[KeptReferencesInAssembly ("copied.dll", new[] { "System.Runtime", "library" })]
	public class AssemblyOnlyUsedByUsingWithCsc
	{
		public static void Main ()
		{
			// Use something to keep the reference at compile time
			AssemblyOnlyUsedByUsing_Copied.UsedToKeepReference ();
		}
	}
}