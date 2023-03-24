using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinqExpressions
{
	public class CanRemoveOperatorsWhenNotUsingLinqExpressions
	{
		public static void Main ()
		{
			var c = new CustomOperators ();
			var c2 = +c;
			var c3 = c + c2;
			var t = (TargetType) c3;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class CustomOperators
		{
			[Kept]
			public static CustomOperators operator + (CustomOperators c) => null;
			public static CustomOperators operator - (CustomOperators c) => null;

			[Kept]
			public static CustomOperators operator + (CustomOperators left, CustomOperators right) => null;
			public static CustomOperators operator + (CustomOperators left, AdditionTypeUnused right) => null;
			public static CustomOperators operator - (CustomOperators left, CustomOperators right) => null;

			[Kept]
			public static explicit operator TargetType (CustomOperators self) => null;

			public static explicit operator CustomOperators (SourceTypeUnused other) => null;
		}

		class AdditionTypeUnused { }

		[Kept]
		class TargetType { }

		class SourceTypeUnused { }
	}
}
