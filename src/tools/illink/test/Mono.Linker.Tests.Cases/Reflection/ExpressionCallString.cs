using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[Reference ("System.Core.dll")]
	[ExpectedNoWarnings]
	[KeptPrivateImplementationDetails ("ThrowSwitchExpressionException")]
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
			TestNullType ();
			TestNoValue ();
			TestNullString ();
			TestEmptyString ();
			TestNoValueString ();

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
			[ExpectedWarning ("IL2072", nameof (Expression) + "." + nameof (Expression.Call))]
			public static void Test ()
			{
				// Keep all methods of the type that made the call
				Expression.Call (GetUnknownType (), "This string will not be reached", Type.EmptyTypes);
				// IL2072
				Expression.Call (TriggerUnrecognizedPattern (), "This string will not be reached", Type.EmptyTypes);
			}

			[Kept]
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
			static Type GetUnknownType ()
			{
				return typeof (TestType);
			}

			[Kept]
			static Type TriggerUnrecognizedPattern ()
			{
				return typeof (TestType);
			}
		}

		[Kept]
		static void TestNullType ()
		{
			Type t = null;
			Expression.Call (t, "This string will not be reached", Type.EmptyTypes);
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			Expression.Call (noValue, "This string will not be reached", Type.EmptyTypes);
		}

		[Kept]
		static void TestNullString ()
		{
			Expression.Call (typeof (TestType), null, Type.EmptyTypes);
		}

		[Kept]
		static void TestEmptyString ()
		{
			Expression.Call (typeof (TestType), string.Empty, Type.EmptyTypes);
		}

		[Kept]
		static void TestNoValueString ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			Expression.Call (typeof (TestType), noValue, Type.EmptyTypes);
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
			static void TestWithNoTypeParameters ()
			{
				// ILLink should not warn even if the type parameters don't match since the target method has no requirements
				// the fact that the reflection API may fail in this case is not something ILLink should worry about.
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
			[ExpectedWarning ("IL2060", "Expression.Call")]
			static void TestMethodWithRequirementsUnknownTypeArray (Type[] types)
			{
				// The passed in types array cannot be analyzed, so a warning is produced.
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodWithRequirements), types);
			}

			[Kept]
			[ExpectedWarning ("IL2060", "Expression.Call")]
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
					// Trimming tools have no idea which method to mark - so it should warn if there are type parameters
					Expression.Call (typeof (UnknownMethodWithRequirements), _unknownMethodName, new Type[] { GetUnknownType () });
				}

				[Kept]
				public static void TestWithoutTypeParameters ()
				{
					// Trimming tools have no idea which method to mark - so it should warn if there are type parameters
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
					// Trimming tools have no idea which method to mark - so it should warn if there are type parameters
					Expression.Call (_unknownType, "NoMethod", new Type[] { GetUnknownType () });
				}

				[Kept]
				public static void TestWithoutTypeParameters ()
				{
					// Trimming tools have no idea which method to mark - so it should warn if there are type parameters
					Expression.Call (_unknownType, "NoMethod", Type.EmptyTypes);
				}
			}

			[Kept]
			[KeptMember (".cctor()")]
			class TwoKnownTypeArrays
			{
				[Kept]
				public static void GenericMethodWithRequirements<
					[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] T> ()
				{ }

				[Kept]
				static string _unknownMethodName = "NoMethod";

				[Kept]
				[ExpectedWarning ("IL2060", "Expression.Call")]
				public static void Test (int p = 0)
				{
					Type[] types = p switch {
						0 => new Type[] { typeof (TestType) },
						1 => new Type[] { typeof (TestType) }
					};

					Expression.Call (typeof (TwoKnownTypeArrays), _unknownMethodName, types);
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
				TwoKnownTypeArrays.Test ();
			}

			[Kept]
			static Type GetUnknownType () { return null; }

			[Kept]
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)]
			static Type GetUnknownTypeWithRequrements () { return null; }
		}

		[Kept]
		class TestType { }
	}
}
