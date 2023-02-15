using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinqExpressions
{
	public class CanPreserveCustomOperators
	{
		public static void Main ()
		{
			var t = typeof (CustomOperators);
			var expression = typeof (System.Linq.Expressions.Expression);

			var t3 = typeof (TargetTypeImplicit);
			var t4 = typeof (SourceTypeImplicit);
			var t5 = typeof (TargetTypeExplicit);
			var t6 = typeof (SourceTypeExplicit);

			var t7 = typeof (GenericCustomOperators<>);
			var t8 = typeof (GenericTypeArgument);
		}

		class CustomOperators
		{
			// Unary operators
			[Kept]
			public static CustomOperators operator + (CustomOperators c) => null;
			[Kept]
			public static CustomOperators operator - (CustomOperators c) => null;
			[Kept]
			public static CustomOperators operator ! (CustomOperators c) => null;
			[Kept]
			public static CustomOperators operator ~ (CustomOperators c) => null;
			[Kept]
			public static CustomOperators operator ++ (CustomOperators c) => null;
			[Kept]
			public static CustomOperators operator -- (CustomOperators c) => null;
			[Kept]
			public static bool operator true (CustomOperators c) => true;
			[Kept]
			public static bool operator false (CustomOperators c) => true;

			// Binary operators
			[Kept]
			public static CustomOperators operator + (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator - (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator * (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator / (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator % (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator & (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator | (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator ^ (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator << (CustomOperators value, int shift) => null;
			[Kept]
			public static CustomOperators operator >> (CustomOperators value, int shift) => null;
			[Kept]
			public static CustomOperators operator == (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator != (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator < (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator > (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator <= (CustomOperators left, CustomOperators right) => null;
			[Kept]
			public static CustomOperators operator >= (CustomOperators left, CustomOperators right) => null;

			// conversion operators
			[Kept]
			public static implicit operator TargetTypeImplicit (CustomOperators self) => null;
			[Kept]
			public static implicit operator CustomOperators (SourceTypeImplicit other) => null;
			[Kept]
			public static explicit operator TargetTypeExplicit (CustomOperators self) => null;
			[Kept]
			public static explicit operator CustomOperators (SourceTypeExplicit other) => null;
		}

		[Kept]
		class TargetTypeImplicit { }
		[Kept]
		class SourceTypeImplicit { }
		[Kept]
		class TargetTypeExplicit { }
		[Kept]
		class SourceTypeExplicit { }

		class GenericCustomOperators<T>
		{
			[Kept]
			public static explicit operator GenericCustomOperators<T> (int i) => null;

			[Kept]
			public static explicit operator GenericCustomOperators<T> (T t) => null;

			[Kept]
			public static explicit operator GenericCustomOperators<T> (int[] i) => null;

			[Kept]
			public static explicit operator GenericCustomOperators<T> (T[] ts) => null;
		}

		[Kept]
		class GenericTypeArgument { }
	}
}
