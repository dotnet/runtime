using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection {
	[SetupCSharpCompilerToUse ("csc")]
	public class PropertyUsedViaReflection
	{
		public static void Main ()
		{
			TestGetterAndSetter ();
			TestSetterOnly ();
			TestGetterOnly ();
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
			TestIfElse (1);
			TestPropertyInBaseType ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.OnlyUsedViaReflection), (Type [])null)]
		static void TestGetterAndSetter ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("OnlyUsedViaReflection");
			property.GetValue (null, new object [] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.SetterOnly), (Type [])null)]
		static void TestSetterOnly ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("SetterOnly");
			property.SetValue (null, 42, new object [] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.GetterOnly), (Type [])null)]
		static void TestGetterOnly ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("GetterOnly");
			property.GetValue (null, new object [] { });
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
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) })]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var property = type.GetProperty ("GetterOnly");
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (IfClass), nameof (IfClass.SetterOnly), (Type [])null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (IfClass), nameof (IfClass.GetterOnly), (Type [])null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (ElseClass), nameof (ElseClass.SetterOnly), (Type [])null)]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (ElseClass), nameof (ElseClass.GetterOnly), (Type [])null)]
		static void TestIfElse (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof(IfClass);
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
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (BaseClass), nameof (BaseClass.GetterSetterOnBaseClass), (Type [])null)]
		static void TestPropertyInBaseType ()
		{
			var property = typeof(DerivedClass).GetProperty ("GetterSetterOnBaseClass");
		}
		[Kept]
		static int _field;

		[Kept]
		static int OnlyUsedViaReflection {
			[Kept]
			get { return _field; }
			[Kept]
			set { _field = value; }
		}

		[Kept]
		static int SetterOnly {
			[Kept]
			set { _field = value; }
		}

		[Kept]
		static int GetterOnly {
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
		[KeptBaseType (typeof(BaseClass))]
		class DerivedClass : BaseClass 
		{
		}
	}
}
