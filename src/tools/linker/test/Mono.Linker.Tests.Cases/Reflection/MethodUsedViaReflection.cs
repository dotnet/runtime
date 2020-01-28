using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {

	[VerifyAllReflectionAccessPatternsAreValidated]
	public class MethodUsedViaReflection {
		public static void Main ()
		{
			TestNameAndExplicitBindingFlags ();
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string), typeof (BindingFlags) },
			typeof (MethodUsedViaReflection), nameof (MethodUsedViaReflection.OnlyCalledViaReflection), new Type [0])]
		static void TestNameAndExplicitBindingFlags ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
			method.Invoke (null, new object [] { });
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string) })]
		static void TestNullName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod (null);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string) })]
		static void TestEmptyName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod (string.Empty);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string) })]
		static void TestNonExistingName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod ("NonExisting");
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string), typeof (BindingFlags) })]
		static void TestNullType ()
		{
			Type type = null;
			var method = type.GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (MethodUsedViaReflection);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string), typeof (BindingFlags) })]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var method = type.GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
		}

		[Kept]
		private static int OnlyCalledViaReflection ()
		{
			return 42;
		}

		private int OnlyCalledViaReflection (int foo)
		{
			return 43;
		}

		public int OnlyCalledViaReflection (int foo, int bar)
		{
			return 44;
		}

		public static int OnlyCalledViaReflection (int foo, int bar, int baz)
		{
			return 45;
		}
	}
}
