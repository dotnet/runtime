using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class FieldUsedViaReflection {
		public static void Main ()
		{
			TestByName ();
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) },
			typeof (FieldUsedViaReflection), nameof (FieldUsedViaReflection.field), (Type[]) null)]
		static void TestByName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField ("field");
			field.GetValue (null);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) })]
		static void TestNullName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField (null);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) })]
		static void TestEmptyName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField (string.Empty);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) })]
		static void TestNonExistingName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField ("NonExisting");
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) })]
		static void TestNullType ()
		{
			Type type = null;
			var field = type.GetField ("field");
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (FieldUsedViaReflection);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) })]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var field = type.GetField ("field");
		}


		[Kept]
		static int field;
	}
}
