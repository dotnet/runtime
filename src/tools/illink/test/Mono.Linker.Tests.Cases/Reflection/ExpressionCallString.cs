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
			Expression.Call (typeof (Foo), "PublicStaticMethod", Type.EmptyTypes);
			Expression.Call (typeof (Foo), "PublicNonStaticMethod", Type.EmptyTypes);
			Expression.Call (typeof (Foo), "ProtectedStaticMethod", Type.EmptyTypes);
			Expression.Call (typeof (Foo), "ProtectedNonStaticMethod", Type.EmptyTypes);
			Expression.Call (typeof (Foo), "PrivateStaticMethod", Type.EmptyTypes);
			Expression.Call (typeof (Foo), "PrivateNonStaticMethod", Type.EmptyTypes);

			Expression.Call (typeof (Derived), "PublicOnBase", Type.EmptyTypes);
			Expression.Call (typeof (Derived), "ProtectedOnBase", Type.EmptyTypes);
			Expression.Call (typeof (Derived), "PrivateOnBase", Type.EmptyTypes);

			// Keep all methods on type Bar
			Expression.Call (typeof (Bar), GetUnknownString (), Type.EmptyTypes);

			TestUnknownType.Test ();
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
				new Type[] { typeof (Type), typeof (string), typeof (Type[]), typeof (Expression[]) })]
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
		class Foo
		{
			[Kept]
			public static void PublicStaticMethod ()
			{
			}

			public void PublicNonStaticMethod ()
			{
			}

			[Kept]
			protected static void ProtectedStaticMethod ()
			{
			}
			protected void ProtectedNonStaticMethod ()
			{
			}

			[Kept]
			private static void PrivateStaticMethod ()
			{
			}

			private void PrivateNonStaticMethod ()
			{
			}
		}

		[Kept]
		class Bar
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
	}
}
