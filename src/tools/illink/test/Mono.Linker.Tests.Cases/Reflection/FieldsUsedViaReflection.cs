using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	public class FieldsUsedViaReflection
	{
		public static void Main ()
		{
			TestGetFields ();
			TestBindingFlags ();
			TestUnknownBindingFlags (BindingFlags.Public);
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIfElse (1);
			TestIgnoreCaseBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		static void TestGetFields ()
		{
			var fields = typeof (FieldsUsedViaReflection).GetFields ();
		}

		[Kept]
		static void TestBindingFlags ()
		{
			var fields = typeof (Foo).GetFields (BindingFlags.Public);
		}

		[Kept]
		static void TestUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all fields on the type
			var fields = typeof (UnknownBindingFlags).GetFields (bindingFlags);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var fields = type.GetFields ();
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var methods = noValue.GetFields ();
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (FieldsUsedViaReflection);
		}

		[Kept]
		[ExpectedWarning ("IL2075", "FindType", "GetFields")]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var fields = type.GetFields (BindingFlags.Public);
		}

		[Kept]
		static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
		{
			var fields = type.GetFields (BindingFlags.Public | BindingFlags.Static);
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
			var fields = myType.GetFields (BindingFlags.Public);
		}

		[Kept]
		static void TestIgnoreCaseBindingFlags ()
		{
			var fields = typeof (IgnoreCaseBindingFlagsClass).GetFields (BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			var fields = typeof (PutDispPropertyBindingFlagsClass).GetFields (BindingFlags.PutDispProperty);
		}

		static int field;

		[Kept]
		public static int publicField;

		[Kept]
		private class Foo
		{
			[Kept]
			public static int field;
			[Kept]
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
		private class MyType
		{
			[Kept]
			public static int ifField;
			[Kept]
			public int elseField;
			protected int nonKept;
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
		private class IgnoreCaseBindingFlagsClass
		{
			[Kept]
			public static int publicField;

			[Kept]
			public static int markedDueToIgnoreCaseField;
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
