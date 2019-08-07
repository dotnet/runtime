using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection {
	[SetupCompileArgument ("/r:System.Core.dll")]
	public class ExpressionCallStringAndLocals {
		public static void Main ()
		{
			var t1 = typeof (ExpressionCallStringAndLocals);
			var t2 = t1;

			var expr = Expression.Call (t2, "OnlyCalledViaExpression", Type.EmptyTypes);
			Console.WriteLine (expr.Method);
		}

		[Kept]
		private static int OnlyCalledViaExpression ()
		{
			return 42;
		}
	}
}
