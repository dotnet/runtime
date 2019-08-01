using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileArgument ("/r:System.Core.dll")]
	public class ExpressionFieldString
	{
		public static void Main ()
		{
			var e1 = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic1");
			var e2 = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic2");

			var e3 = Expression.Field (Expression.Parameter (typeof(int), "somename"), typeof (ExpressionFieldString), "TestName1");
		}

		[Kept]
		private static int TestOnlyStatic1;

		private int TestOnlyStatic2;

		[Kept]
		private int TestName1;
	}
}
