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
			TryCatchInRemovedBranch.Test ();
			TryCatchInKeptBranchBeforeRemovedBranch.Test ();
		}

		class TryCatchInRemovedBranch
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.6",
				"ldc.i4.6",
				"beq.s il_4",
				"ldc.i4.3",
				"ret"
			})]
			[ExpectedLocalsSequence (new string[0])]
			[System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
			public static int Test ()
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

			static int Prop {
				get {
					return 6;
				}
			}

			static void Unreached_1 ()
			{
			}
		}

		class TryCatchInKeptBranchBeforeRemovedBranch
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.0",
				"pop",
				".try",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.TryCatchBlocks/TryCatchInKeptBranchBeforeRemovedBranch::Reached()",
				"leave.s il_11",
				".endtry",
				".catch",
				"pop",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.TryCatchBlocks/TryCatchInKeptBranchBeforeRemovedBranch::Reached_2()",
				"leave.s il_11",
				".endcatch",
				"ret",
			})]
			public static void Test ()
			{
				if (Prop == 0) {
					try { Reached (); } catch { Reached_2 (); }
				} else {
					Unreached ();
				}
			}

			static int Prop { get => 0; }

			[Kept]
			static void Reached () { }

			[Kept]
			static void Reached_2 () { }

			static void Unreached () { }
		}
	}
}