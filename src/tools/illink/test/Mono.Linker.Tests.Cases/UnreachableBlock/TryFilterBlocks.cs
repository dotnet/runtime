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
			"call",
			"brfalse.s il_7",
			"call",
			"leave.s il_1c",
			".endtry",
			".filter",
			"pop",
			"call",
			"ldc.i4.0",
			"cgt.un",
			"endfilter",
			".catch",
			"pop",
			"leave.s il_1c",
			".endcatch",
			"ldc.i4.2",
			"ret",
		})]
		[ExpectedExceptionHandlerSequence (new string[] { "filter" })]
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
			"call",
			"leave.s il_18",
			".endtry",
			".filter",
			"pop",
			"call",
			"brfalse.s il_f",
			"ldc.i4.0",
			"ldc.i4.0",
			"cgt.un",
			"endfilter",
			".catch",
			"pop",
			"leave.s il_18",
			".endcatch",
			"ldc.i4.3",
			"ret",
		})]
		[ExpectedExceptionHandlerSequence (new string[] { "filter" })]
		static int TestUnreachableInsideFilterCondition ()
		{
			try {
				Reached_2 ();
			} catch when (Log () && Unreached_2 ()) {
			}

			return 3;
		}

		[Kept]
		static bool Prop {
			[Kept]
			get {
				return false;
			}
		}

		[Kept]
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