using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class SimpleConditionalProperty
	{
		public static void Main ()
		{
			TestProperty_int_1 ();
			TestProperty_int_2 ();
			TestProperty_int_3 ();
			TestProperty_int_4 ();
			TestProperty_bool_1 ();
			TestProperty_bool_2 ();
			TestProperty_bool_3 ();
			TestProperty_enum_1 ();
			TestProperty_null_1 ();
			TestProperty_SignedComparisons ();
			TestProperty_UnsignedComparisons ();
			TestDefaultInterface ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.3",
			"ldc.i4.3",
			"beq.s il_4",
			"ret",
			})]
		static void TestProperty_int_1 ()
		{
			if (Prop != 3)
				NeverReached_1 ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.3",
			"ldc.i4.3",
			"beq.s il_4",
			"ret"
			})]
		static void TestProperty_int_2 ()
		{
			if (3 == Prop) {

			} else {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.3",
			"ldc.i4.5",
			"ble.s il_4",
			"ldc.i4.0",
			"ret"
			})]
		[System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		static int TestProperty_int_3 ()
		{
			if (Prop > 5 && TestProperty_int_3 () == 0) {
				NeverReached_1 ();
			}

			return 0;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.3",
			"pop",
			"ldloca.s",
			"initobj System.Nullable`1<System.Int64>",
			"ldloc.0",
			"ret"
			})]
		static long? TestProperty_int_4 ()
		{
			if (Prop == 3)
				return null;

			return new Nullable<long> (1);
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"brfalse.s il_3",
			"ret"
			})]
		static void TestProperty_bool_1 ()
		{
			if (!PropBool) {

			} else {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"brfalse.s il_3",
			"ret",
			})]
		static void TestProperty_bool_2 ()
		{
			if (PropBool) {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"ldc.i4.0",
			"beq.s il_4",
			"ret"
			})]
		static void TestProperty_bool_3 ()
		{
			if (PropBool != PropBool) {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"br.s il_2",
			"ldc.i4.1",
			"pop",
			"ret",
			})]
		static void TestProperty_enum_1 ()
		{
			while (PropEnum == TestEnum.C) {
				NeverReached_1 ();
			}
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldnull",
			"brfalse.s il_3",
			"ret"
			})]
		static void TestProperty_null_1 ()
		{
			if (PropNull != null)
				NeverReached_1 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_SignedComparisons ()
		{
			if (PropInt == -10)
				NeverReached_1 ();

			if (PropInt != 10)
				NeverReached_1 ();

			if (PropInt < -10)
				NeverReached_1 ();

			if (PropInt <= -10)
				NeverReached_1 ();

			if (-10 > PropInt)
				NeverReached_1 ();

			if (-10 >= PropInt)
				NeverReached_1 ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestProperty_UnsignedComparisons ()
		{
			// This is effectively comparing 10 and 4294967286

			if (PropUInt == unchecked((uint) -10))
				NeverReached_1 ();

			if (PropUInt != (uint) 10)
				NeverReached_1 ();

			if (PropUInt > unchecked((uint) -10))
				NeverReached_1 ();

			if (PropUInt >= unchecked((uint) -10))
				NeverReached_1 ();

			if (unchecked((uint) -10) < PropUInt)
				NeverReached_1 ();

			if (unchecked((uint) -10) <= PropUInt)
				NeverReached_1 ();
		}

		static int Prop {
			get {
				int i = 3;
				return i;
			}
		}

		static bool PropBool {
			get {
				return false;
			}
		}

		static TestEnum PropEnum {
			get {
				return TestEnum.B;
			}
		}

		static string PropNull {
			get {
				return null;
			}
		}

		static int PropInt {
			get => 10;
		}

		static uint PropUInt {
			get => 10;
		}

		static void NeverReached_1 ()
		{
		}

		enum TestEnum
		{
			A = 0,
			B = 1,
			C = 2
		}

		[Kept]
		interface IDefaultInterface
		{
			[Kept]
			public void NonDefault ();

			[Kept]
			[ExpectBodyModified]
			public void Default ()
			{
				if (SimpleConditionalProperty.PropBool)
					SimpleConditionalProperty.NeverReached_1 ();
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IDefaultInterface))]
		class ImplementsDefaultInterface : IDefaultInterface
		{
			[Kept]
			[ExpectBodyModified]
			public void NonDefault ()
			{
				if (PropBool)
					NeverReached_1 ();
			}
		}

		[Kept]
		static void TestDefaultInterface ()
		{
			IDefaultInterface i = new ImplementsDefaultInterface ();
			i.NonDefault ();
			i.Default ();
		}
	}
}