// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Invoke()
        {
            var p = Expression.Parameter(typeof(int));
            var i = Expression.Parameter(typeof(Action));
            var d = Expression.Parameter(typeof(Action));

            var cs = new[] { i, i, i, d, i, d, d, i };

            for (var j = 1; j < cs.Length; j++)
            {
                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Invoke,
                    Expression.Block(
                        new[] { p, i, d },
                        Expression.Assign(i, Expression.Lambda<Action>(Expression.PostIncrementAssign(p))),
                        Expression.Assign(d, Expression.Lambda<Action>(Expression.PostDecrementAssign(p))),
                        Expression.Block(cs.Take(j).Select(e => Expression.Invoke(e))),
                        p
                    )
                );
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Invoke_ByRef()
        {
            var q = Expression.Parameter(typeof(int).MakeByRefType());
            var f = Expression.Lambda<IntByRef>(Expression.Assign(q, Expression.Constant(43)), q);

            var concatIntStringTemplate = (Expression<Func<int, string, string>>)((x, s) => x + s);

            {
                var p = Expression.Parameter(typeof(int));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Invoke,
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(p, Expression.Constant(42)),
                        Expression.Invoke(f, p),
                        p
                    )
                );
            }

            {
                var p = Expression.Parameter(typeof(HolderWithLog<int>));
                var value = Expression.Property(p, "Value");

                yield return
                    WithLogExpr(ExpressionType.Invoke, (add, summarize) =>
                        Expression.Block(
                            new[] { p },
                            Expression.Assign(
                                p,
                                Expression.New(typeof(HolderWithLog<int>).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(Action<string>) }), add)
                            ),
                            Expression.Invoke(f, value),
                            Expression.Invoke(
                                concatIntStringTemplate,
                                value,
                                Expression.Invoke(summarize)
                            )
                        )
                    );
            }

            {
                var p = Expression.Parameter(typeof(int[]));
                var value = Expression.ArrayAccess(p, Expression.Constant(0));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Invoke,
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.NewArrayBounds(typeof(int), Expression.Constant(1))
                        ),
                        Expression.Invoke(f, value),
                        value
                    )
                );
            }

            {
                var p = Expression.Parameter(typeof(VectorWithLog<int>));
                var value = Expression.MakeIndex(p, p.Type.GetTypeInfo().GetDeclaredProperty("Item"), new[] { Expression.Constant(0) });

                yield return
                    WithLogExpr(ExpressionType.Invoke, (add, summarize) =>
                        Expression.Block(
                            new[] { p },
                            Expression.Assign(
                                p,
                                Expression.New(typeof(VectorWithLog<int>).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(Action<string>) }), add)
                            ),
                            Expression.Invoke(f, value),
                            Expression.Invoke(
                                concatIntStringTemplate,
                                value,
                                Expression.Invoke(summarize)
                            )
                        )
                    );
            }
        }

        delegate void IntByRef(ref int x);
    }
}