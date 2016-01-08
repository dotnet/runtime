// Copyright (c) Jon Hanna. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

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

        private static IEnumerable<Type> Types
        {
            get
            {
                return new[] { typeof(void), typeof(int), typeof(object), typeof(string), typeof(long), typeof(Type), typeof(Uri), typeof(BinaryExpression), typeof(Expression), typeof(DateTime), typeof(DateTime?), typeof(IComparable) };
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
                yield return Expression.Constant(Expression.Empty());
                yield return Expression.Constant(Expression.And(Expression.Constant(1), Expression.Constant(2)));
                yield return Expression.Constant(typeof(int));
                yield return Expression.New(typeof(string).GetConstructor(new[] { typeof(char[]) }), Expression.Constant(new[] { 't', 'e', 's', 't' }));
                yield return Expression.Convert(Expression.Constant(DateTime.MaxValue), typeof(DateTime?));
                yield return Expression.Constant(1, typeof(int?));
            }
        }

        public static IEnumerable<object[]> ExpressionAndTypeCombinations
        {
            get
            {
                return Types.SelectMany(t => Constants, (t, c) => new object[] { c, t });
            }
        }
    }
}
