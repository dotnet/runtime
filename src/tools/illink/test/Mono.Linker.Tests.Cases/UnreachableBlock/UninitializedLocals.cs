using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/LocalsWithoutStore.il" })]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[SkipPeVerify]
	public class UninitializedLocals
	{
		[ExpectedInstructionSequence (new[] {
			"ldnull",
			"pop",
			"call System.Object Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.ClassA::Method_2()",
			"pop",
			"ret",
		})]
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.ClassA.Method_1 ();
			Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.ClassA.Method_2 ();
#endif
		}
	}
}