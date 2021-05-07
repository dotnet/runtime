using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class TryFinallyBlocks
	{
		public static void Main ()
		{
			TestSimpleTry ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call",
			"ldc.i4.3",
			"beq.s il_8",
			"ret"
		})]
		static void TestSimpleTry ()
		{
			if (Prop != 3)
				Unreached_1 ();
		}

		[Kept]
		static int Prop {
			[Kept]
			get {
				try {
					return 3;
				} finally {

				}
			}
		}

		static void Unreached_1 ()
		{
		}
	}
}