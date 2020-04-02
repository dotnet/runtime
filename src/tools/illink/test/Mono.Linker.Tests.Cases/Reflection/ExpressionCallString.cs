using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using System;
using System.Linq;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[Reference ("System.Core.dll")]
	public class ExpressionCallString
	{
		public static void Main ()
		{
			Branch_SystemTypeValueNode_KnownStringValue ();
			Branch_SystemTypeValueNode_NullValueNode ();
			Branch_NullValueNode ();
			Branch_MethodParameterValueNode (typeof (ExpressionCallString), "Foo");
			Branch_UnrecognizedPatterns ();
		}

		[Kept]
		static void Branch_SystemTypeValueNode_KnownStringValue ()
		{
			TestByName (0);
			TestByName (1);
			TestByType (0);
			TestByType (1);
			TestByNameWithParameters ();
			TestNonExistingName ();
		}

		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) },
			typeof (ExpressionCallString), nameof (A), new Type [0])]
		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) },
			typeof (ExpressionCallString), nameof (B), new Type [0])]
		[Kept]
		static void TestByName (int i)
		{
			string MethodName = string.Empty;
			switch (i) {
				case 0:
					MethodName = "A";
					break;
				case 1:
					MethodName = "B";
					break;
				default:
					break;
			}

			Expression.Call (typeof (ExpressionCallString), MethodName, Type.EmptyTypes);
		}

		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) },
			typeof (C), "Foo", new Type [0])]
		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) },
			typeof (D), "Foo", new Type [0])]
		[Kept]
		static void TestByType (int i)
		{
			Type T = (Type)null;
			switch (i) {
				case 0:
					T = typeof (C);
					break;
				case 1:
					T = typeof (D);
					break;
				default:
					break;
			}

			Expression.Call (T, "Foo", Type.EmptyTypes);
		}

		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) },
			typeof (ExpressionCallString), nameof (Count) + "<T>", new string [] { "T" })]
		[Kept]
		static void TestByNameWithParameters ()
		{
			IQueryable source = null;
			Expression.Call (typeof (ExpressionCallString), "Count", new Type [] { source.ElementType }, source.Expression);
		}

		[Kept]
		static void Branch_SystemTypeValueNode_NullValueNode ()
		{
			Expression.Call (typeof (ExpressionCallString), null, Type.EmptyTypes);
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) })]
		[Kept]
		static void TestNonExistingName ()
		{
			Expression.Call (typeof (ExpressionCallString), "NonExisting", Type.EmptyTypes);
		}

		[Kept]
		static void Branch_NullValueNode ()
		{
			Expression.Call ((Type)null, "OnlyCalledViaExpression", Type.EmptyTypes);
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (ExpressionCallString);
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) })]
		[Kept]
		static void Branch_UnrecognizedPatterns ()
		{
			Expression.Call (FindType (), "OnlyCalledViaExpression", Type.EmptyTypes);
		}

		[Kept]
		static void Branch_MethodParameterValueNode (Type T, string s)
		{
			TestNonExistingTypeParameter (T);
			TestNonExistingNameParameter (s);
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) })]
		[Kept]
		static void TestNonExistingTypeParameter (Type T)
		{
			Expression.Call (T, "Foo", Type.EmptyTypes);
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) })]
		[Kept]
		static void TestNonExistingNameParameter (string s)
		{
			Expression.Call (typeof (ExpressionCallString), s, Type.EmptyTypes);
		}

		[Kept]
		static void A () { }

		[Kept]
		static void B () { }

		[Kept]
		class C
		{
			[Kept]
			static void Foo () { }
		}

		[Kept]
		class D
		{
			[Kept]
			static void Foo () { }
		}

		[Kept]
		protected static T Count<T> (T t)
		{
			return default (T);
		}
	}
}
