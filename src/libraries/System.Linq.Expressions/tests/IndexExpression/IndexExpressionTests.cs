// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class IndexExpressionTests
    {
        [Fact]
        public void UpdateSameTest()
        {
            var instance = new SampleClassWithProperties { DefaultProperty = new List<int> { 100, 101 } };
            IndexExpression expr = instance.DefaultIndexExpression;

            IndexExpression exprUpdated = expr.Update(expr.Object, expr.Arguments);

            // Has to be the same, because everything is the same.
            Assert.Same(expr, exprUpdated);

            // Invoke to check expression.
            IndexExpressionHelpers.AssertInvokeCorrect(100, expr);
            IndexExpressionHelpers.AssertInvokeCorrect(100, exprUpdated);
        }

        [Fact]
        public void UpdateTest()
        {
            var instance = new SampleClassWithProperties
            {
                DefaultProperty = new List<int> { 100, 101 },
                AlternativeProperty = new List<int> { 200, 201 }
            };

            IndexExpression expr = instance.DefaultIndexExpression;
            MemberExpression newProperty = Expression.Property(Expression.Constant(instance),
                typeof(SampleClassWithProperties).GetProperty(nameof(instance.AlternativeProperty)));
            ConstantExpression[] newArguments = {Expression.Constant(1)};

            IndexExpression exprUpdated = expr.Update(newProperty, newArguments);

            // Replace Object and Arguments of IndexExpression.
            IndexExpressionHelpers.AssertEqual(
                exprUpdated,
                Expression.MakeIndex(newProperty, instance.DefaultIndexer, newArguments));

            // Invoke to check expression.
            IndexExpressionHelpers.AssertInvokeCorrect(100, expr);
            IndexExpressionHelpers.AssertInvokeCorrect(201, exprUpdated);
        }

        [Fact]
        public static void ToStringTest()
        {
            var e1 = Expression.MakeIndex(Expression.Parameter(typeof(Vector1), "v"), typeof(Vector1).GetProperty("Item"), new[] { Expression.Parameter(typeof(int), "i") });
            Assert.Equal("v.Item[i]", e1.ToString());

            var e2 = Expression.MakeIndex(Expression.Parameter(typeof(Vector2), "v"), typeof(Vector2).GetProperty("Item"), new[] { Expression.Parameter(typeof(int), "i"), Expression.Parameter(typeof(int), "j") });
            Assert.Equal("v.Item[i, j]", e2.ToString());

            var e3 = Expression.ArrayAccess(Expression.Parameter(typeof(int[,]), "xs"), Expression.Parameter(typeof(int), "i"), Expression.Parameter(typeof(int), "j"));
            Assert.Equal("xs[i, j]", e3.ToString());
        }

        class Vector1
        {
            public int this[int x]
            {
                get { return 0; }
            }
        }

        class Vector2
        {
            public int this[int x, int y]
            {
                get { return 0; }
            }
        }
    }
}
