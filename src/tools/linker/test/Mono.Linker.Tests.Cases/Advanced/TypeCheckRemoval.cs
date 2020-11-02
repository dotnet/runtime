using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Advanced
{
	[SetupCompileArgument ("/optimize+")]
	class TypeCheckRemoval
	{
		public static void Main ()
		{
			TestTypeCheckRemoved_1 (null);
			TestTypeCheckRemoved_2<string> (null);
			TestTypeCheckRemoved_3 (null, null);

			TestTypeCheckKept_1 ();
			TestTypeCheckKept_2<string> (null);
			TestTypeCheckKept_3 ();
			TestTypeCheckKept_4 (null);
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"pop",
			"ldnull",
			"ldnull",
			"cgt.un",
			"call",
			"ret"
		})]
		static void TestTypeCheckRemoved_1 (object o)
		{
			Console.WriteLine (o is T1);
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"box",
			"pop",
			"ldnull",
			"ldnull",
			"cgt.un",
			"call",
			"ret"
		})]
		static void TestTypeCheckRemoved_2<T> (T o)
		{
			T local = o;
			Console.WriteLine (local is T1);
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"dup",
			"brtrue.s",
			"pop",
			"ldarg.1",
			"pop",
			"ldnull",
			"ldnull",
			"cgt.un",
			"ret"
		})]
		static bool TestTypeCheckRemoved_3 (object o1, object o2)
		{
			return (o1 ?? o2) is T4;
		}

		[Kept]
		static void TestTypeCheckKept_1 ()
		{
			object[] o = new object[] { new T2 () };
			Console.WriteLine (o[0] is T2);

			object t3 = new T3 ();
			Console.WriteLine (t3 is I1);
		}

		[Kept]
		static void TestTypeCheckKept_2<T> (object arg)
		{
			Console.WriteLine (arg is T);
			Console.WriteLine (arg is T[]);
		}

		[Kept]
		static bool TestTypeCheckKept_3 ()
		{
			object array = new T5[0];
			return array is T5[]; // Has to be true
		}

		[Kept]
		static void TestTypeCheckKept_4 (object o)
		{
			Console.WriteLine (o is I2);
		}

		class T1
		{
		}

		[Kept]
		class T2
		{
			[Kept]
			public T2 ()
			{
			}
		}

		[Kept]
		interface I1
		{
		}

		[Kept]
		[KeptInterface (typeof (I1))]
		class T3 : I1
		{
			[Kept]
			public T3 ()
			{
			}
		}

		[Kept]
		interface I2
		{
		}

		class T4
		{
		}

		[Kept]
		class T5
		{
			public T5 ()
			{
			}
		}
	}
}