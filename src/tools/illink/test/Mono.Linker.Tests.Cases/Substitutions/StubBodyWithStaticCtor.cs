using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("StubBodyWithStaticCtor.xml")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class StubBodyWithStaticCtor
	{
		public static void Main ()
		{
			TestMethod_1 ();
			TestMethod_2 ();
			TestMethod_3 ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"call System.Int32 Mono.Linker.Tests.Cases.Substitutions.StubBodyWithStaticCtorImpl::TestMethod()",
				"ldc.i4.2",
				"beq.s il_8",
				"ldc.i4.3",
				"ret",
			})]
		static int TestMethod_1 ()
		{
			if (StubBodyWithStaticCtorImpl.TestMethod () != 2) {
				Console.WriteLine ();
				return 1;
			}

			return 3;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"call System.Int32 Mono.Linker.Tests.Cases.Substitutions.IntermediateClass::GetValue()",
				"ldc.i4.2",
				"beq.s il_8",
				"ldc.i4.3",
				"ret",
			})]
		static int TestMethod_2 ()
		{
			if (IntermediateClass.GetValue () != 2) {
				Console.WriteLine ();
				return 1;
			}

			return 3;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"call System.Boolean Mono.Linker.Tests.Cases.Substitutions.WrappingClass::GetValue()",
				"brfalse.s il_7",
				"ldc.i4.3",
				"ret",
			})]
		static int TestMethod_3 ()
		{
			if (WrappingClass.GetValue ()) {
				Console.WriteLine ();
				return 1;
			}

			return 3;
		}
	}

	[Kept]
	class WrappingClass
	{
		[Kept]
		public static bool GetValue ()
		{
			return Settings.TestValue ();
		}

		static class Settings
		{
			[Kept]
			static Settings ()
			{
				Console.WriteLine ();
			}

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.0",
				"ret",
			})]
			public static bool TestValue ()
			{
				throw new NotImplementedException ();
			}
		}
	}

	[Kept]
	class StubBodyWithStaticCtorImpl
	{
		[Kept]
		public static int count;

		[Kept]
		static StubBodyWithStaticCtorImpl ()
		{
			count = 100;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldc.i4 0x2",
				"ret",
			})]
		public static int TestMethod ()
		{
			++count;
			return Environment.ExitCode;
		}
	}

	[Kept]
	class IntermediateClass
	{
		[Kept]
		public static int GetValue ()
		{
			return StubBodyWithStaticCtorImpl.TestMethod ();
		}
	}
}
