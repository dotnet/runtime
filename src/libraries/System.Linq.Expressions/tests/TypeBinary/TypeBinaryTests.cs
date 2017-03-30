// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    public class TypeBinaryTests
    {
        protected class TypeBinaryVisitCheckingVisitor : ExpressionVisitor
        {
            public TypeBinaryExpression LastTypeBinaryVisited { get; private set; }
            protected override Expression VisitTypeBinary(TypeBinaryExpression node)
            {
                LastTypeBinaryVisited = node;
                return base.VisitTypeBinary(node);
            }
        }

        protected static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        protected enum ByteBased : byte
        {
            A, B, C, D
        }

        protected enum SByteBased : byte
        {
            A, B, C, D
        }

        private static IEnumerable<Type> Types
        {
            get
            {
                return new[] { typeof(void), typeof(int), typeof(object), typeof(string), typeof(long), typeof(Type), typeof(Uri), typeof(BinaryExpression), typeof(Expression), typeof(DateTime), typeof(DateTime?), typeof(IComparable), typeof(ByteBased), typeof(ByteBased?), typeof(SByteBased), typeof(StringComparison) };
            }
        }

        private static IEnumerable<Expression> Constants
        {
            get
            {
                foreach (var type in Types)
                    yield return Expression.Default(type);
                yield return Expression.Constant(1);
                yield return Expression.Constant(DateTime.MinValue);
                yield return Expression.Constant("hello");
                yield return Expression.Constant("hello", typeof(object));
                yield return Expression.Constant(Expression.Empty());
                yield return Expression.Constant(Expression.And(Expression.Constant(1), Expression.Constant(2)));
                yield return Expression.Constant(typeof(int));
                yield return Expression.New(typeof(string).GetConstructor(new[] { typeof(char[]) }), Expression.Constant(new[] { 't', 'e', 's', 't' }));
                yield return Expression.Convert(Expression.Constant(DateTime.MaxValue), typeof(DateTime?));
                yield return Expression.Constant(1, typeof(int?));
                yield return Expression.Constant(1, typeof(object));
                yield return Expression.Constant(1, typeof(IComparable));
                yield return Expression.Constant(DateTime.MinValue, typeof(object));
                yield return Expression.Constant(1, typeof(ValueType));
                yield return Expression.Constant(DateTime.MinValue, typeof(ValueType));
                yield return Expression.Constant(DateTime.MinValue, typeof(IComparable));
                yield return Expression.Constant(ByteBased.A);
                yield return Expression.Constant(ByteBased.D, typeof(ByteBased?));
                yield return Expression.Constant(ByteBased.B, typeof(object));
                yield return Expression.Constant(ByteBased.C, typeof(Enum));
                yield return Expression.Constant(ByteBased.C, typeof(IComparable));
                yield return Expression.Constant(SByteBased.A);
                yield return Expression.Constant(SByteBased.B, typeof(object));
                yield return Expression.Constant(SByteBased.C, typeof(Enum));
                yield return Expression.Constant(SByteBased.C, typeof(IComparable));
                yield return Expression.Constant(StringComparison.CurrentCulture);
                yield return Expression.Constant(StringComparison.CurrentCultureIgnoreCase, typeof(object));
                yield return Expression.Constant(StringComparison.Ordinal, typeof(Enum));
                yield return Expression.Constant(StringComparison.OrdinalIgnoreCase, typeof(IComparable));
                yield return Expression.Constant(new NullReferenceException(), typeof(Exception));
            }
        }

        public static IEnumerable<object[]> ExpressionAndTypeCombinations
        {
            get
            {
                return Types.SelectMany(t => Constants, (t, c) => new object[] { c, t });
            }
        }

        public static IEnumerable<object[]> TypeArguments => Types.Select(t => new object[] {t});
    }
}
