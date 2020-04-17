using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection {
	[SetupCSharpCompilerToUse ("csc")]
	public class FieldUsedViaReflection {
		public static void Main ()
		{
			TestByName ();
			TestNameBindingFlags ();
			TestNameWrongBindingFlags ();
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
			TestIfElse (1);
			TestFieldInBaseType ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) },
			typeof (FieldUsedViaReflection), nameof (FieldUsedViaReflection.field), (Type [])null)]
		static void TestByName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField ("field");
			field.GetValue (null);
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string), typeof(BindingFlags) },
			typeof (Foo), nameof (Foo.field), (Type [])null)]
		static void TestNameBindingFlags ()
		{
			var field = typeof (Foo).GetField ("field", BindingFlags.Static);
		}

		[Kept]
		static void TestNameWrongBindingFlags ()
		{
			var field = typeof (Foo).GetField ("nonStatic", BindingFlags.Static);
		}

		[Kept]
		static void TestNullName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField (null);
		}

		[Kept]
		static void TestEmptyName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField (string.Empty);
		}

		[Kept]
		static void TestNonExistingName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField ("NonExisting");
		}

		[Kept]
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
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string)},
			typeof (IfClass), nameof (IfClass.ifField), (Type [])null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) },
			typeof (ElseClass), nameof (ElseClass.elseField), (Type [])null)]
		static void TestIfElse (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof (IfClass);
			} else {
				myType = typeof (ElseClass);
			}
			String myString;
			if (i == 1) {
				myString = "ifField";
			} else {
				myString = "elseField";
			}
			var field = myType.GetField (myString);
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetField), new Type [] { typeof (string) },
			typeof (BaseClass), nameof (BaseClass.publicFieldOnBase), (Type [])null)]
		static void TestFieldInBaseType ()
		{
			var protectedField = typeof(DerivedClass).GetField ("protectedFieldOnBase");
			var publicField = typeof(DerivedClass).GetField ("publicFieldOnBase");
		}

		[Kept]
		static int field;

		[Kept]
		private class Foo 
		{
			[Kept]
			public static int field;
			public int nonStatic;
			private static int nonKept;
		}

		[Kept]
		private class IfClass
		{
			[Kept]
			public static int ifField;
			[Kept]
			private int elseField;
			protected int nonKept;
		}

		[Kept]
		private class ElseClass
		{
			[Kept]
			public int elseField;
			[Kept]
			static string ifField;
			volatile char nonKept;
		}

		[Kept]
		class BaseClass
		{
			[Kept]
			protected int protectedFieldOnBase;
			[Kept]
			public char publicFieldOnBase;
		}

		[Kept]
		[KeptBaseType (typeof (BaseClass))]
		class DerivedClass : BaseClass
		{
		}
	}
}
