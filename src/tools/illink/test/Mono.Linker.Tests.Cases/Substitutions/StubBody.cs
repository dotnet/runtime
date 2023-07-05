using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("StubBody.xml")]
	public class StubBody
	{
		[ExpectedInstructionSequence (new[] {
			"nop",
			"newobj System.Void Mono.Linker.Tests.Cases.Substitutions.StubBody::.ctor()",
			"pop",
			"ldc.i4.5",
			"newobj System.Void Mono.Linker.Tests.Cases.Substitutions.StubBody/NestedType::.ctor(System.Int32)",
			"pop",
			"ldnull",
			"pop",
			"ldc.i4.0",
			"pop",
			"ldc.i4.0",
			"pop",
			"call System.Decimal Mono.Linker.Tests.Cases.Substitutions.StubBody::TestMethod_4()",
			"pop",
			"ldc.i4.0",
			"pop",
			"call System.Void Mono.Linker.Tests.Cases.Substitutions.StubBody::TestMethod_6()",
			"nop",
			"ldc.r8 0",
			"pop",
			"ldc.i4.5",
			"call T Mono.Linker.Tests.Cases.Substitutions.StubBody::TestMethod_8<System.Int32>(T)",
			"pop",
			"ldc.r4 0",
			"pop",
			"ldc.i8 0x0",
			"pop",
			"ldnull",
			"pop",
			"ldnull",
			"pop",
			"ldnull",
			"pop",
			"ret",
		})]
		public static void Main ()
		{
			new StubBody ();
			new NestedType (5);

			TestMethod_1 ();
			TestMethod_2 ();
			TestMethod_3 ();
			TestMethod_4 ();
			TestMethod_5 ();
			TestMethod_6 ();
			TestMethod_7 ();
			TestMethod_8 (5);
			TestMethod_9 ();
			TestMethod_10 ();
			TestMethod_11 ();
			TestMethod_12 ();
			TestMethod_13 ();
		}

		struct NestedType
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ret",
			})]
			public NestedType (int arg)
			{
				throw new NotImplementedException ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldarg.0",
				"call System.Void System.Object::.ctor()",
				"ret",
			})]
		public StubBody ()
		{
			throw new NotImplementedException ();
		}

		static string TestMethod_1 ()
		{
			throw new NotImplementedException ();
		}

		static byte TestMethod_2 ()
		{
			throw new NotImplementedException ();
		}

		static char TestMethod_3 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldloca.s",
				"initobj System.Decimal",
				"ldloc.0",
				"ret"
			})]
		[ExpectLocalsModified]
		static decimal TestMethod_4 ()
		{
			throw new NotImplementedException ();
		}

		static bool TestMethod_5 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ret",
			})]
		static void TestMethod_6 ()
		{
			TestMethod_5 ();
		}

		static double TestMethod_7 ()
		{
			double d = 1.1;
			return d;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldloca.s",
				"initobj T",
				"ldloc.0",
				"ret"
			})]
		[ExpectLocalsModified]
		static T TestMethod_8<T> (T t)
		{
			throw new NotImplementedException ();
		}

		static float TestMethod_9 ()
		{
			float f = 1.1f;
			return f;
		}

		static ulong TestMethod_10 ()
		{
			throw new NotImplementedException ();
		}

		static long[] TestMethod_11 ()
		{
			throw new NotImplementedException ();
		}

		static object TestMethod_12 ()
		{
			throw new NotImplementedException ();
		}

		static System.Collections.Generic.List<int> TestMethod_13 ()
		{
			throw new NotImplementedException ();
		}
	}
}