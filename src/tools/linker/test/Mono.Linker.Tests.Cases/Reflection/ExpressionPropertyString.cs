using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCompileArgument ("/r:System.Core.dll")]
	public class ExpressionPropertyString
	{
		public static void Main ()
		{
			var e1 = Expression.Property (null, typeof (ExpressionPropertyString), "TestOnlyStatic1");
			var e2 = Expression.Property (null, typeof (ExpressionPropertyString), "TestOnlyStatic2");

			var e3 = Expression.Property (Expression.Parameter (typeof(int), "somename"), typeof (ExpressionPropertyString), "TestName1");
		}

		[Kept]
		[KeptBackingField]
		private static int TestOnlyStatic1 { [Kept] get; [Kept] set; }

		private int TestOnlyStatic2 { get; }

		[Kept]
		[KeptBackingField]
		private int TestName1 { [Kept] get; }
	}
}
