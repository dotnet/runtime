using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerSubstitutionFile ("MethodWithParametersSubstitutions.xml")]
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	class MethodWithParametersSubstitutions
	{
		[Kept] MethodWithParametersSubstitutions () { }

		public static void Main ()
		{
			var instance = new MethodWithParametersSubstitutions ();

			TestMethodWithValueParam ();
			TestMethodWithReferenceParam ();
			TestMethodWithComplexParams_1 ();
			TestMethodWithComplexParams_2 ();
			TestMethodWithComplexParams_3 (3);
			TestMethodWithComplexParams_4 ();
			TestMethodWithComplexParams_5 ();
			instance.TestMethodWithMultipleInParamsInstance ();
			// TestMethodWithOutParam ();
			TestMethodWithRefParam ();
			TestMethodWithMultipleRefParams ();
			TestMethodWithValueParamAndConstReturn_NoSubstitutions ();
			TestMethodWithVarArgs ();
			TestMethodWithParamAndVarArgs ();
		}

		static bool _isEnabledField;

		static bool IsEnabledWithValueParam (int param)
		{
			return _isEnabledField;
		}

		[Kept]
		[ExpectBodyModified]
		static void TestMethodWithValueParam ()
		{
			if (IsEnabledWithValueParam (0))
				MethodWithValueParam_Reached ();
			else
				MethodWithValueParam_NeverReached ();
		}

		[Kept] static void MethodWithValueParam_Reached () { }
		static void MethodWithValueParam_NeverReached () { }

		static bool IsEnabledWithReferenceParam (string param)
		{
			return _isEnabledField;
		}

		[Kept]
		[ExpectBodyModified]
		static void TestMethodWithReferenceParam ()
		{
			if (IsEnabledWithReferenceParam (""))
				MethodWithReferenceParam_Reached ();
			else
				MethodWithReferenceParam_NeverReached ();
		}

		[Kept] static void MethodWithReferenceParam_Reached () { }
		static void MethodWithReferenceParam_NeverReached () { }

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"nop",
			"ldc.i4.1",
			"pop",
			"ldc.i4.1",
			"ret",
		})]
		[System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		static int TestMethodWithComplexParams_1 ()
		{
			if (StaticMethod (null, null))
				return 1;
			else
				return 2;
		}

		[Kept]
		static bool StaticMethod (object o, int[] array)
		{
			return true;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.1",
			"newobj System.Void System.Nullable`1<System.Int32>::.ctor(!0)",
			"box System.Nullable`1<System.Int32>",
			"ldc.i4.0",
			"newarr System.Int32",
			"call System.Boolean Mono.Linker.Tests.Cases.UnreachableBlock.MethodWithParametersSubstitutions::StaticMethod(System.Object,System.Int32[])",
			"pop",
			"ldc.i4.1",
			"ret",
		})]
		static int TestMethodWithComplexParams_2 ()
		{
			int? v = 1;
			if (StaticMethod (v, new int[0]))
				return 1;
			else
				return 2;
		}

		[Kept]
		[ExpectBodyModified]
		static void TestMethodWithComplexParams_3 (int arg)
		{
			int value = 1;
			int? ovalue = arg;
			if (StaticMethod (arg == 11 ? 'a' : 'b', new int[] { arg, arg++, value = arg, int.Parse (new TestStruct ().ToString ()), ovalue ?? arg }))
				TestMethodWithComplexParams_3_Used ();
			else
				TestMethodWithComplexParams_3_Unused ();
		}

		[Kept] static void TestMethodWithComplexParams_3_Used () { }
		static void TestMethodWithComplexParams_3_Unused () { }

		[Kept]
		static void TestMethodWithComplexParams_4 ()
		{
			if (TestMethodWithComplexParams_4_Propagate (3))
				TestMethodWithComplexParams_4_Used ();
			else
				TestMethodWithComplexParams_4_Unused ();
		}

		[Kept]
		static bool TestMethodWithComplexParams_4_Propagate (int arg)
		{
			//
			// This body should propagate 'true' return value but
			// the current implementation does only simple stack backtracking and does not track any jumps
			// 
			return StaticMethod (arg == 11 ? new object () : new object (), null);
		}

		[Kept] static void TestMethodWithComplexParams_4_Used () { }
		[Kept] static void TestMethodWithComplexParams_4_Unused () { }

		[Kept]
		static void TestMethodWithComplexParams_5 ()
		{
			MethodWithParametersSubstitutions instance = new MethodWithParametersSubstitutions ();
			instance._TestMethodWithComplexParameters_5_field = instance;
			instance.TestMethodWithComplexParams_5_Propagate ();
		}

		[Kept]
		MethodWithParametersSubstitutions _TestMethodWithComplexParameters_5_field;

		[Kept]
		// Special crafted body which has a sequence of "ret; call <substituted instance method>;"
		// This causes the method param "unroll" logic to hit the "ret" instruction which is problematic.
		bool TestMethodWithComplexParams_5_Propagate ()
			=> _TestMethodWithComplexParameters_5_field?.TestMethodWithComplexParams_5_Substituted () ?? false;

		[Kept]
		[ExpectBodyModified]
		bool TestMethodWithComplexParams_5_Substituted () => true;

		[Kept]
		[ExpectBodyModified]
		bool IsEnabledWithMultipleInParamsInstance (int p1, string p2, TestClass p3, TestStruct p4, TestEnum p5)
		{
			return _isEnabledField;
		}

		[Kept]
		[ExpectBodyModified]
		void TestMethodWithMultipleInParamsInstance ()
		{
			if (IsEnabledWithMultipleInParamsInstance (0, "", new TestClass (), new TestStruct (), TestEnum.None))
				MethodWithMultipleInParamsInstance_Reached ();
			else
				MethodWithMultipleInParamsInstance_NeverReached ();
		}

		[Kept] void MethodWithMultipleInParamsInstance_Reached () { }
		void MethodWithMultipleInParamsInstance_NeverReached () { }

		// CodeRewriterStep actually fails when asked to rewrite method body with an out parameter.
		// So no point in testing that the substitution works or not.
		//[Kept] static bool _isEnabledWithOutParamField;

		//[Kept]
		//static bool IsEnabledWithOutParam (out int param)
		//{
		//	param = 0;
		//	return _isEnabledWithOutParamField;
		//}

		//[Kept]
		//[LogDoesNotContain("IsEnabledWithOutParam")]
		//static void TestMethodWithOutParam ()
		//{
		//	if (IsEnabledWithOutParam (out var _))
		//		MethodWithOutParam_Reached1 ();
		//	else
		//		MethodWithOutParam_Reached2 ();
		//}

		//[Kept] static void MethodWithOutParam_Reached1 () { }
		//[Kept] static void MethodWithOutParam_Reached2 () { }

		static bool _isEnabledWithRefParamField;

		[Kept]
		[ExpectBodyModified]
		static bool IsEnabledWithRefParam (ref int param)
		{
			param = 0;
			return _isEnabledWithRefParamField;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"stloc.0",
			"ldloca.s",
			"call System.Boolean Mono.Linker.Tests.Cases.UnreachableBlock.MethodWithParametersSubstitutions::IsEnabledWithRefParam(System.Int32&)",
			"pop",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.MethodWithParametersSubstitutions::MethodWithRefParam_Reached1()",
			"ret",
		})]
		static void TestMethodWithRefParam ()
		{
			int p = 0;
			if (IsEnabledWithRefParam (ref p))
				MethodWithRefParam_Reached1 ();
			else
				MethodWithRefParam_Reached2 ();
		}

		[Kept] static void MethodWithRefParam_Reached1 () { }
		static void MethodWithRefParam_Reached2 () { }

		static bool _isEnabledWithMultipleRefParamsField;

		[Kept]
		[ExpectBodyModified]
		static bool IsEnabledWithMultipleRefParams (int p1, ref int p2, ref TestStruct p3, string p4)
		{
			p2 = 0;
			return _isEnabledWithMultipleRefParamsField;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"stloc.0",
			"ldloca.s",
			"initobj Mono.Linker.Tests.Cases.UnreachableBlock.MethodWithParametersSubstitutions/TestStruct",
			"ldc.i4.0",
			"ldloca.s",
			"ldloca.s",
			"ldstr ''",
			"call System.Boolean Mono.Linker.Tests.Cases.UnreachableBlock.MethodWithParametersSubstitutions::IsEnabledWithMultipleRefParams(System.Int32,System.Int32&,Mono.Linker.Tests.Cases.UnreachableBlock.MethodWithParametersSubstitutions/TestStruct&,System.String)",
			"pop",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.MethodWithParametersSubstitutions::MethodWithMultipleRefParams_Reached1()",
			"ret",
		})]
		static void TestMethodWithMultipleRefParams ()
		{
			int p = 0;
			TestStruct p2 = new TestStruct ();
			if (IsEnabledWithMultipleRefParams (0, ref p, ref p2, ""))
				MethodWithMultipleRefParams_Reached1 ();
			else
				MethodWithMultipleRefParams_Reached2 ();
		}

		[Kept] static void MethodWithMultipleRefParams_Reached1 () { }
		static void MethodWithMultipleRefParams_Reached2 () { }

		static bool IsEnabledWithValueParamAndConstReturn_NoSubstitutions (int param)
		{
			return true;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4.1",
			"pop",
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.MethodWithParametersSubstitutions::MethodWithValueParamAndConstReturn_NoSubstitutions_Reached1()",
			"ret",
		})]
		static void TestMethodWithValueParamAndConstReturn_NoSubstitutions ()
		{
			if (IsEnabledWithValueParamAndConstReturn_NoSubstitutions (0))
				MethodWithValueParamAndConstReturn_NoSubstitutions_Reached1 ();
			else
				MethodWithValueParamAndConstReturn_NoSubstitutions_Reached2 ();
		}

		[Kept] static void MethodWithValueParamAndConstReturn_NoSubstitutions_Reached1 () { }
		static void MethodWithValueParamAndConstReturn_NoSubstitutions_Reached2 () { }


		static bool _isEnabledWithVarArgsField;

		[Kept]
		[ExpectBodyModified]
		static bool IsEnabledWithVarArgs (__arglist)
		{
			return _isEnabledWithVarArgsField;
		}

		[Kept]
		[LogDoesNotContain ("IsEnabledWithVarArgs")]
		static void TestMethodWithVarArgs ()
		{
			if (IsEnabledWithVarArgs (__arglist (1)))
				MethodWithVarArgs_Reached1 ();
			else
				MethodWithVarArgs_Reached2 ();
		}

		[Kept] static void MethodWithVarArgs_Reached1 () { }
		[Kept] static void MethodWithVarArgs_Reached2 () { }


		static bool _isEnabledWithParamAndVarArgsField;

		[Kept]
		[ExpectBodyModified]
		static bool IsEnabledWithParamAndVarArgs (int p1, __arglist)
		{
			return _isEnabledWithParamAndVarArgsField;
		}

		[Kept]
		[LogDoesNotContain ("IsEnabledWithParamAndVarArgs")]
		static void TestMethodWithParamAndVarArgs ()
		{
			if (IsEnabledWithParamAndVarArgs (1, __arglist (1)))
				MethodWithParamAndVarArgs_Reached1 ();
			else
				MethodWithParamAndVarArgs_Reached2 ();
		}

		[Kept] static void MethodWithParamAndVarArgs_Reached1 () { }
		[Kept] static void MethodWithParamAndVarArgs_Reached2 () { }


		[Kept][KeptMember (".ctor()")] class TestClass { }
		[Kept] struct TestStruct { }

		[Kept]
		[KeptMember ("value__")]
		[KeptBaseType (typeof (Enum))]
		enum TestEnum
		{
			[Kept]
			None
		}
	}
}