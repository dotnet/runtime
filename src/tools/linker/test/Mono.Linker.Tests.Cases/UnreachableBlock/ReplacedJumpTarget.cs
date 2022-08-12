using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class ReplacedJumpTarget
	{
		public static void Main ()
		{
			Test_1 (int.Parse ("91"));
			TestRemovedLastBranch (int.Parse ("92"));
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"ldc.i4.2",
			"sub",
			"switch (il_2a, il_2c, il_2e, il_30, il_32)",
			"ldarg.0",
			"ldc.i4.s 0x32",
			"sub",
			"ldc.i4.5",
			"ble.un.s il_34",
			"ldarg.0",
			"ldc.i4.s 0x64",
			"bne.un.s il_36",
			"ldc.i4.1",
			"ret",
			"ldc.i4.2",
			"ret",
			"ldc.i4.2",
			"ret",
			"ldc.i4.2",
			"ret",
			"ldc.i4.5",
			"ret",
			"ldc.i4.5",
			"ret",
			"ldc.i4.5",
			"ret",
			"ldc.i4.2",
			"ret",
			})]
		static int Test_1 (int value)
		{
			switch (value) {
			case 100:
				return 1;
			case 2:
				return Value;
			case 3:
				return Value;
			case 4:
				return Value;
			case 5:
				return 5;
			case 6:
				return 5;
			case 50:
			case 51:
			case 52:
			case 53:
			case 54:
			case 55:
				return 5;
			}

			return 2;
		}

		static int Value => 2;

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"br.s il_3",
			"ret",
			"ldc.i4.1",
			"brtrue.s il_2",
			"ldnull",
			"throw"
			})]
		static void TestRemovedLastBranch (int param)
		{
			goto DoWork;

		ReturnFromMethod:
			return;

		DoWork:
			if (AlwaysTrue) {
				goto ReturnFromMethod;
			} else { // This branch will be removed
				DoSomething ();
				goto ReturnFromMethod;
			}
		}

		static void DoSomething ()
		{
		}

		static bool AlwaysTrue => true;
	}
}