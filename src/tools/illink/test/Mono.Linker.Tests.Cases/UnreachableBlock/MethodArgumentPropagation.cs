using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	class MethodArgumentPropagation
	{
		public static void Main ()
		{
			TestSimpleStaticCall ();
			TestFailedAndSuccessfullPropagation ();
			TestComplexButAlwaysConstant ();
			TestModifiesArgumentOnStack ();
			TestConditionalStaticCall ();
			TestSimpleLocalVariable ();
			TestConditionalJumpIntoReplacedTarget (3);
			TestNullPropagation ();
			TestFirstLevelReduction ();
			TestConditionalArguments ();
			TestConditionalArguments_2 ();

			TestRecursionFromDeadCode ();
			TestIndirectRecursion ();
			TestStringCalls ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4 0x0",
			"brfalse.s il_8",
			"ret",
		})]
		static void TestSimpleStaticCall ()
		{
			if (StaticBool (4))
				NeverReached ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4 0x0",
			"brfalse.s il_8",
			"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.MethodArgumentPropagation::GetUnknownValue()",
			"call System.Boolean Mono.Linker.Tests.Cases.UnreachableBlock.MethodArgumentPropagation::SimpleCompare(System.Int32)",
			"brfalse.s il_19",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.MethodArgumentPropagation::Reached()",
			"ret",
		})]
		static void TestFailedAndSuccessfullPropagation ()
		{
			if (SimpleCompare (GetConstValue ()))
				NeverReached ();

			if (SimpleCompare (GetUnknownValue ()))
				Reached ();
		}

		[Kept]
		static bool SimpleCompare (int arg)
		{
			return arg == 3;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.1",
			"ldstr 'aa '",
			"call System.String System.String::Trim()",
			"ldc.i4.2",
			"newarr System.Object",
			"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.MethodArgumentPropagation::ComplexButAlwaysConstant(System.Int32,System.String,System.Object[])",
			"ldc.i4.0",
			"ble.s il_19",
			"ret",
		})]
		static void TestComplexButAlwaysConstant ()
		{
			if (ComplexButAlwaysConstant (1, "aa ".Trim (), new object[] { null, null }) > 0)
				NeverReached ();
		}

		[Kept]
		static int ComplexButAlwaysConstant (int arg, string s, object[] array)
		{
			return -1;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.3",
			"stloc.0",
			"ldloca.s",
			"call System.Int32 Mono.Linker.Tests.Cases.UnreachableBlock.MethodArgumentPropagation::ModifiesArgumentOnStack(System.Int32&)",
			"ldc.i4.1",
			"beq.s il_11",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.MethodArgumentPropagation::Reached()",
			"ret",
		})]
		static void TestModifiesArgumentOnStack ()
		{
			int value = 3;
			if (ModifiesArgumentOnStack (ref value) != 1)
				Reached ();
		}

		[Kept]
		static int ModifiesArgumentOnStack (ref int arg)
		{
			arg = 2;
			return 1;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4.s 0x9",
			"ldc.i4.1",
			"bne.un.s il_6",
			"ret",
		})]
		static void TestConditionalStaticCall ()
		{
			if (ConditionalReturn (false) == 1)
				NeverReached ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldstr 'a'",
			"call System.Void System.Console::WriteLine(System.String)",
			"ret",
		})]
		static void TestSimpleLocalVariable ()
		{
			Console.WriteLine (LocalVariableMix (int.MinValue));
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldarg.0",
			"ldc.r8 0",
			"bge.un.s il_1c",
			"ldstr 'd'",
			"ldstr 's'",
			"newobj System.Void System.ArgumentOutOfRangeException::.ctor(System.String,System.String)",
			"throw",
			"ldstr 's'",
			"call System.Void System.Console::WriteLine(System.String)",
			"ldarg.0",
			"ldc.r8 0",
			"bge.un.s il_42",
			"ldstr 'd'",
			"ldstr 's'",
			"newobj System.Void System.ArgumentOutOfRangeException::.ctor(System.String,System.String)",
			"throw",
			"ldstr 's'",
			"call System.Void System.Console::WriteLine(System.String)",
			"ret",
		})]
		static void TestConditionalJumpIntoReplacedTarget (double d)
		{
			if (d < 0)
				throw new ArgumentOutOfRangeException (nameof (d), GetString ());

			Console.WriteLine (GetString ());

			if (d < 0)
				throw new ArgumentOutOfRangeException (nameof (d), GetString ());

			Console.WriteLine (GetString ());
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldnull",
			"brfalse.s il_4",
			"ret",
		})]
		static void TestNullPropagation ()
		{
			if (GetNull (2) is not null)
				NeverReached ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4.4",
			"ldc.i4.4",
			"beq.s il_5",
			"nop",
			"ldc.i4.s 0xa",
			"call System.Void System.Console::WriteLine(System.Int32)",
			"ret",
		})]
		static void TestFirstLevelReduction ()
		{
			if (SimpleIntInOut (4) != 4)
				NeverReached ();

			Console.WriteLine (SimpleIntInOut (10));
		}

		[Kept]
		static void TestConditionalArguments ()
		{
			if (KeptIntInOut (GetUnknownValue () > 0 ? 2 : 3) != 4)
				Reached ();
		}

		[Kept]
		static void TestConditionalArguments_2 ()
		{
			if (KeptIntInOut (GetUnknownValue () > 0 ? 2 : 3, 1) != 4)
				Reached ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4.0",
			"ldc.i4.1",
			"bne.un.s il_5",
			"ret",
		})]
		static void TestRecursionFromDeadCode ()
		{
			if (RecursionFromDeadCode (3) == 1) {
				NeverReached ();
			}
		}

		static int RecursionFromDeadCode (int arg)
		{
			if (arg > 0) {
				if (StaticBool (4)) {
					return 1;
				}
			} else {
				RecursionFromDeadCode (--arg);
			}

			return 0;
		}

		[Kept]
		static int TestIndirectRecursion ()
		{
			return TestIndirectRecursion_1 ();
		}

		[Kept]
		static int TestIndirectRecursion_1 ()
		{
			return TestIndirectRecursion_2 ();
		}

		[Kept]
		static int TestIndirectRecursion_2 ()
		{
			return TestIndirectRecursion ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"nop",
			"nop",
			"ldc.i4 0x0",
			"brfalse.s il_a",
			"nop",
			"nop",
			"nop",
			"ldc.i4 0x0",
			"brfalse.s il_14",
			"ret",
		})]
		static void TestStringCalls ()
		{
			string s = GetStringValue ("s");
			if (StringsEqual (s, "v"))
				NeverReached ();

			if (StringsNotEqual (GetStringValue ("s"), "s"))
				NeverReached ();
		}

		static bool StringsEqual (string a, string b)
		{
			return a == b;
		}

		static bool StringsNotEqual (string a, string b)
		{
			return a != b;
		}

		static bool StaticBool (int arg)
		{
			return arg == 3;
		}

		static int ConditionalReturn (bool arg)
		{
			if (arg)
				return 1;

			return 9;
		}

		static string LocalVariableMix (int s)
		{
			int l = int.MinValue;
			return l == s ? "a" : "b";
		}

		static string GetString ()
		{
			return "s";
		}

		static object GetNull (int arg)
		{
			return arg > 5 ? 9 : null;
		}

		static int SimpleIntInOut (int arg)
		{
			return arg;
		}

		static int GetConstValue ()
		{
			return 5;
		}

		static string GetStringValue (string s)
		{
			return s;
		}

		[Kept]
		static int GetUnknownValue ()
		{
			return Environment.ProcessId + 10;
		}

		[Kept]
		static int KeptIntInOut (int arg)
		{
			return arg;
		}

		[Kept]
		static int KeptIntInOut (int arg, int unused)
		{
			return arg;
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
