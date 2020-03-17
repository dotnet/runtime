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
			TestByName ();
			TestByNameWithParameters ();
			TestNullName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
		}

		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type[]), typeof (Expression []) },
			typeof (ExpressionCallString), nameof (OnlyCalledViaExpression), new Type [0])]
		[Kept]
		static void TestByName ()
		{
			var expr = Expression.Call (typeof (ExpressionCallString), "OnlyCalledViaExpression", Type.EmptyTypes);
			Console.WriteLine (expr.Method);
		}

		[RecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) },
			typeof (ExpressionCallString), nameof (Count) + "<T>", new string [] { "T" } )]
		[Kept]
		static void TestByNameWithParameters ()
		{
			IQueryable source = null;
			var e2 = Expression.Call (typeof (ExpressionCallString), "Count", new Type [] { source.ElementType }, source.Expression);
		}

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) })]
		[Kept]
		static void TestNullName ()
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

		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Call), new Type [] { typeof (Type), typeof (string), typeof (Type []), typeof (Expression []) })]
		[Kept]
		static void TestNullType ()
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
		static void TestDataFlowType ()
		{
			var expr = Expression.Call (FindType (), "OnlyCalledViaExpression", Type.EmptyTypes);
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
