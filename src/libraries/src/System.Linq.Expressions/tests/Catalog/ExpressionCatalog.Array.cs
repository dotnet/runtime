// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> ArrayIndex()
        {
            var arr1 = Expression.Constant(new[] { 2, 3, 5 });

            for (var i = -1; i <= 3; i++)
            {
                yield return Expression.ArrayIndex(arr1, Expression.Constant(i));
            }

            var arr2 = Expression.Constant(new[,] { { 2, 3, 5 }, { 7, 11, 13 } });

            for (var i = -1; i <= 2; i++)
            {
                for (var j = -1; i <= 3; i++)
                {
                    yield return Expression.ArrayIndex(arr2, Expression.Constant(i), Expression.Constant(j));
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> ArrayIndex_WithLog()
        {
            var arr1 = Expression.Constant(new[] { 2, 3, 5 });
            var arr2 = Expression.Constant(new[,] { { 2, 3, 5 }, { 7, 11, 13 } });

            yield return WithLog(ExpressionType.ArrayIndex, (log, summary) =>
            {
                var valueParam = Expression.Parameter(typeof(int));
                var toStringTemplate = (Expression<Func<int, string>>)(x => x.ToString());
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.ArrayIndex(arr1, ReturnWithLog(log, 1))
                        ),
                        Expression.Invoke(
                            concatTemplate,
                            Expression.Invoke(toStringTemplate, valueParam),
                            summary
                        )
                    );
            });

            yield return WithLog(ExpressionType.ArrayIndex, (log, summary) =>
            {
                var valueParam = Expression.Parameter(typeof(int));
                var toStringTemplate = (Expression<Func<int, string>>)(x => x.ToString());
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.ArrayIndex(arr2, ReturnWithLog(log, 1), ReturnWithLog(log, 2))
                        ),
                        Expression.Invoke(
                            concatTemplate,
                            Expression.Invoke(toStringTemplate, valueParam),
                            summary
                        )
                    );
            });
        }

        private static IEnumerable<Expression> ArrayAccess()
        {
            var arr1 = Expression.Constant(new[] { 2, 3, 5 });

            for (var i = -1; i <= 3; i++)
            {
                yield return Expression.ArrayAccess(arr1, Expression.Constant(i));
            }

            var arr2 = Expression.Constant(new[,] { { 2, 3, 5 }, { 7, 11, 13 } });

            for (var i = -1; i <= 2; i++)
            {
                for (var j = -1; i <= 3; i++)
                {
                    yield return Expression.ArrayAccess(arr2, Expression.Constant(i), Expression.Constant(j));
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> ArrayAccess_WithLog()
        {
            var arr1 = Expression.Constant(new[] { 2, 3, 5 });
            var arr2 = Expression.Constant(new[,] { { 2, 3, 5 }, { 7, 11, 13 } });

            yield return WithLog(ExpressionType.Index, (log, summary) =>
            {
                var valueParam = Expression.Parameter(typeof(int));
                var toStringTemplate = (Expression<Func<int, string>>)(x => x.ToString());
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.ArrayAccess(arr1, ReturnWithLog(log, 1))
                        ),
                        Expression.Invoke(
                            concatTemplate,
                            Expression.Invoke(toStringTemplate, valueParam),
                            summary
                        )
                    );
            });

            yield return WithLog(ExpressionType.Index, (log, summary) =>
            {
                var valueParam = Expression.Parameter(typeof(int));
                var toStringTemplate = (Expression<Func<int, string>>)(x => x.ToString());
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.ArrayAccess(arr2, ReturnWithLog(log, 1), ReturnWithLog(log, 2))
                        ),
                        Expression.Invoke(
                            concatTemplate,
                            Expression.Invoke(toStringTemplate, valueParam),
                            summary
                        )
                    );
            });
        }

        private static IEnumerable<Expression> ArrayLength()
        {
            yield return Expression.ArrayLength(Expression.Constant(new object[0]));
            yield return Expression.ArrayLength(Expression.Constant(new int[1]));
            yield return Expression.ArrayLength(Expression.Constant(new string[42]));
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> NewArrayInit()
        {
            var expr = (Expression<Func<int>>)(() => new[] { 2, 3, 5, 7 }.Sum());
            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.NewArrayInit, expr.Body);
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> NewArrayInit_WithLog()
        {
            yield return WithLog(ExpressionType.NewArrayInit, (log, summary) =>
            {
                var valueParam = Expression.Parameter(typeof(int[]));
                var newArrTemplate = (Expression<Func<int[]>>)(() => new[] { 2, 3, 5, 7 });
                var toStringTemplate = (Expression<Func<int[], string>>)(xs => string.Join(", ", xs));
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.Invoke(
                                ReturnAllConstants(log, newArrTemplate)
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

        private static IEnumerable<Expression> NewArrayBounds()
        {
            foreach (var e in new[]
            {
                Expression.NewArrayBounds(typeof(int), Expression.Constant(-1)),
                Expression.NewArrayBounds(typeof(int), Expression.Constant(0)),
                Expression.NewArrayBounds(typeof(int), Expression.Constant(1), Expression.Constant(-1)),
                Expression.NewArrayBounds(typeof(int), Expression.Constant(1), Expression.Constant(0)),
            })
            {
                yield return e;
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> NewArrayBounds_WithLog()
        {
            yield return WithLog(ExpressionType.NewArrayBounds, (log, summary) =>
            {
                var valueParam = Expression.Parameter(typeof(int[]));
                var toStringTemplate = (Expression<Func<int[], string>>)(xs => string.Join(", ", xs));
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.NewArrayBounds(
                                typeof(int),
                                ReturnWithLog(log, 1)
                            )
                        ),
                        Expression.Invoke(
                            concatTemplate,
                            Expression.Invoke(toStringTemplate, valueParam),
                            summary
                        )
                    );
            });

            yield return WithLog(ExpressionType.NewArrayBounds, (log, summary) =>
            {
                var valueParam = Expression.Parameter(typeof(int[,]));
                var toStringTemplate = (Expression<Func<int[,], string>>)(xs => string.Join(", ", xs));
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.NewArrayBounds(
                                typeof(int),
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
