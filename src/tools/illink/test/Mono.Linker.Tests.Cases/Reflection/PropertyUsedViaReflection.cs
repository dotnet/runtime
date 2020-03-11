using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection {
	[SetupCSharpCompilerToUse ("csc")]
	public class PropertyUsedViaReflection {
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
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.OnlyUsedViaReflection), (Type[]) null)]
		static void TestGetterAndSetter ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("OnlyUsedViaReflection");
			property.GetValue (null, new object [] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.SetterOnly), (Type []) null)]
		static void TestSetterOnly ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("SetterOnly");
			property.SetValue (null, 42, new object [] { });
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) },
			typeof (PropertyUsedViaReflection), nameof (PropertyUsedViaReflection.GetterOnly), (Type []) null)]
		static void TestGetterOnly ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("GetterOnly");
			property.GetValue (null, new object [] { });
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) })]
		static void TestNullName ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty (null);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) })]
		static void TestEmptyName ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty (string.Empty);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) })]
		static void TestNonExistingName ()
		{
			var property = typeof (PropertyUsedViaReflection).GetProperty ("NonExisting");
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetProperty), new Type [] { typeof (string) })]
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
	}
}
