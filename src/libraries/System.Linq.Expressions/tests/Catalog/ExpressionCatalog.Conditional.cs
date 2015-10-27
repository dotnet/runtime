// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> Conditional()
        {
            var tests = new[] { Expression.Constant(true), Expression.Constant(false) };

            foreach (var test in tests)
            {
                foreach (var values in s_exprs.Values)
                {
                    foreach (var ifTrue in values)
                    {
                        foreach (var ifFalse in values)
                        {
                            yield return Expression.Condition(test, ifTrue, ifFalse);
                        }
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> IfThen_WithLog()
        {
            var tests = new[] { Expression.Constant(true), Expression.Constant(false) };

            foreach (var test in tests)
            {
                yield return WithLog(ExpressionType.Conditional, (log, summary) =>
                {
                    return
                        Expression.Block(
                            Expression.IfThen(test, log(Expression.Constant("T"))),
                            summary
                        );
                });
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> IfThenElse_WithLog()
        {
            var tests = new[] { Expression.Constant(true), Expression.Constant(false) };

            foreach (var test in tests)
            {
                yield return WithLog(ExpressionType.Conditional, (log, summary) =>
                {
                    return
                        Expression.Block(
                            Expression.IfThenElse(test, log(Expression.Constant("T")), log(Expression.Constant("F"))),
                            summary
                        );
                });
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Conditional_WithLog()
        {
            var tests = new[] { Expression.Constant(true), Expression.Constant(false) };

            foreach (var test in tests)
            {
                yield return WithLog(ExpressionType.Conditional, (log, summary) =>
                {
                    var valueParam = Expression.Parameter(typeof(int));
                    var toStringTemplate = (Expression<Func<int, string>>)(x => x.ToString());
                    var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                    return
                        Expression.Block(
                            new[] { valueParam },
                            Expression.Assign(
                                valueParam,
                                Expression.Condition(
                                    ReturnWithLog(log, test),
                                    ReturnWithLog(log, 1),
                                    ReturnWithLog(log, 2)
                                )
                            ),
                            Expression.Invoke(
                                concatTemplate,
                                Expression.Invoke(toStringTemplate, valueParam),
                                summary
                            )
                        );
                });
            }
        }
    }
}