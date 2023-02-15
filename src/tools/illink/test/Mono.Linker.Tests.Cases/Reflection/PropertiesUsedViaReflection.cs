using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	[SetupLinkerArgument ("--disable-opt", "unreachablebodies")]
	public class PropertiesUsedViaReflection
	{
		public static void Main ()
		{
			TestGetProperties ();
			TestBindingFlags ();
			TestUnknownBindingFlags (BindingFlags.Public);
			TestPropertiesOfArray ();
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIfElse (1);
			TestIgnoreCaseBindingFlags ();
			TestIgnorableBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		static void TestGetProperties ()
		{
			var properties = typeof (PropertiesUsedViaReflection).GetProperties ();
		}

		[Kept]
		static void TestBindingFlags ()
		{
			var properties = typeof (BindingFlagsTest).GetProperties (BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		static void TestUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all properties on the type
			var properties = typeof (UnknownBindingFlags).GetProperties (bindingFlags);
		}

		[Kept]
		static void TestPropertiesOfArray ()
		{
			var properties = typeof (int[]).GetProperties (BindingFlags.Public);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var properties = type.GetProperties (BindingFlags.Public);
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var methods = noValue.GetProperties (BindingFlags.Public);
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (PropertiesUsedViaReflection);
		}

		[Kept]
		[ExpectedWarning ("IL2075", "GetProperties")]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var properties = type.GetProperties (BindingFlags.Public);
		}

		[Kept]
		private static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
		{
			var properties = type.GetProperties (BindingFlags.Public | BindingFlags.Static);
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
			var properties = myType.GetProperties (BindingFlags.Public);
		}

		[Kept]
		static void TestIgnoreCaseBindingFlags ()
		{
			var properties = typeof (IgnoreCaseBindingFlagsClass).GetProperties (BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestIgnorableBindingFlags ()
		{
			var properties = typeof (ExactBindingBindingFlagsClass).GetProperties (BindingFlags.Public | BindingFlags.ExactBinding);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			var properties = typeof (ChangeTypeBindingFlagsClass).GetProperties (BindingFlags.Public | BindingFlags.SuppressChangeType);
		}

		[Kept]
		static int _field;

		[Kept]
		public static int OnlyUsedViaReflection {
			[Kept]
			get { return _field; }
			[Kept]
			set { _field = value; }
		}

		static int InternalProperty {
			get { return _field; }
			set { _field = value; }
		}

		[Kept]
		public static int SetterOnly {
			[Kept]
			set { _field = value; }
		}

		[Kept]
		public static int GetterOnly {
			[Kept]
			get { return _field; }
		}

		[Kept]
		class MyType
		{
			[Kept]
			public static int _field;
			private static int _privatefield;
			[Kept]
			public static int SetterOnly {
				[Kept]
				set { _field = value; }

			}
			[Kept]
			public static int GetterOnly {
				[Kept]
				get { return _field; }
			}
			private static int privateProperty {
				get { return _privatefield; }
				set { _privatefield = value; }
			}
		}

		[Kept]
		class IfClass
		{
			private static int _private;
			[Kept]
			public static int SetterOnly {
				[Kept]
				set { _field = value; }

			}
			[Kept]
			public static int GetterOnly {
				[Kept]
				get { return _field; }
			}

			private static int privateProperty {
				get { return _private; }
				set { _private = value; }
			}
		}
		[Kept]
		class ElseClass
		{
			[Kept]
			public static int SetterOnly {
				[Kept]
				set { _field = value; }

			}
			[Kept]
			public static int GetterOnly {
				[Kept]
				get { return _field; }
			}
		}

		[Kept]
		class BindingFlagsTest
		{
			private int _private;

			[Kept]
			public int PublicProperty {
				[Kept]
				get { return _field; }
				[Kept]
				set { _field = value; }
			}

			private int PrivateProperty {
				get { return _private; }
				set { _private = value; }
			}
		}

		[Kept]
		class UnknownBindingFlags
		{
			[Kept]
			internal static int SomeProperty {
				[Kept]
				private get { return _field; }
				[Kept]
				set { _field = value; }
			}
		}

		[Kept]
		class IgnoreCaseBindingFlagsClass
		{
			[Kept]
			public static int SetterOnly {
				[Kept]
				set { _field = value; }
			}

			[Kept]
			private static int MakedDueToIgnoreCase {
				[Kept]
				get { return _field; }
			}
		}

		[Kept]
		class ExactBindingBindingFlagsClass
		{
			[Kept]
			public static int SetterOnly {
				[Kept]
				set { _field = value; }
			}

			private static int Unmarked {
				get { return _field; }
			}
		}

		[Kept]
		class ChangeTypeBindingFlagsClass
		{
			[Kept]
			public static int SetterOnly {
				[Kept]
				set { _field = value; }
			}

			[Kept]
			private static int KeptDueToChangeType {
				[Kept]
				get { return _field; }
			}
		}
	}
}
