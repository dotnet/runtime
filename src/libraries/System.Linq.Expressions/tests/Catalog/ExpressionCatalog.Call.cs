// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        public static IEnumerable<KeyValuePair<ExpressionType, Expression>> Call_Misc()
        {
            var concat = ((MethodCallExpression)((Expression<Func<object, string, string>>)((o, s) => string.Concat(o, s))).Body).Method;
            var add = ((MethodCallExpression)((Expression<Func<int, int, int>>)((a, b) => C.Add(a, b))).Body).Method;

            var ex = new Exception("Oops!");
            var err = Expression.Throw(Expression.Constant(ex), typeof(int));

            var argss = new Expression[][]
            {
                new Expression[] { Expression.Constant(1), Expression.Constant(2) },
                new Expression[] { Expression.Constant(1), err },
                new Expression[] { err, Expression.Constant(2) },
                new Expression[] { err, err },
            };

            foreach (var args in argss)
            {
                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Call, Expression.Call(add, args));
            }

            foreach (var args in argss)
            {
                var v = Expression.Parameter(typeof(int));

                yield return WithLogExpr(ExpressionType.Call, (log, summary) =>
                    Expression.Block(
                        new[] { v },
                        Expression.Assign(v, Expression.Call(add, args.Select((arg, i) => Expression.Block(Expression.Invoke(log, Expression.Constant("Arg" + i)), arg)))),
                        Expression.Call(
                            concat,
                            Expression.Convert(v, typeof(object)),
                            Expression.Invoke(summary)
                        )
                    )
                );
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Call_ByRef()
        {
            var res = Expression.Parameter(typeof(int));
            var mtd = typeof(C).GetTypeInfo().GetDeclaredMethod("Foo");
            var toStringTemplate = (Expression<Func<int, string>>)(x => x.ToString());
            var concatIntStringTemplate = (Expression<Func<int, string, string>>)((x, s) => x + s);
            var concatIntStringStringTemplate = (Expression<Func<int, string, string, string>>)((x, s1, s2) => x + s1 + s2);

            {
                var p = Expression.Parameter(typeof(int));

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Call,
                    Expression.Block(
                        new[] { res, p },
                        Expression.Assign(p, Expression.Constant(42)),
                        Expression.Assign(
                            res,
                            Expression.Call(
                                mtd,
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
                    WithLogExpr(ExpressionType.Call, (add, summarize) =>
                        Expression.Block(
                            new[] { res, p },
                            Expression.Assign(
                                p,
                                Expression.New(typeof(HolderWithLog<int>).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(Action<string>) }), add)
                            ),
                            Expression.Assign(
                                res,
                                Expression.Call(
                                    mtd,
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

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Call,
                    Expression.Block(
                        new[] { res, p },
                        Expression.Assign(
                            p,
                            Expression.NewArrayBounds(typeof(int), Expression.Constant(1))
                        ),
                        Expression.Assign(
                            res,
                            Expression.Call(
                                mtd,
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
                    WithLogExpr(ExpressionType.Call, (add, summarize) =>
                        Expression.Block(
                            new[] { res, p },
                            Expression.Assign(
                                p,
                                Expression.New(typeof(VectorWithLog<int>).GetTypeInfo().GetDeclaredConstructor(new[] { typeof(Action<string>) }), add)
                            ),
                            Expression.Assign(
                                res,
                                Expression.Call(
                                    mtd,
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

        // TODO: more writebacks and by-ref tests
        // TODO: tests for special methods that can't just be call'd or callvirt'd

        class C
        {
            public static int Add(int a, int b)
            {
                return a + b;
            }

            public static int Foo(string x, ref int y, bool z)
            {
                y = y + 1;
                return y * 2;
            }
        }
    }
}