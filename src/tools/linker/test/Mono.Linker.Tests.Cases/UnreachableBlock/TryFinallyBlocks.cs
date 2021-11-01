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
			TryFinallyInConstantProperty.Test ();
			TryFinallyInRemovedBranch.Test ();
			TryFinallyInKeptBranchBeforeRemovedBranch.Test ();
		}

		class TryFinallyInConstantProperty
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.TryFinallyBlocks/TryFinallyInConstantProperty::get_Prop()",
				"ldc.i4.3",
				"beq.s il_8",
				"ret"
			})]
			public static void Test ()
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

		class TryFinallyInRemovedBranch
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.TryFinallyBlocks/TryFinallyInRemovedBranch::get_Prop()",
				"pop",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.TryFinallyBlocks/TryFinallyInRemovedBranch::Reached()",
				"ret",
			})]
			public static void Test ()
			{
				if (Prop == 0) {
					Reached ();
				} else {
					try { Unreached (); } finally { Unreached_2 (); }
				}
			}

			[Kept]
			static int Prop { [Kept] get => 0; }

			[Kept]
			static void Reached () { }

			static void Unreached () { }

			static void Unreached_2 () { }
		}

		class TryFinallyInKeptBranchBeforeRemovedBranch
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.TryFinallyBlocks/TryFinallyInKeptBranchBeforeRemovedBranch::get_Prop()",
				"pop",
				".try",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.TryFinallyBlocks/TryFinallyInKeptBranchBeforeRemovedBranch::Reached()",
				"leave.s il_13",
				".endtry",
				".catch",
				"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.TryFinallyBlocks/TryFinallyInKeptBranchBeforeRemovedBranch::Reached_2()",
				"endfinally",
				".endcatch",
				"ret",
			})]
			public static void Test ()
			{
				if (Prop == 0) {
					try { Reached (); } finally { Reached_2 (); }
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