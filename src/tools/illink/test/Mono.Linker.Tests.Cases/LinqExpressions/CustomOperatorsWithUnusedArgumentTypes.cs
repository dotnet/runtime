using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinqExpressions
{
	public class CustomOperatorsWithUnusedArgumentTypes
	{
		public static void Main ()
		{
			var t = typeof (CustomOperators);
			var expression = typeof (System.Linq.Expressions.Expression);

			var t1 = typeof (AdditionType);
			var t2 = typeof (SubtractionType);
		}

		public class CustomOperators
		{
			// simple cases are still kept
			[Kept]
			public static CustomOperators operator + (CustomOperators c) => null;
			[Kept]
			public static CustomOperators operator + (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator - (CustomOperators left, CustomOperators right) => null;

			// binary operators taking kept other types are still kept
			[Kept]
			public static CustomOperators operator + (CustomOperators left, AdditionType right) => null;
			[Kept]
			public static CustomOperators operator - (SubtractionType left, CustomOperators right) => null;

			// binary operators taking unused other types are removed
			public static CustomOperators operator * (CustomOperators left, MultiplicationTypeUnused right) => null;
			public static CustomOperators operator / (DivisionTypeUnused left, CustomOperators right) => null;

			// conversion operators to/from unused other types are removed
			public static implicit operator TargetTypeImplicitUnused (CustomOperators self) => null;
			public static implicit operator CustomOperators (SourceTypeImplicitUnused other) => null;
			public static explicit operator TargetTypeExplicitUnused (CustomOperators self) => null;
			public static explicit operator CustomOperators (SourceTypeExplicitUnused other) => null;
		}

		[Kept]
		public class AdditionType { }
		[Kept]
		public class SubtractionType { }

		public class MultiplicationTypeUnused { }
		public class DivisionTypeUnused { }

		public class TargetTypeImplicitUnused { }
		public class SourceTypeImplicitUnused { }
		public class TargetTypeExplicitUnused { }
		public class SourceTypeExplicitUnused { }
	}
}
