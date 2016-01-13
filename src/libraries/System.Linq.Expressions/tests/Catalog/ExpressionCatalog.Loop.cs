// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Loop()
        {
            var intToString = ((MethodCallExpression)((Expression<Func<int, string>>)(x => x.ToString())).Body).Method;

            var b = Expression.Label();
            var c = Expression.Label();
            var i = Expression.Parameter(typeof(int));

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Loop,
                Expression.Block(
                    new[] { i },
                    Expression.Loop(
                        Expression.Block(
                            Expression.IfThen(
                                Expression.GreaterThanOrEqual(i, Expression.Constant(10)),
                                Expression.Break(b)
                            ),
                            Expression.PostIncrementAssign(i)
                        ),
                        b, c
                    ),
                    Expression.Multiply(i, Expression.Constant(2))
                )
            );

            yield return WithLog(ExpressionType.Loop, (add, summary) =>
                Expression.Block(
                    new[] { i },
                    Expression.Assign(i, Expression.Constant(0)),
                    add(Expression.Constant("LB")),
                    Expression.Loop(
                        Expression.Block(
                            add(Expression.Constant("LIB")),
                            Expression.IfThen(
                                Expression.GreaterThanOrEqual(i, Expression.Constant(10)),
                                Expression.Break(b)
                            ),
                            add(Expression.Call(i, intToString)),
                            Expression.PostIncrementAssign(i),
                            add(Expression.Constant("LIE"))
                        ),
                        b, c
                    ),
                    add(Expression.Constant("LE")),
                    summary
                )
            );

            yield return WithLog(ExpressionType.Loop, (add, summary) =>
                Expression.Block(
                    new[] { i },
                    Expression.Assign(i, Expression.Constant(0)),
                    add(Expression.Constant("LB")),
                    Expression.Loop(
                        Expression.Block(
                            add(Expression.Constant("LIB")),
                            Expression.IfThen(
                                Expression.GreaterThanOrEqual(i, Expression.Constant(10)),
                                Expression.Break(b)
                            ),
                            Expression.IfThen(
                                Expression.Equal(Expression.Modulo(i, Expression.Constant(2)), Expression.Constant(0)),
                                Expression.Block(
                                    Expression.PostIncrementAssign(i),
                                    Expression.Continue(c)
                                )
                            ),
                            add(Expression.Call(i, intToString)),
                            Expression.PostIncrementAssign(i),
                            add(Expression.Constant("LIE"))
                        ),
                        b, c
                    ),
                    add(Expression.Constant("LE")),
                    summary
                )
            );

            // TODO: typed labels
        }
    }
}