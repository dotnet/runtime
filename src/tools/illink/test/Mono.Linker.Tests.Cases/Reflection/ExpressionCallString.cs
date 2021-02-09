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
			// BUG:https://github.com/mono/linker/issues/1819
			// [ExpectedWarning("IL9999", nameof(GenericMethodWithRequirements))]
			public static void Test ()
			{
				// Linker doesn't check if it's valid to call a generic method without generic parameters, it looks like a non-generic call
				// so it will preserve the target method.
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodCalledAsNonGeneric), Type.EmptyTypes);

				// This may not warn - as it's safe
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodWithNoRequirements), new Type[] { GetUnknownType () });

				// This must warn - as this is dangerous
				Expression.Call (typeof (TestGenericMethods), nameof (GenericMethodWithRequirements), new Type[] { GetUnknownType () });
			}

			[Kept]
			static Type GetUnknownType () { return null; }
		}
	}
}
