using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	/// <summary>
	/// We can't detect the using usage in the assembly.  As a result, nothing in `library` is going to be marked and that assembly will be deleted.
	/// However, because we specified an assembly action of `copy`, we do not rewrite `copied`, and it ends up with an unused reference to the removed `library`.
	/// </summary>
	[SetupLinkerAction ("copy", "copied")]

	// When csc is used, `copied.dll` will have a reference to `library.dll`
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyOnlyUsedByUsing_Lib.cs" })]
	[SetupCompileBefore ("copied.dll", new[] { "Dependencies/AssemblyOnlyUsedByUsing_UnusedUsing.cs" }, new[] { "library.dll" }, compilerToUse: "csc")]

	// Here to assert that the test is setup correctly to copy the copied assembly.  This is an important aspect of the bug
	[KeptMemberInAssembly ("copied.dll", typeof (AssemblyOnlyUsedByUsing_UnusedUsing), "Unused()")]

	// The library should be gone. The `using` statement leaves no traces in the IL so nothing in `library` will be marked
	[RemovedAssembly ("library.dll")]
	[KeptReferencesInAssembly ("copied.dll", new[] { "System.Runtime", "library" })]
	public class AssemblyOnlyUsedByUsingWithCsc
	{
		public static void Main ()
		{
			// Use something to keep the reference at compile time
			AssemblyOnlyUsedByUsing_UnusedUsing.UsedToKeepReference ();
		}
	}
}
