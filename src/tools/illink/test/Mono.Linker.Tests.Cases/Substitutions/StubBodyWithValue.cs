using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("StubBodyWithValue.xml")]
	public class StubBodyWithValue
	{
		public static void Main()
		{
			TestMethod_1 ();
			TestMethod_2 ();
			TestMethod_3 ();
			TestMethod_4 ();
			TestMethod_5 ();
			TestMethod_6 ();
			TestMethod_7 ();
			TestMethod_8 ();
			TestMethod_9 ();
			TestMethod_10 ();
			TestMethod_11 ();
			TestMethod_12 ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldstr",
				"ret",
			})]
		static string TestMethod_1 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4",
				"ret",
			})]
		static byte TestMethod_2 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4",
				"ret",
			})]
		static char TestMethod_3 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4",
				"ret"
			})]
		static sbyte TestMethod_4 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4.1",
				"ret",
			})]
		static bool TestMethod_5()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4.1",
				"ret",
			})]
		static bool TestMethod_6()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.r8",
				"ret",
			})]
		static double TestMethod_7()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4",
				"ret"
			})]
		static int TestMethod_8 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.r4",
				"ret",
			})]
		static float TestMethod_9()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i8",
				"ret",
			})]
		static ulong TestMethod_10()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i8",
				"ret",
			})]
		static long TestMethod_11 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new [] {
				"ldc.i4",
				"ret",
			})]
		static uint TestMethod_12 ()
		{
			throw new NotImplementedException ();
		}
	}
}
