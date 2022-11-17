using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	public class FieldUsedViaReflection
	{
		public static void Main ()
		{
			TestByName ();
			TestPrivateByName ();
			TestNameBindingFlags ();
			TestNameWrongBindingFlags ();
			TestNameUnknownBindingFlags (BindingFlags.Public);
			TestNameUnknownBindingFlagsAndName (BindingFlags.Public, "DoesntMatter");
			TestNullName ();
			TestNoValueName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			TestIfElse (1);
			TestFieldInBaseType ();
			TestIgnoreCaseBindingFlags ();
			TestFailIgnoreCaseBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		static void TestByName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField ("publicField");
			field.GetValue (null);
		}

		[Kept]
		static void TestPrivateByName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField ("field"); // This will not mark the field as GetField(string) only returns public fields
			field.GetValue (null);
		}

		[Kept]
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
		static void TestNameUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all fields on the type
			var field = typeof (UnknownBindingFlags).GetField ("field", bindingFlags);
		}

		[Kept]
		static void TestNameUnknownBindingFlagsAndName (BindingFlags bindingFlags, string name)
		{
			// Since the binding flags and name are not known linker should mark all fields on the type
			var field = typeof (UnknownBindingFlagsAndName).GetField (name, bindingFlags);
		}

		[Kept]
		static void TestNullName ()
		{
			var field = typeof (FieldUsedViaReflection).GetField (null);
		}

		[Kept]
		static void TestNoValueName ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			var method = typeof (FieldUsedViaReflection).GetField (noValue);
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
			var field = type.GetField ("publicField");
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var method = noValue.GetField ("publicField");
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (FieldUsedViaReflection);
		}

		[Kept]
		[ExpectedWarning ("IL2075", "FindType", "GetField")]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var field = type.GetField ("publicField");
		}

		[Kept]
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
		static void TestFieldInBaseType ()
		{
			var protectedField = typeof (DerivedClass).GetField ("protectedFieldOnBase"); // Will not be marked - only public fields work this way
			var publicField = typeof (DerivedClass).GetField ("publicFieldOnBase");
		}

		[Kept]
		static void TestIgnoreCaseBindingFlags ()
		{
			var field = typeof (IgnoreCaseBindingFlagsClass).GetField ("publicfield", BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestFailIgnoreCaseBindingFlags ()
		{
			var field = typeof (FailIgnoreCaseBindingFlagsClass).GetField ("publicfield", BindingFlags.Public);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			var field = typeof (PutDispPropertyBindingFlagsClass).GetField ("putDispPropertyField", BindingFlags.PutDispProperty);
		}

		static int field;

		[Kept]
		public static int publicField;

		[Kept]
		private class Foo
		{
			[Kept]
			public static int field;
			public int nonStatic;
			private static int nonKept;
		}

		[Kept]
		private class UnknownBindingFlags
		{
			[Kept]
			public static int field;
			[Kept]
			public int nonStatic;
			[Kept]
			private static int privatefield;
		}

		[Kept]
		private class UnknownBindingFlagsAndName
		{
			[Kept]
			public static int field;
			[Kept]
			public int nonStatic;
			[Kept]
			private static int privatefield;
		}

		[Kept]
		private class IfClass
		{
			[Kept]
			public static int ifField;
			[Kept]
			public int elseField;
			protected int nonKept;
		}

		[Kept]
		private class ElseClass
		{
			[Kept]
			public int elseField;
			[Kept]
			public static string ifField;
			volatile char nonKept;
		}

		[Kept]
		class BaseClass
		{
			protected int protectedFieldOnBase;
			[Kept]
			public char publicFieldOnBase;
		}

		[Kept]
		[KeptBaseType (typeof (BaseClass))]
		class DerivedClass : BaseClass
		{
		}

		[Kept]
		private class IgnoreCaseBindingFlagsClass
		{
			[Kept]
			public static int publicField;

			[Kept]
			public static int markedDueToIgnoreCaseField;
		}

		[Kept]
		private class FailIgnoreCaseBindingFlagsClass
		{
			public static int publicField;
		}

		[Kept]
		private class PutDispPropertyBindingFlagsClass
		{
			[Kept]
			public static int putDispPropertyField;

			[Kept]
			private int markedDueToPutDispPropertyField;
		}
	}
}
