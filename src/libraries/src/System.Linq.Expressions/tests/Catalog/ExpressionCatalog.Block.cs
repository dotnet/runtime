// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> Block()
        {
            yield return Expression.Block(Expression.Constant(1));
            yield return Expression.Block(Expression.Constant("bar"), Expression.Constant(2));
            yield return Expression.Block(Expression.Constant("bar"), Expression.Constant(false), Expression.Constant(3));

            yield return Expression.Block(typeof(int), Expression.Constant(1));
            yield return Expression.Block(typeof(int), Expression.Constant("bar"), Expression.Constant(2));
            yield return Expression.Block(typeof(int), Expression.Constant("bar"), Expression.Constant(false), Expression.Constant(3));

            yield return Expression.Block(typeof(object), Expression.Constant("bar"));
            yield return Expression.Block(typeof(object), Expression.Constant(2), Expression.Constant("bar"));
            yield return Expression.Block(typeof(object), Expression.Constant(false), Expression.Constant(3), Expression.Constant("bar"));

            yield return Expression.Block(typeof(int), new[] { Expression.Parameter(typeof(int)) }, Expression.Constant(1));
            yield return Expression.Block(typeof(int), new[] { Expression.Parameter(typeof(int)) }, Expression.Constant("bar"), Expression.Constant(2));
            yield return Expression.Block(typeof(int), new[] { Expression.Parameter(typeof(int)) }, Expression.Constant("bar"), Expression.Constant(false), Expression.Constant(3));

            yield return Expression.Block(typeof(object), new[] { Expression.Parameter(typeof(int)) }, Expression.Constant("bar"));
            yield return Expression.Block(typeof(object), new[] { Expression.Parameter(typeof(int)) }, Expression.Constant(2), Expression.Constant("bar"));
            yield return Expression.Block(typeof(object), new[] { Expression.Parameter(typeof(int)) }, Expression.Constant(false), Expression.Constant(3), Expression.Constant("bar"));
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Block_WithLog()
        {
            yield return WithLog(ExpressionType.Block, (log, summary) =>
            {
                return Expression.Block(
                    log(Expression.Constant("A")),
                    log(Expression.Constant("B")),
                    log(Expression.Constant("C")),
                    summary
                );
            });
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Block_Primes()
        {
            var to = Expression.Parameter(typeof(int), "to");
            var res = Expression.Variable(typeof(List<int>), "res");
            var n = Expression.Variable(typeof(int), "n");
            var found = Expression.Variable(typeof(bool), "found");
            var d = Expression.Variable(typeof(int), "d");
            var breakOuter = Expression.Label();
            var breakInner = Expression.Label();
            var getPrimes =
                // Func<int, List<int>> getPrimes =
                Expression.Lambda<Func<int, List<int>>>(
                    // {
                    Expression.Block(
                        // List<int> res;
                        new[] { res },
                        // res = new List<int>();
                        Expression.Assign(
                            res,
                            Expression.New(typeof(List<int>))
                        ),
                        // {
                        Expression.Block(
                            // int n;
                            new[] { n },
                            // n = 2;
                            Expression.Assign(
                                n,
                                Expression.Constant(2)
                            ),
                            // while (true)
                            Expression.Loop(
                                // {
                                Expression.Block(
                                    // if
                                    Expression.IfThen(
                                        // (!
                                        Expression.Not(
                                            // (n <= to)
                                            Expression.LessThanOrEqual(
                                                n,
                                                to
                                            )
                                        // )
                                        ),
                                        // break;
                                        Expression.Break(breakOuter)
                                    ),
                                    // {
                                    Expression.Block(
                                        // bool found;
                                        new[] { found },
                                        // found = false;
                                        Expression.Assign(
                                            found,
                                            Expression.Constant(false)
                                        ),
                                        // {
                                        Expression.Block(
                                            // int d;
                                            new[] { d },
                                            // d = 2;
                                            Expression.Assign(
                                                d,
                                                Expression.Constant(2)
                                            ),
                                            // while (true)
                                            Expression.Loop(
                                                // {
                                                Expression.Block(
                                                    // if
                                                    Expression.IfThen(
                                                        // (!
                                                        Expression.Not(
                                                            // d <= Math.Sqrt(n)
                                                            Expression.LessThanOrEqual(
                                                                d,
                                                                Expression.Convert(
                                                                    Expression.Call(
                                                                        null,
                                                                        typeof(Math).GetTypeInfo().GetDeclaredMethod("Sqrt"),
                                                                        Expression.Convert(
                                                                            n,
                                                                            typeof(double)
                                                                        )
                                                                    ),
                                                                    typeof(int)
                                                                )
                                                            )
                                                        // )
                                                        ),
                                                        // break;
                                                        Expression.Break(breakInner)
                                                    ),
                                                    // {
                                                    Expression.Block(
                                                        // if (n % d == 0)
                                                        Expression.IfThen(
                                                            Expression.Equal(
                                                                Expression.Modulo(
                                                                    n,
                                                                    d
                                                                ),
                                                                Expression.Constant(0)
                                                            ),
                                                            // {
                                                            Expression.Block(
                                                                // found = true;
                                                                Expression.Assign(
                                                                    found,
                                                                    Expression.Constant(true)
                                                                ),
                                                                // break;
                                                                Expression.Break(breakInner)
                                                            // }
                                                            )
                                                        )
                                                    // }
                                                    ),
                                                    // d++;
                                                    Expression.PostIncrementAssign(d)
                                                // }
                                                ),
                                                breakInner
                                            )
                                        ),
                                        // if
                                        Expression.IfThen(
                                            // (!found)
                                            Expression.Not(found),
                                            //    res.Add(n);
                                            Expression.Call(
                                                res,
                                                typeof(List<int>).GetTypeInfo().GetDeclaredMethod("Add"),
                                                n
                                            )
                                        )
                                    ),
                                    // n++;
                                    Expression.PostIncrementAssign(n)
                                // }
                                ),
                                breakOuter
                            )
                        ),
                        res
                    ),
                    to
                // }
                );

            var joinTemplate = (Expression<Func<List<int>, string>>)(xs => string.Join(", ", xs));

            var expr = Expression.Invoke(joinTemplate, Expression.Invoke(getPrimes, Expression.Constant(100)));

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Block, expr);
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Block_Primes_WithLog()
        {
            var joinTemplate = (Expression<Func<List<int>, string>>)(xs => string.Join(", ", xs));
            var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

            var to = Expression.Parameter(typeof(int), "to");
            var res = Expression.Variable(typeof(List<int>), "res");
            var n = Expression.Variable(typeof(int), "n");
            var found = Expression.Variable(typeof(bool), "found");
            var d = Expression.Variable(typeof(int), "d");
            var breakOuter = Expression.Label();
            var breakInner = Expression.Label();
            var expr =
                WithLog((log, summary) =>
                {
                    var body =
                        // {
                        Expression.Block(
                            // List<int> res;
                            new[] { res, to },
                            // to = 100;
                            Expression.Assign(
                                to,
                                Expression.Constant(100)
                            ),
                            // res = new List<int>();
                            Expression.Assign(
                                res,
                                Expression.New(typeof(List<int>))
                            ),
                            // {
                            Expression.Block(
                                // int n;
                                new[] { n },
                                // n = 2;
                                Expression.Assign(
                                    n,
                                    Expression.Constant(2)
                                ),
                                // while (true)
                                Expression.Loop(
                                    // {
                                    Expression.Block(
                                        // if
                                        Expression.IfThen(
                                            // (!
                                            Expression.Not(
                                                // (n <= to)
                                                Expression.LessThanOrEqual(
                                                    n,
                                                    to
                                                )
                                            // )
                                            ),
                                            // break;
                                            Expression.Break(breakOuter)
                                        ),
                                        // {
                                        Expression.Block(
                                            // bool found;
                                            new[] { found },
                                            // found = false;
                                            Expression.Assign(
                                                found,
                                                Expression.Constant(false)
                                            ),
                                            // {
                                            Expression.Block(
                                                // int d;
                                                new[] { d },
                                                // d = 2;
                                                Expression.Assign(
                                                    d,
                                                    Expression.Constant(2)
                                                ),
                                                // while (true)
                                                Expression.Loop(
                                                    // {
                                                    Expression.Block(
                                                        // if
                                                        Expression.IfThen(
                                                            // (!
                                                            Expression.Not(
                                                                // d <= Math.Sqrt(n)
                                                                Expression.LessThanOrEqual(
                                                                    d,
                                                                    Expression.Convert(
                                                                        Expression.Call(
                                                                            null,
                                                                            typeof(Math).GetTypeInfo().GetDeclaredMethod("Sqrt"),
                                                                            Expression.Convert(
                                                                                n,
                                                                                typeof(double)
                                                                            )
                                                                        ),
                                                                        typeof(int)
                                                                    )
                                                                )
                                                            // )
                                                            ),
                                                            // break;
                                                            Expression.Break(breakInner)
                                                        ),
                                                        // {
                                                        Expression.Block(
                                                            // if (n % d == 0)
                                                            Expression.IfThen(
                                                                Expression.Equal(
                                                                    Expression.Modulo(
                                                                        n,
                                                                        d
                                                                    ),
                                                                    Expression.Constant(0)
                                                                ),
                                                                // {
                                                                Expression.Block(
                                                                    // found = true;
                                                                    Expression.Assign(
                                                                        found,
                                                                        Expression.Constant(true)
                                                                    ),
                                                                    // break;
                                                                    Expression.Break(breakInner)
                                                                // }
                                                                )
                                                            )
                                                        // }
                                                        ),
                                                        // d++;
                                                        Expression.PostIncrementAssign(d)
                                                    // }
                                                    ),
                                                    breakInner
                                                )
                                            ),
                                            // if
                                            Expression.IfThen(
                                                // (!found)
                                                Expression.Not(found),
                                                //    res.Add(n);
                                                Expression.Call(
                                                    res,
                                                    typeof(List<int>).GetTypeInfo().GetDeclaredMethod("Add"),
                                                    n
                                                )
                                            )
                                        ),
                                        // n++;
                                        Expression.PostIncrementAssign(n)
                                    // }
                                    ),
                                    breakOuter
                                )
                            ),
                            Expression.Invoke(
                                concatTemplate,
                                Expression.Invoke(joinTemplate, res),
                                summary
                            )
                        );

                    return new InstrumentWithLog(log).Visit(body);
                });

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Block, expr);
        }

        class InstrumentWithLog : ExpressionVisitor
        {
            private readonly Func<Expression, Expression> _log;
            private int _n;

            public InstrumentWithLog(Func<Expression, Expression> log)
            {
                _log = log;
            }

            protected override Expression VisitBlock(BlockExpression node)
            {
                var exprs = node.Expressions.SelectMany(e => new Expression[] { _log(Expression.Constant("S" + _n++)), Visit(e) }).ToList();
                return node.Update(node.Variables, exprs);
            }
        }
    }
}