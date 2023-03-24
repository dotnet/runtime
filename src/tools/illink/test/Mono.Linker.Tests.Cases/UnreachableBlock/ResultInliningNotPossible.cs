using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class ResultInliningNotPossible
	{
		public static void Main ()
		{
			Test_TypeWithStaticCtor ();
			Test_TypeWithExplicitStaticCtor ();
			Test_MethodWithRefArgument ();
			Test_MethodWithInstanceCall ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.ResultInliningNotPossible/TypeWithStaticCtor::GetResult()",
			"ldc.i4.1",
			"beq.s il_8",
			"ret",
		})]
		static void Test_TypeWithStaticCtor ()
		{
			if (TypeWithStaticCtor.GetResult () != 1) {
				NeverReached ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.ResultInliningNotPossible/TypeWithExplicitStaticCtor::GetResult()",
			"ldc.i4.1",
			"beq.s il_8",
			"ret",
		})]
		static void Test_TypeWithExplicitStaticCtor ()
		{
			if (TypeWithExplicitStaticCtor.GetResult () != 1) {
				NeverReached ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldnull",
			"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.ResultInliningNotPossible::MethodWithInstanceCall(Mono.Linker.Tests.Cases.UnreachableBlock.ResultInliningNotPossible/InstanceMethodType)",
			"ldc.i4.2",
			"beq.s il_9",
			"ret",
		})]
		static void Test_MethodWithInstanceCall ()
		{
			if (MethodWithInstanceCall (null) != 2) {
				NeverReached ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"callvirt System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.ResultInliningNotPossible/InstanceMethodType::GetResult()",
			"pop",
			"ldc.i4.2",
			"ret",
		})]
		static int MethodWithInstanceCall (InstanceMethodType imt)
		{
			if (imt.GetResult () > 0)
				return 2;

			return 1;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"stloc.0",
			"ldloca.s",
			"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.ResultInliningNotPossible::MethodWithRefArgument(System.Int32&)",
			"ldc.i4.1",
			"beq.s il_11",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.ResultInliningNotPossible::Reached()",
			"ret",
		})]
		static void Test_MethodWithRefArgument ()
		{
			int i = 0;
			if (MethodWithRefArgument (ref i) != 1) {
				Reached ();
			}
		}

		[Kept]
		static int MethodWithRefArgument (ref int arg)
		{
			arg = 1;
			return 1;
		}

		[Kept]
		[KeptMember (".cctor()")]
		class TypeWithStaticCtor
		{
			[Kept]
			static int Field = 4;

			[Kept]
			public static int GetResult ()
			{
				Inside ();
				return 1;
			}

			[Kept]
			static void Inside ()
			{
				Field = 2;
			}
		}

		[Kept]
		class TypeWithExplicitStaticCtor
		{
			[Kept]
			static TypeWithExplicitStaticCtor ()
			{
				Console.WriteLine ("Has to be called");
			}

			[Kept]
			public static int GetResult ()
			{
				return 1;
			}
		}

		[Kept]
		class InstanceMethodType
		{
			[Kept]
			public int GetResult ()
			{
				return 1;
			}
		}

		[Kept]
		static void Reached ()
		{
		}

		static void NeverReached ()
		{
		}
	}
}