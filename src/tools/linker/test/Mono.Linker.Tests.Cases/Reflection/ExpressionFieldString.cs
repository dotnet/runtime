using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Reference ("System.Core.dll")]
	public class ExpressionFieldString
	{
		public static void Main ()
		{
			var e1 = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic1");
			var e2 = Expression.Field (null, typeof (ExpressionFieldString), "TestOnlyStatic2");

			var e3 = Expression.Field (Expression.Parameter (typeof(int), "somename"), typeof (ExpressionFieldString), "TestName1");

			Expression.Field (null, typeof (ADerived), "_protectedFieldOnBase");
			Expression.Field (null, typeof (ADerived), "_publicFieldOnBase");
		}

		[Kept]
		private static int TestOnlyStatic1;

		private int TestOnlyStatic2;

		[Kept]
		private int TestName1;
	}

	[Kept]
	class ABase
	{
		// [Kept] - TODO - should be kept: https://github.com/mono/linker/issues/1042
		protected static bool _protectedFieldOnBase;

		// [Kept] - TODO - should be kept: https://github.com/mono/linker/issues/1042
		public static bool _publicFieldOnBase;
	}

	[Kept]
	[KeptBaseType (typeof (ABase))]
	class ADerived : ABase
	{
	}
}
