// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> New()
        {
            yield return Expression.New(typeof(TimeSpan));
            yield return Expression.New(typeof(TimeSpan).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(long) }), Expression.Constant(1234567890L));
            yield return Expression.New(typeof(TimeSpan).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(int), typeof(int), typeof(int) }), Expression.Constant(12), Expression.Constant(34), Expression.Constant(56));

            yield return Expression.New(typeof(string).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(char), typeof(int) }), Expression.Constant('*'), Expression.Constant(-1));
            yield return Expression.New(typeof(string).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(char), typeof(int) }), Expression.Constant('*'), Expression.Constant(2));

            yield return ((Expression<Func<object>>)(() => new { })).Body;
            yield return ((Expression<Func<object>>)(() => new { a = 1 })).Body;
            yield return ((Expression<Func<object>>)(() => new { a = 1, b = "foo" })).Body;
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> New_ByRef()
        {
            var res = Expression.Parameter(typeof(N));
            var ctor = typeof(N).GetTypeInfo().DeclaredConstructors.Single();
            var toStringTemplate = (Expression<Func<N, string>>)(x => x.ToString());
            var concatIntStringTemplate = (Expression<Func<int, string, string>>)((x, s) => x + s);
            var concatIntStringStringTemplate = (Expression<Func<int, string, string, string>>)((x, s1, s2) => x + s1 + s2);

            {
                var p = Expression.Parameter(typeof(int));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.New,
                    Expression.Block(
                        new[] { res, p },
                        Expression.Assign(p, Expression.Constant(42)),
                        Expression.Assign(
                            res,
                            Expression.New(
                                ctor,
                                Expression.Constant("bar"),
                                p,
                                Expression.Constant(true)
                            )
                        ),
                        Expression.Invoke(
                            concatIntStringTemplate,
                            p,
                            Expression.Invoke(toStringTemplate, res)
                        )
                    )
                );
            }

            {
                var p = Expression.Parameter(typeof(HolderWithLog<int>));
                var value = Expression.Property(p, "Value");

                yield return
                    WithLogExpr(ExpressionType.New, (add, summarize) =>
                        Expression.Block(
                            new[] { res, p },
                            Expression.Assign(
                                p,
                                Expression.New(typeof(HolderWithLog<int>).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(Action<string>) }), add)
                            ),
                            Expression.Assign(
                                res,
                                Expression.New(
                                    ctor,
                                    ReturnWithLog(add, "bar"),
                                    value,
                                    ReturnWithLog(add, true)
                                )
                            ),
                            Expression.Invoke(
                                concatIntStringStringTemplate,
                                value,
                                Expression.Invoke(toStringTemplate, res),
                                Expression.Invoke(summarize)
                            )
                        )
                    );
            }

            {
                var p = Expression.Parameter(typeof(int[]));
                var value = Expression.ArrayAccess(p, Expression.Constant(0));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.New,
                    Expression.Block(
                        new[] { res, p },
                        Expression.Assign(
                            p,
                            Expression.NewArrayBounds(typeof(int), Expression.Constant(1))
                        ),
                        Expression.Assign(
                            res,
                            Expression.New(
                                ctor,
                                Expression.Constant("bar"),
                                value,
                                Expression.Constant(true)
                            )
                        ),
                        Expression.Invoke(
                            concatIntStringTemplate,
                            value,
                            Expression.Invoke(toStringTemplate, res)
                        )
                    )
                );
            }

            {
                var p = Expression.Parameter(typeof(VectorWithLog<int>));
                var value = Expression.MakeIndex(p, p.Type.GetTypeInfo().GetDeclaredProperty("Item"), new[] { Expression.Constant(0) });

                yield return
                    WithLogExpr(ExpressionType.New, (add, summarize) =>
                        Expression.Block(
                            new[] { res, p },
                            Expression.Assign(
                                p,
                                Expression.New(typeof(VectorWithLog<int>).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(Action<string>) }), add)
                            ),
                            Expression.Assign(
                                res,
                                Expression.New(
                                    ctor,
                                    ReturnWithLog(add, "bar"),
                                    ReturnAllConstants(add, value),
                                    ReturnWithLog(add, true)
                                )
                            ),
                            Expression.Invoke(
                                concatIntStringStringTemplate,
                                value,
                                Expression.Invoke(toStringTemplate, res),
                                Expression.Invoke(summarize)
                            )
                        )
                    );
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> New_WithLog()
        {
            yield return WithLog(ExpressionType.New, (log, summary) =>
            {
                var valueParam = Expression.Parameter(typeof(TimeSpan));
                var toStringTemplate = (Expression<Func<TimeSpan, string>>)(x => x.ToString());
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.New(
                                typeof(TimeSpan).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(int), typeof(int), typeof(int) }),
                                ReturnWithLog(log, 1),
                                ReturnWithLog(log, 2),
                                ReturnWithLog(log, 3)
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

        class N
        {
            private readonly bool _b;
            private readonly string _s;
            private readonly int _x;

            public N(string s, ref int x, bool b)
            {
                _s = s;
                _x = x;
                _b = b;

                x++;
            }

            public override string ToString()
            {
                return "{ s = " + _s + ", x = " + _x + ", b = " + _b + " }";
            }
        }
    }
}