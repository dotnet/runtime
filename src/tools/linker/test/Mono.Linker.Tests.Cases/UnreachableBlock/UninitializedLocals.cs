using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/LocalsWithoutStore.il" })]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[SkipPeVerify]
	public class UninitializedLocals
	{
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.ClassA.Method_1 ();
			Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.ClassA.Method_2 ();
#endif
		}
	}
}