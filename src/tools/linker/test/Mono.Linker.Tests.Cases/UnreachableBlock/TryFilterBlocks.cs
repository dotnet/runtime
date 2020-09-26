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
			"call",
			"brfalse.s",
			"call",
			"leave.s",
			"pop",
			"call",
			"ldc.i4.0",
			"cgt.un",
			"endfilter",
			"pop",
			"leave.s",
			"ldc.i4.2",
			"ret"
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
			"call",
			"leave.s",
			"pop",
			"call",
			"brfalse.s",
			"ldc.i4.0",
			"ldc.i4.0",
			"cgt.un",
			"endfilter",
			"pop",
			"leave.s",
			"ldc.i4.3",
			"ret"
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