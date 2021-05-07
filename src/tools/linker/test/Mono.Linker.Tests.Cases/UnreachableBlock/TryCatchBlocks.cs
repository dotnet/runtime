using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class TryCatchBlocks
	{
		public static void Main ()
		{
			TestSimpleTryUnreachable ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call",
			"ldc.i4.6",
			"beq.s il_8",
			"ldc.i4.3",
			"ret"
		})]
		[ExpectedExceptionHandlerSequence (new string[0])]
		[ExpectedLocalsSequence (new string[0])]
		static int TestSimpleTryUnreachable ()
		{
			if (Prop != 6) {
				try {
					Unreached_1 ();
					return 1;
				} catch {
					return 2;
				}
			}

			return 3;
		}

		[Kept]
		static int Prop {
			[Kept]
			get {
				return 6;
			}
		}

		static void Unreached_1 ()
		{
		}
	}
}