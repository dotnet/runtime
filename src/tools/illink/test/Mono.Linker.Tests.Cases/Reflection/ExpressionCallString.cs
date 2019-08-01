using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using System;
using System.Linq;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileArgument ("/r:System.Core.dll")]
	public class ExpressionCallString
	{
		public static void Main ()
		{
			var expr = Expression.Call (typeof (ExpressionCallString), "OnlyCalledViaExpression", Type.EmptyTypes);
			Console.WriteLine (expr.Method);

            IQueryable source = null;
            var e2 = Expression.Call (typeof (ExpressionCallString), "Count", new Type [] { source.ElementType }, source.Expression);
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
