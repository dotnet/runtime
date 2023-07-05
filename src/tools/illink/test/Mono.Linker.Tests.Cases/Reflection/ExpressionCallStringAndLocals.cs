using System;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Reference ("System.Core.dll")]
	[ExpectedNoWarnings]
	public class ExpressionCallStringAndLocals
	{
		public static void Main ()
		{
			Branch_SystemTypeValueNode_KnownStringValue ();
		}

		[Kept]
		private static int OnlyCalledViaExpression ()
		{
			return 42;
		}

		[Kept]
		static void Branch_SystemTypeValueNode_KnownStringValue ()
		{
			var t1 = typeof (ExpressionCallStringAndLocals);
			var t2 = t1;

			var expr = Expression.Call (t2, "OnlyCalledViaExpression", Type.EmptyTypes);
			Console.WriteLine (expr.Method);
		}
	}
}
