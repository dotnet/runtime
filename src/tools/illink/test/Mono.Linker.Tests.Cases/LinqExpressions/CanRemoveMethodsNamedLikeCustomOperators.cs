using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinqExpressions
{
	public class CanRemoveMethodsNamedLikeCustomOperators
	{
		public static void Main ()
		{
			var t = typeof (FakeOperators);
			var expression = typeof (System.Linq.Expressions.Expression);
			var t1 = typeof (SubtractionType);
			var t2 = typeof (TargetType);
		}

		public class FakeOperators
		{
			[Kept]
			public static FakeOperators operator - (FakeOperators f) => null;

			public static FakeOperators op_UnaryPlus (FakeOperators f) => null;
			public static FakeOperators op_Addition (FakeOperators left, FakeOperators right) => null;
			public static FakeOperators op_Subtraction (FakeOperators left, SubtractionType right) => null;
			public static TargetType op_Explicit (FakeOperators self) => null;
		}

		[Kept]
		public class SubtractionType { }
		[Kept]
		public class TargetType { }
	}
}
