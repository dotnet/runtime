using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[Reference ("System.Core.dll")]
	[ExpectedNoWarnings]
	public class ExpressionCallString
	{
		public static void Main ()
		{
			PublicMethods.Test ();
			ProtectedMethods.Test ();
			PrivateMethods.Test ();

			Expression.Call (typeof (Derived), "PublicOnBase", Type.EmptyTypes);
			Expression.Call (typeof (Derived), "ProtectedOnBase", Type.EmptyTypes);
			Expression.Call (typeof (Derived), "PrivateOnBase", Type.EmptyTypes);

			// Keep all methods on type UnknownNameMethodClass
			Expression.Call (typeof (UnknownNameMethodClass), GetUnknownString (), Type.EmptyTypes);

			TestUnknownType.Test ();

			TestGenericMethods.Test ();
		}

		[Kept]
		class PublicMethods
		{
			[Kept]
			public static void PublicStaticMethod () { }

			public void PublicInstanceMethod () { }

			protected static void ProtectedStaticMethod () { }

			protected void ProtectedInstanceMethod () { }

			private static void PrivateStaticMethod () { }

			private void PrivateInstanceMethod () { }

			[Kept]
			public static void Test ()
			{
				Expression.Call (typeof (PublicMethods), nameof (PublicStaticMethod), Type.EmptyTypes);

				// This should not mark anything, but it should also not warn (it should fail at runtime to find the method as well)
				Expression.Call (typeof (PublicMethods), nameof (PublicInstanceMethod), Type.EmptyTypes);
			}
		}

		[Kept]
		class ProtectedMethods
		{
			public static void PublicStaticMethod () { }

			public void PublicInstanceMethod () { }

			[Kept]
			protected static void ProtectedStaticMethod () { }

			protected void ProtectedInstanceMethod () { }

			private static void PrivateStaticMethod () { }

			private void PrivateInstanceMethod () { }

			[Kept]
			public static void Test ()
			{
				Expression.Call (typeof (ProtectedMethods), nameof (ProtectedStaticMethod), Type.EmptyTypes);

				// This should not mark anything, but it should also not warn (it should fail at runtime to find the method as well)
				Expression.Call (typeof (ProtectedMethods), nameof (ProtectedInstanceMethod), Type.EmptyTypes);
			}
		}

		[Kept]
		class PrivateMethods
		{
			public static void PublicStaticMethod () { }

			public void PublicInstanceMethod () { }

			protected static void ProtectedStaticMethod () { }

			protected void ProtectedInstanceMethod () { }

			[Kept]
			private static void PrivateStaticMethod () { }

			private void PrivateInstanceMethod () { }

			[Kept]
			public static void Test ()
			{
				Expression.Call (typeof (PrivateMethods), nameof (PrivateStaticMethod), Type.EmptyTypes);

				// This should not mark anything, but it should also not warn (it should fail at runtime to find the method as well)
				Expression.Call (typeof (PrivateMethods), nameof (PrivateInstanceMethod), Type.EmptyTypes);
			}
		}

		[Kept]
		static string GetUnknownString ()
		{
			return "unknownstring";
		}

		[Kept]
		class TestUnknownType
		{
			[Kept]
			public static void PublicMethod ()
			{
			}

			[Kept]
			protected static void ProtectedMethod ()
			{
			}

			[Kept]
			private static void PrivateMethod ()
			{
			}

			[Kept]
			[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.Call),
				new Type[] { typeof (Type), typeof (string), typeof (Type[]), typeof (Expression[]) }, messageCode: "IL2072")]
			public static void Test ()
			{
				// Keep all methods of the type that made the call
				Expression.Call (GetUnknownType (), "This string will not be reached", Type.EmptyTypes);
				// UnrecognizedReflectionAccessPattern
				Expression.Call (TriggerUnrecognizedPattern (), "This string will not be reached", Type.EmptyTypes);
			}

			[Kept]
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			static Type GetUnknownType ()
			{
				return typeof (TestUnknownType);
			}

			[Kept]
			static Type TriggerUnrecognizedPattern ()
			{
				return typeof (TestUnknownType);
			}
		}

		[Kept]
		class UnknownNameMethodClass
		{
			[Kept]
			public static void PublicStaticMethod ()
			{
			}

			[Kept]
			public void PublicNonStaticMethod ()
			{
			}

			[Kept]
			protected static void ProtectedStaticMethod ()
			{
			}

			[Kept]
			protected void ProtectedNonStaticMethod ()
			{
			}

			[Kept]
			private static void PrivateStaticMethod ()
			{
			}

			[Kept]
			private void PrivateNonStaticMethod ()
			{
			}
		}

		[Kept]
		class Base
		{
			[Kept]
			public static void PublicOnBase ()
			{
			}

			[Kept]
			protected static void ProtectedOnBase ()
			{
			}

			private static void PrivateOnBase ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base
		{
		}

		[Kept]
		class TestGenericMethods
		{
			[Kept]
			public static void GenericMethodCalledAsNonGeneric<T> () { }

			[Kept]
			public static void GenericMethodWithNoRequirements<T> () { }

			[Kept]
			public static void GenericMethodWithRequirements<
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> ()
			{ }

			[Kept]
			public static void GenericMethodWithRequirementsNoArguments<
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> ()
			{ }

			[Kept]
			[ExpectedWarning ("IL2060", "Expression::Call")]
			static void TestWithNoTypeParameters ()
			{
				// Linker warns since this is a call to a generic method with a mismatching number of generic parameters
				// and provided type values for the generic instantiation.
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodCalledAsNonGeneric), Type.EmptyTypes);
			}

			[Kept]
			static void TestMethodWithoutRequirements ()
			{
				// This may not warn - as it's safe
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodWithNoRequirements), new Type[] { GetUnknownType () });
			}

			[Kept]
			static void TestMethodWithRequirements ()
			{
				// This may not warn - as it's safe
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodWithRequirements), new Type[] { GetUnknownTypeWithRequrements () });
			}

			[Kept]
			[ExpectedWarning ("IL2060", "Expression::Call")]
			static void TestMethodWithRequirementsUnknownTypeArray (Type[] types)
			{
				// The passed in types array cannot be analyzed, so a warning is produced.
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodWithRequirements), types);
			}

			[Kept]
			[ExpectedWarning ("IL2060", "Expression::Call")]
			static void TestMethodWithRequirementsButNoTypeArguments ()
			{
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodWithRequirementsNoArguments), Type.EmptyTypes);
			}

			[Kept]
			[KeptMember (".cctor()")]
			class UnknownMethodWithRequirements
			{
				[Kept]
				public static void GenericMethodWithRequirements<
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> ()
				{ }

				[Kept]
				static string _unknownMethodName = "NoMethod";

				[Kept]
				[ExpectedWarning ("IL2060")]
				public static void TestWithTypeParameters ()
				{
					// Linker has no idea which method to mark - so it should warn if there are type parameters
					Expression.Call (typeof (UnknownMethodWithRequirements), _unknownMethodName, new Type[] { GetUnknownType () });
				}

				[Kept]
				public static void TestWithoutTypeParameters ()
				{
					// Linker has no idea which method to mark - so it should warn if there are type parameters
					Expression.Call (typeof (UnknownMethodWithRequirements), _unknownMethodName, Type.EmptyTypes);
				}
			}

			[Kept]
			[KeptMember (".cctor()")]
			class UnknownTypeWithRequirements
			{
				public static void GenericMethodWithRequirements<
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> ()
				{ }

				[Kept]
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
				static Type _unknownType = null;

				[Kept]
				[ExpectedWarning ("IL2060")]
				public static void TestWithTypeParameters ()
				{
					// Linker has no idea which method to mark - so it should warn if there are type parameters
					Expression.Call (_unknownType, "NoMethod", new Type[] { GetUnknownType () });
				}

				[Kept]
				public static void TestWithoutTypeParameters ()
				{
					// Linker has no idea which method to mark - so it should warn if there are type parameters
					Expression.Call (_unknownType, "NoMethod", Type.EmptyTypes);
				}
			}

			[Kept]
			public static void Test ()
			{
				TestWithNoTypeParameters ();
				TestMethodWithoutRequirements ();
				TestMethodWithRequirements ();
				TestMethodWithRequirementsUnknownTypeArray (null);
				TestMethodWithRequirementsButNoTypeArguments ();
				UnknownMethodWithRequirements.TestWithTypeParameters ();
				UnknownMethodWithRequirements.TestWithoutTypeParameters ();
				UnknownTypeWithRequirements.TestWithTypeParameters ();
				UnknownTypeWithRequirements.TestWithoutTypeParameters ();
			}

			[Kept]
			static Type GetUnknownType () { return null; }

			[Kept]
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			static Type GetUnknownTypeWithRequrements () { return null; }
		}
	}
}
