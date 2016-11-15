// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class IndexExpressionVisitorTests
    {
        private class IndexVisitor : ExpressionVisitor
        {
            private readonly Dictionary<Expression, Expression> _dict;

            public IndexVisitor(IndexExpression expr, Expression newObject, Expression[] newArguments)
            {
                _dict = new Dictionary<Expression, Expression>
                {
                    {expr.Object, newObject}
                };

                for (int i = 0; i < expr.Arguments.Count; i++)
                {
                    _dict.Add(expr.Arguments[i], newArguments?[i]);
                }
            }

            public override Expression Visit(Expression node)
            {
                Expression result;
                return _dict.TryGetValue(node, out result) ? result : base.Visit(node);
            }
        }

        [Fact]
        public void RewriteObjectTest()
        {
            var instance = new SampleClassWithProperties
            {
                DefaultProperty = new List<int> { 100, 101 },
                AlternativeProperty = new List<int> { 200, 201 }
            };

            IndexExpression expr = instance.DefaultIndexExpression;
            MemberExpression newProperty = Expression.Property(Expression.Constant(instance),
                typeof(SampleClassWithProperties).GetProperty(nameof(instance.AlternativeProperty)));

            var visitor = new IndexVisitor(expr, newProperty, expr.Arguments.ToArray());
            IndexExpression actual = (IndexExpression)visitor.Visit(expr);
            IndexExpression expected = Expression.MakeIndex(newProperty, expr.Indexer, expr.Arguments);

            // Object of ExpressionIndex replaced via Rewrite method call.
            IndexExpressionHelpers.AssertEqual(expected, actual);

            // Invoke to check expression.
            IndexExpressionHelpers.AssertInvokeCorrect(100, expr);
            IndexExpressionHelpers.AssertInvokeCorrect(200, actual);
        }

        [Fact]
        public void RewriteArgumentsTest()
        {
            var instance = new SampleClassWithProperties {DefaultProperty = new List<int> {100, 101}};

            IndexExpression expr = instance.DefaultIndexExpression;
            Expression[] newArguments = {Expression.Constant(1)};

            var visitor = new IndexVisitor(expr, expr.Object, newArguments);
            IndexExpression expected = Expression.MakeIndex(expr.Object, expr.Indexer, newArguments);
            var actual = (IndexExpression) visitor.Visit(expr);

            IndexExpressionHelpers.AssertEqual(expected, actual);

            // Invoke to check expression.
            IndexExpressionHelpers.AssertInvokeCorrect(100, expr);
            IndexExpressionHelpers.AssertInvokeCorrect(101, actual);
        }
    }
}
