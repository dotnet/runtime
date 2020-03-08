using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
//	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class SimpleConditionalProperty
	{
		public static void Main()
		{
			TestProperty_int_1 ();
			TestProperty_int_2 ();
			TestProperty_int_3 ();
			TestProperty_bool_1 ();
			TestProperty_bool_2 ();
			TestProperty_bool_3 ();
			TestProperty_enum_1 ();
			TestProperty_null_1 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_int_1 ()
		{
			if (Prop != 3)
				NeverReached_1 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_int_2 ()
		{
			if (3 == Prop) {

			} else {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static int TestProperty_int_3 ()
		{
			if (Prop > 5 && TestProperty_int_3 () == 0) {
				NeverReached_1 ();
			}

			return 0;
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_bool_1 ()
		{
			if (!PropBool) {

			} else {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_bool_2 ()
		{
			if (PropBool) {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_bool_3 ()
		{
			if (PropBool != PropBool) {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_enum_1 ()
		{
			while (PropEnum == TestEnum.C) {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_null_1 ()
		{
			if (PropNull != null)
				NeverReached_1 ();
		}

		[Kept]
		static int Prop {
			[Kept]
			get {
				int i = 3;
				return i;
			}
		}

		[Kept]
		static bool PropBool {
			[Kept]
			get {
				return false;
			}
		}

		[Kept]
		static TestEnum PropEnum {
			[Kept]
			get {
				return TestEnum.B;
			}
		}

		[Kept]
		static string PropNull {
			[Kept]
			get {
				return null;
			}
		}

		static void NeverReached_1 ()
		{			
		}

		[Kept]
		[KeptMember ("value__")]
		[KeptBaseType (typeof (Enum))]
		enum TestEnum {
			[Kept]
			A = 0,
			[Kept]
			B = 1,
			[Kept]
			C = 2
		}
	}
}