// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    public abstract class SharedBlockTests
    {
        private static IEnumerable<object> ObjectAssignableConstantValues()
        {
            yield return new object();
            yield return "Hello";
            yield return new Uri("http://example.net/");
        }
        private static IEnumerable<object> ConstantValues()
        {
            yield return 42;
            yield return 42L;
            yield return DateTime.MinValue;
            foreach (object obj in ObjectAssignableConstantValues())
                yield return obj;
        }

        public static IEnumerable<object[]> ConstantValueData()
        {
            return ConstantValues().Select(i => new object[] { i });
        }

        public static IEnumerable<object[]> ConstantValuesAndSizes()
        {
            return
                from size in Enumerable.Range(1, 6)
                from value in ConstantValues()
                select new object[] { value, size };
        }

        public static IEnumerable<object[]> ObjectAssignableConstantValuesAndSizes()
        {
            return
                from size in Enumerable.Range(1, 6)
                from value in ObjectAssignableConstantValues()
                select new object[] { value, size };
        }

        public static IEnumerable<object[]> BlockSizes()
        {
            return Enumerable.Range(1, 6).Select(i => new object[] { i });
        }

        protected static IEnumerable<Expression> PadBlock(int padCount, Expression tailExpression)
        {
            while (padCount-- != 0) yield return Expression.Empty();
            yield return tailExpression;
        }

        protected class TestVistor : ExpressionVisitor
        {
            protected override Expression VisitDefault(DefaultExpression node)
            {
                return Expression.Default(node.Type);
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                return Expression.Constant(node.Value, node.Type);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return Expression.Parameter(node.Type.IsByRef ? node.Type.MakeByRefType() : node.Type, node.Name);
            }
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        protected static Expression UnreadableExpression
        {
            get
            {
                return Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            }
        }
    }
}
