using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class TryFilterBlocks
	{
		public static void Main ()
		{
			TestUnreachableInsideTry ();
			TestUnreachableInsideFilterCondition ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			".try",
			"ldc.i4.0",
			"brfalse.s il_3",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.TryFilterBlocks::Reached_1()",
			"leave.s il_14",
			".endtry",
			".filter",
			"pop",
			"ldc.i4.0",
			"ldc.i4.0",
			"cgt.un",
			"endfilter",
			".catch",
			"pop",
			"leave.s il_14",
			".endcatch",
			"ldc.i4.2",
			"ret",
		})]
		static int TestUnreachableInsideTry ()
		{
			try {
				if (Prop)
					Unreached_1 ();

				Reached_1 ();
			} catch when (Log ()) {
			}

			return 2;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			".try",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.TryFilterBlocks::Reached_2()",
			"leave.s il_14",
			".endtry",
			".filter",
			"pop",
			"ldc.i4.0",
			"brfalse.s il_b",
			"ldc.i4.0",
			"ldc.i4.0",
			"cgt.un",
			"endfilter",
			".catch",
			"pop",
			"leave.s il_14",
			".endcatch",
			"ldc.i4.3",
			"ret",
		})]
		static int TestUnreachableInsideFilterCondition ()
		{
			try {
				Reached_2 ();
			} catch when (Log () && Unreached_2 ()) {
			}

			return 3;
		}

		static bool Prop {
			get {
				return false;
			}
		}

		static bool Log () => false;

		[Kept]
		static void Reached_1 ()
		{
		}

		[Kept]
		static void Reached_2 ()
		{
		}

		static void Unreached_1 ()
		{
		}

		static bool Unreached_2 () => true;
	}
}