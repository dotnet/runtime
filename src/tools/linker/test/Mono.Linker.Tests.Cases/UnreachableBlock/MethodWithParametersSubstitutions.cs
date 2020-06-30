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
			instance.TestMethodWithMultipleInParamsInstance ();
			// TestMethodWithOutParam ();
			TestMethodWithRefParam ();
			TestMethodWithMultipleRefParams ();
			TestMethodWithValueParamAndConstReturn_NoSubstitutions ();
			TestMethodWithVarArgs ();
			TestMethodWithParamAndVarArgs ();
		}

		static bool _isEnabledField;

		[Kept]
		[ExpectBodyModified]
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

		[Kept]
		[ExpectBodyModified]
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
		[LogDoesNotContain ("IsEnabledWithRefParam")]
		static void TestMethodWithRefParam ()
		{
			int p = 0;
			if (IsEnabledWithRefParam (ref p))
				MethodWithRefParam_Reached1 ();
			else
				MethodWithRefParam_Reached2 ();
		}

		[Kept] static void MethodWithRefParam_Reached1 () { }
		[Kept] static void MethodWithRefParam_Reached2 () { }

		static bool _isEnabledWithMultipleRefParamsField;

		[Kept]
		[ExpectBodyModified]
		static bool IsEnabledWithMultipleRefParams (int p1, ref int p2, ref TestStruct p3, string p4)
		{
			p2 = 0;
			return _isEnabledWithMultipleRefParamsField;
		}

		[Kept]
		[LogDoesNotContain ("IsEnabledWithMultipleRefParams")]
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
		[Kept] static void MethodWithMultipleRefParams_Reached2 () { }

		[Kept]
		static bool IsEnabledWithValueParamAndConstReturn_NoSubstitutions (int param)
		{
			return true;
		}

		[Kept]
		static void TestMethodWithValueParamAndConstReturn_NoSubstitutions ()
		{
			// The return value inlining for methods with params only works on explicitly substituted methods.
			// Linker will not do this implicitly.
			if (IsEnabledWithValueParamAndConstReturn_NoSubstitutions (0))
				MethodWithValueParamAndConstReturn_NoSubstitutions_Reached1 ();
			else
				MethodWithValueParamAndConstReturn_NoSubstitutions_Reached2 ();
		}

		[Kept] static void MethodWithValueParamAndConstReturn_NoSubstitutions_Reached1 () { }
		[Kept] static void MethodWithValueParamAndConstReturn_NoSubstitutions_Reached2 () { }


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


		[Kept] [KeptMember (".ctor()")] class TestClass { }
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