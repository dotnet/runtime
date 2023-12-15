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
			TestTypeCheckRemoved_4 (null);
			TestTypeCheckRemoved_5 (null);
			TestTypeCheckRemoved_6 (null);

			TestTypeCheckKept_1 ();
			TestTypeCheckKept_2<string> (null);
			TestTypeCheckKept_3 ();
			TestTypeCheckKept_4 (null);

			TypeCheckRemovalInExceptionFilter.Test ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"pop",
			"ldnull",
			"ldnull",
			"cgt.un",
			"call System.Void System.Console::WriteLine(System.Boolean)",
			"ret"
		})]
		static void TestTypeCheckRemoved_1 (object o)
		{
			Console.WriteLine (o is T1);
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"box T",
			"pop",
			"ldnull",
			"ldnull",
			"cgt.un",
			"call System.Void System.Console::WriteLine(System.Boolean)",
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
			"brtrue.s il_6",
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
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"pop",
			"ldnull",
			"brfalse.s il_6",
			"ret",
			"call System.Void Mono.Linker.Tests.Cases.Advanced.TypeCheckRemoval/T6::Call()",
			"ret"
		})]
		static void TestTypeCheckRemoved_4 (object o1)
		{
			if (o1 is T6)
				return;

			T6.Call ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"pop",
			"ldnull",
			"ret"
		})]
		static object TestTypeCheckRemoved_5 (object o1)
		{
			object o = o1 as T7;
			return o;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"pop",
			"ldnull",
			"dup",
			"brtrue.s il_a",
			"pop",
			"ldnull",
			"br.s il_f",
			"ldfld System.Object Mono.Linker.Tests.Cases.Advanced.TypeCheckRemoval/T8::Instance",
			"pop",
			"ldnull",
			"brfalse.s il_15",
			"ldc.i4.1",
			"ret",
			"ldc.i4.2",
			"ret"
		})]
		static int TestTypeCheckRemoved_6 (object o)
		{
			if ((o as T8)?.Instance is T8) {
				return 1;
			}

			return 2;
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

		[Kept]
		class T6
		{
			public T6 ()
			{
			}

			[Kept]
			public static void Call ()
			{

			}
		}

		class T7
		{
		}

		[Kept]
		class T8
		{
			[Kept]
			public object Instance;
		}

		[Kept]
		class TypeCheckRemovalInExceptionFilter
		{
			[Kept]
			[KeptBaseType (typeof (Exception))]
			class TypeToCheckException : Exception
			{
				[Kept]
				public int Value;
			}

			[Kept]
			[ExpectedInstructionSequence (new string[] {
				".try",
				"ldarg.0",
				"pop",
				"ldnull",
				"pop",
				"leave.s il_1f",
				".endtry",
				".filter",
				"pop",
				"ldnull",
				"dup",
				"brtrue.s il_f",
				"pop",
				"ldc.i4.0",
				"br.s il_1a",
				"ldfld System.Int32 Mono.Linker.Tests.Cases.Advanced.TypeCheckRemoval/TypeCheckRemovalInExceptionFilter/TypeToCheckException::Value",
				"ldc.i4.0",
				"ceq",
				"ldc.i4.0",
				"cgt.un",
				"endfilter",
				".catch",
				"pop",
				"leave.s il_1f",
				".endcatch",
				"ret"
			})]
			static void MethodWithFilterRemovalInTry (object o)
			{
				try {
					if (o is TypeToCheckException) {
					}
				} catch (TypeToCheckException ex) when (ex.Value == 0) {
				}
			}

			[Kept]
			[ExpectedInstructionSequence (new string[] {
				".try",
				"newobj System.Void System.Object::.ctor()",
				"pop",
				"leave.s il_3a",
				".endtry",
				".filter",
				"pop",
				"ldnull",
				"dup",
				"brtrue.s il_11",
				"pop",
				"ldc.i4.0",
				"br.s il_1c",
				"ldfld System.Int32 Mono.Linker.Tests.Cases.Advanced.TypeCheckRemoval/TypeCheckRemovalInExceptionFilter/TypeToCheckException::Value",
				"ldc.i4.0",
				"ceq",
				"ldc.i4.0",
				"cgt.un",
				"endfilter",
				".catch",
				"pop",
				"leave.s il_3a",
				".endcatch",
				".filter",
				"pop",
				"ldnull",
				"dup",
				"brtrue.s il_2a",
				"pop",
				"ldc.i4.0",
				"br.s il_35",
				"ldfld System.Int32 Mono.Linker.Tests.Cases.Advanced.TypeCheckRemoval/TypeCheckRemovalInExceptionFilter/TypeToCheckException::Value",
				"ldc.i4.1",
				"ceq",
				"ldc.i4.0",
				"cgt.un",
				"endfilter",
				".catch",
				"pop",
				"leave.s il_3a",
				".endcatch",
				"ret",
			})]
			static void MethodWithTwoFilters ()
			{
				try {
					new object ();
				} catch (TypeToCheckException ex) when (ex.Value == 0) {
				} catch (TypeToCheckException ex) when (ex.Value == 1) {
				}
			}

			[Kept]
			public static void Test ()
			{
				MethodWithFilterRemovalInTry (null);
				MethodWithTwoFilters ();
			}
		}
	}
}