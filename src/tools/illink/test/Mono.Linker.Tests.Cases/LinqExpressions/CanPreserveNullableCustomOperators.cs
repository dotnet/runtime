using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinqExpressions
{
	[SetupCompileArgument ("/unsafe")]
	[SetupLinkerArgument ("--used-attrs-only")]
	public class CanPreserveNullableCustomOperators
	{
		public static void Main ()
		{
			var expression = typeof (System.Linq.Expressions.Expression);

			var r = typeof (ReferenceTypeOperators);
			var t1 = typeof (TargetReferenceType);
			var t2 = typeof (SourceReferenceType);

			var s = typeof (ValueTypeOperators);
			var t3 = typeof (AdditionValueType);
			var t4 = typeof (TargetValueType);
			var t5 = typeof (SourceValueType);

			var s2 = typeof (ValueTypeUnusedOperators);

			var e = typeof (ArrayElementValueType);
			var p = typeof (PointerElementValueType);
			var f = typeof (FunctionPointerArgumentValueType);
		}

		class ReferenceTypeOperators
		{
			[Kept]
			public static ReferenceTypeOperators operator + (ReferenceTypeOperators? c) => null;
			[Kept]
			public static bool operator true (ReferenceTypeOperators? c) => true;
			[Kept]
			public static bool operator false (ReferenceTypeOperators? c) => true;
			[Kept]
			public static ReferenceTypeOperators? operator + (ReferenceTypeOperators? left, ReferenceTypeOperators? right) => null;
			[Kept]
			public static explicit operator TargetReferenceType (ReferenceTypeOperators? self) => null;
			[Kept]
			public static explicit operator ReferenceTypeOperators (SourceReferenceType? other) => null;
		}

		[Kept]
		class TargetReferenceType { }
		[Kept]
		class SourceReferenceType { }

		struct ValueTypeOperators
		{
			[Kept]
			public static ValueTypeOperators operator + (ValueTypeOperators? c) => default (ValueTypeOperators);
			[Kept]
			public static ValueTypeOperators? operator - (ValueTypeOperators? c) => null;
			[Kept]
			public static bool operator true (ValueTypeOperators? c) => true;
			[Kept]
			public static bool operator false (ValueTypeOperators? c) => true;
			[Kept]
			public static ValueTypeOperators? operator + (ValueTypeOperators? left, ValueTypeOperators? right) => null;
			[Kept]
			public static ValueTypeOperators? operator + (ValueTypeOperators? left, AdditionValueType? right) => null;
			[Kept]
			public static explicit operator TargetValueType? (ValueTypeOperators? self) => null;
			[Kept]
			public static explicit operator ValueTypeOperators? (SourceValueType? other) => null;

			[Kept]
			public static ValueTypeOperators? operator + (ArrayElementValueType?[] left, ValueTypeOperators? right) => null;

			[Kept]
			public static ValueTypeOperators? operator + (ArrayElementValueType?[][][] left, ValueTypeOperators? right) => null;

			[Kept]
			public static unsafe explicit operator ValueTypeOperators? (PointerElementValueType?* other) => null;

			[Kept]
			public static unsafe explicit operator ValueTypeOperators? (delegate*<FunctionPointerArgumentValueType?, void> other) => null;
		}

		[Kept]
		struct ValueTypeUnusedOperators
		{
			public static ValueTypeUnusedOperators? operator + (ValueTypeUnusedOperators? left, AdditionValueTypeUnused? right) => null;
			public static explicit operator TargetValueTypeUnused? (ValueTypeUnusedOperators? self) => null;
			public static explicit operator ValueTypeUnusedOperators? (SourceValueTypeUnused? other) => null;
		}

		[Kept]
		struct AdditionValueType { }
		[Kept]
		struct TargetValueType { }
		[Kept]
		struct SourceValueType { }

		struct AdditionValueTypeUnused { }
		struct TargetValueTypeUnused { }
		struct SourceValueTypeUnused { }

		[Kept]
		struct ArrayElementValueType { }

		[Kept]
		struct PointerElementValueType { }

		[Kept]
		struct FunctionPointerArgumentValueType { }
	}
}
