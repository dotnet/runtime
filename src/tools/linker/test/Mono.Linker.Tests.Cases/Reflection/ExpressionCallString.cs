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
			TestByName ();
			TestByNameWithParameters ();
			TestNonExistingName ();
		}

		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) },
			typeof (ExpressionCallString), nameof (OnlyCalledViaExpression), new Type [0])]
		[Kept]
		static void TestByName ()
		{
			var expr = Expression.Call (typeof (ExpressionCallString), "OnlyCalledViaExpression", Type.EmptyTypes);
			Console.WriteLine (expr.Method);
		}

		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) },
			typeof (ExpressionCallString), nameof (Count) + "<T>", new string [] { "T" })]
		[Kept]
		static void TestByNameWithParameters ()
		{
			IQueryable source = null;
			var e2 = Expression.Call (typeof (ExpressionCallString), "Count", new Type [] { source.ElementType }, source.Expression);
		}

		[Kept]
		static void Branch_SystemTypeValueNode_NullValueNode ()
		{
			var expr = Expression.Call (typeof (ExpressionCallString), null, Type.EmptyTypes);
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) })]
		[Kept]
		static void TestNonExistingName ()
		{
			var expr = Expression.Call (typeof (ExpressionCallString), "NonExisting", Type.EmptyTypes);
		}

		[Kept]
		static void Branch_NullValueNode ()
		{
			var expr = Expression.Call ((Type)null, "OnlyCalledViaExpression", Type.EmptyTypes);
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
			var expr = Expression.Call (FindType (), "OnlyCalledViaExpression", Type.EmptyTypes);
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
			var expr = Expression.Call (T, "Foo", Type.EmptyTypes);
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) })]
		[Kept]
		static void TestNonExistingNameParameter (string s)
		{
			var expr = Expression.Call (typeof (ExpressionCallString), s, Type.EmptyTypes);
		}

		[Kept]
		private static int OnlyCalledViaExpression ()
		{
			return 42;
		}

		[Kept]
		private static int OnlyCalledViaExpression<T> (T arg)
		{
			return 2;
		}

		[Kept]
		protected static T Count<T> (T t)
		{
			return default (T);
		}
	}
}
