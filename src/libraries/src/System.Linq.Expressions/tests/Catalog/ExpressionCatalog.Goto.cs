// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Goto()
        {
            var l1 = Expression.Label();
            var l2 = Expression.Label();
            var l3 = Expression.Label();
            var b = Expression.Parameter(typeof(bool));

            yield return WithLog(ExpressionType.Goto, (add, summary) =>
                Expression.Block(
                    add(Expression.Constant("B1")),
                    Expression.Label(l1),
                    add(Expression.Constant("B2")),
                    Expression.Label(l2),
                    add(Expression.Constant("B3")),
                    summary
                )
            );

            yield return WithLog(ExpressionType.Goto, (add, summary) =>
                Expression.Block(
                    add(Expression.Constant("B1")),
                    Expression.Goto(l2),
                    add(Expression.Constant("B2")),
                    Expression.Label(l2),
                    add(Expression.Constant("B3")),
                    summary
                )
            );

            yield return WithLog(ExpressionType.Goto, (add, summary) =>
                Expression.Block(
                    new[] { b },
                    Expression.Assign(b, Expression.Constant(false)),
                    add(Expression.Constant("B1")),
                    Expression.Label(l1),
                    add(Expression.Constant("B2")),
                    Expression.IfThenElse(
                        b,
                        Expression.Goto(l3),
                        Expression.Assign(b, Expression.Constant(true))
                    ),
                    Expression.Goto(l1),
                    Expression.Label(l3),
                    add(Expression.Constant("B3")),
                    summary
                )
            );

            // TODO: goto with values
            // TODO: branches across blocks and exception handlers
        }
    }
}