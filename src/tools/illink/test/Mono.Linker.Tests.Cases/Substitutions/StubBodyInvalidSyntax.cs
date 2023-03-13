using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("StubBodyInvalidSyntax.xml")]
	public class StubBodyInvalidSyntax
	{
		public static void Main ()
		{
			new StubBodyInvalidSyntax ();

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
		}

		[Kept]
		public StubBodyInvalidSyntax ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		static sbyte TestMethod_1 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		static byte TestMethod_2 ()
		{
			return 3;
		}

		[Kept]
		static char TestMethod_3 ()
		{
			return 'a';
		}

		[Kept]
		static decimal TestMethod_4 ()
		{
			return 9.2m;
		}

		[Kept]
		static bool TestMethod_5 ()
		{
			return true;
		}

		[Kept]
		static void TestMethod_6 ()
		{
			TestMethod_5 ();
		}

		[Kept]
		static double TestMethod_7 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		static int TestMethod_8<T> (T t)
		{
			throw new NotImplementedException ();
		}

		[Kept]
		static float TestMethod_9 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		static ulong TestMethod_10 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		static long[] TestMethod_11 ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		static object TestMethod_12 ()
		{
			throw new NotImplementedException ();
		}
	}
}