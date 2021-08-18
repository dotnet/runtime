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
				"call",
				"ldc.i4.6",
				"beq.s il_8",
				"ldc.i4.3",
				"ret"
			})]
			[ExpectedLocalsSequence (new string[0])]
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

		class TryCatchInKeptBranchBeforeRemovedBranch
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"call",
				"pop",
				".try",
				"call",
				"leave.s il_15",
				".endtry",
				".catch",
				"pop",
				"call",
				"leave.s il_15",
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

			[Kept]
			static int Prop { [Kept] get => 0; }

			[Kept]
			static void Reached () { }

			[Kept]
			static void Reached_2 () { }

			static void Unreached () { }
		}
	}
}