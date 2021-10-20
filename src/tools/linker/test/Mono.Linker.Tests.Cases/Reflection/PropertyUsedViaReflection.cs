using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupLinkerArgument ("--disable-opt", "unreachablebodies")]
	public class PropertyUsedViaReflection
	{
		public static void Main ()
		{
			TestGetterAndSetter ();
			TestGetterAndSetterInternal ();
			TestSetterOnly ();
			TestGetterOnly ();
			TestBindingFlags ();
			TestUnknownBindingFlags (BindingFlags.Public);
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestPropertyOfArray ();
			TestNullType ();
			TestDataFlowType ();
			TestIfElse (1);
			TestPropertyInBaseType ();
			TestIgnoreCaseBindingFlags ();
			TestFailIgnoreCaseBindingFlags ();
			TestIgnorableBindingFlags ();
			TestUnsupportedBindingFlags ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.OnlyUsedViaReflection), (Type[]) null)]
		static void TestGetterAndSetter ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("OnlyUsedViaReflection");
			property.GetValue (null, new object[] { });
		}

		[Kept]
		static void TestGetterAndSetterInternal ()
		{
			// This will not mark the property as GetProperty(string) only returns public properties
			var property = typeof (PropertyUsedViaReflection).GetProperty ("InternalProperty");
			property.GetValue (null, new object[] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.SetterOnly), (Type[]) null)]
		static void TestSetterOnly ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("SetterOnly");
			property.SetValue (null, 42, new object[] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.GetterOnly), (Type[]) null)]
		static void TestGetterOnly ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("GetterOnly");
			property.GetValue (null, new object[] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (BindingFlagsTest), nameof (BindingFlagsTest.PublicProperty), (Type[]) null)]
		static void TestBindingFlags ()
		{
			var property = typeof (BindingFlagsTest).GetProperty ("PublicProperty", BindingFlags.Public);
			property.GetValue (null, new object[] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (UnknownBindingFlags), nameof (UnknownBindingFlags.SomeProperty), (Type[]) null)]
		static void TestUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all properties on the type
			var property = typeof (UnknownBindingFlags).GetProperty ("SomeProperty", bindingFlags);
			property.GetValue (null, new object[] { });
		}

		[Kept]
		static void TestNullName ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty (null);
		}

		[Kept]
		static void TestEmptyName ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty (string.Empty);
		}

		[Kept]
		static void TestNonExistingName ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("NonExisting");
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (Array), nameof (Array.LongLength))]
		static void TestPropertyOfArray ()
		{
			var property = typeof (int[]).GetProperty ("LongLength");
			property.GetValue (null);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var property = type.GetProperty ("GetterOnly");
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (PropertyUsedViaReflection);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			messageCode: "IL2075", message: new string[] { "FindType", "GetProperty" })]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var property = type.GetProperty ("GetterOnly");
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (IfClass), nameof (IfClass.SetterOnly), (Type[]) null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (IfClass), nameof (IfClass.GetterOnly), (Type[]) null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (ElseClass), nameof (ElseClass.SetterOnly), (Type[]) null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (ElseClass), nameof (ElseClass.GetterOnly), (Type[]) null)]
		static void TestIfElse (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof (IfClass);
			} else {
				myType = typeof (ElseClass);
			}
			string myString;
			if (i == 1) {
				myString = "SetterOnly";
			} else {
				myString = "GetterOnly";
			}
			var property = myType.GetProperty (myString);
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string) },
			typeof (BaseClass), nameof (BaseClass.GetterSetterOnBaseClass), (Type[]) null)]
		static void TestPropertyInBaseType ()
		{
			var property = typeof (DerivedClass).GetProperty ("GetterSetterOnBaseClass");
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type[] { typeof (string), typeof (BindingFlags) },
			typeof (IgnoreCaseBindingFlagsClass), nameof (IgnoreCaseBindingFlagsClass.SetterOnly), (Type[]) null)]
		static void TestIgnoreCaseBindingFlags ()
		{
			var property = typeof (IgnoreCaseBindingFlagsClass).GetProperty ("setteronly", BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestFailIgnoreCaseBindingFlags ()
		{
			var property = typeof (FailIgnoreCaseBindingFlagsClass).GetProperty ("setteronly", BindingFlags.Public);
		}

		[Kept]
		static void TestIgnorableBindingFlags ()
		{
			var property = typeof (ExactBindingBindingFlagsClass).GetProperty ("SetterOnly", BindingFlags.Public | BindingFlags.ExactBinding);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			var property = typeof (ChangeTypeBindingFlagsClass).GetProperty ("SetterOnly", BindingFlags.Public | BindingFlags.SuppressChangeType);
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
		class IfClass
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
		class BaseClass
		{
			[Kept]
			static int _basefield;

			static int _removedField;

			[Kept]
			public static int GetterSetterOnBaseClass {
				[Kept]
				set { _basefield = value; }
				[Kept]
				get { return _basefield; }
			}

			public static int GetterOnly {
				get { return _removedField; }
			}

			public static int SetterOnly {
				set { _removedField = value; }
			}
		}

		[Kept]
		[KeptBaseType (typeof (BaseClass))]
		class DerivedClass : BaseClass
		{
		}

		[Kept]
		class BindingFlagsTest
		{
			[Kept]
			public int PublicProperty {
				[Kept]
				get { return _field; }
				[Kept]
				set { _field = value; }
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
			public static int MakedDueToIgnoreCase {
				[Kept]
				get { return _field; }
			}
		}

		[Kept]
		class FailIgnoreCaseBindingFlagsClass
		{
			public static int SetterOnly {
				set { _field = value; }
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

			public static int Unmarked {
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
			public static int Marked {
				[Kept]
				get { return _field; }
			}
		}
	}
}
