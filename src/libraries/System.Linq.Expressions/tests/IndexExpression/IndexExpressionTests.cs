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
    }
}
