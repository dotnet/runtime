// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> Unbox()
        {
            yield return Expression.Unbox(Expression.Constant(42, typeof(object)), typeof(int));
            yield return Expression.Unbox(Expression.Constant(42, typeof(IEquatable<int>)), typeof(int));

            yield return Expression.Unbox(Expression.Constant(null, typeof(object)), typeof(int));
            yield return Expression.Unbox(Expression.Constant(null, typeof(IEquatable<int>)), typeof(int));

            yield return Expression.Unbox(Expression.Constant(42, typeof(object)), typeof(int?));
            yield return Expression.Unbox(Expression.Constant(42, typeof(IEquatable<int>)), typeof(int?));

            yield return Expression.Unbox(Expression.Constant(null, typeof(object)), typeof(int?));
            yield return Expression.Unbox(Expression.Constant(null, typeof(IEquatable<int>)), typeof(int?));
        }

        private static IEnumerable<Expression> Convert()
        {
            var factories = new Func<Expression, Type, Expression>[]
            {
                (e, t) => Expression.Convert(e, t),
                (e, t) => Expression.ConvertChecked(e, t),
            };

            var convertValueTypes = new[]
            {
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(ushort),
                typeof(short),
                typeof(uint),
                typeof(int),
                typeof(ulong),
                typeof(long),
                typeof(float),
                typeof(double),
                typeof(char),
                typeof(E),
            };

            foreach (var t in convertValueTypes)
            {
                var n = typeof(Nullable<>).MakeGenericType(t);

                foreach (var v in s_values[t])
                {
                    foreach (var f in factories)
                    {
                        yield return f(Expression.Constant(v, t), t);
                        yield return f(Expression.Constant(v, t), n);
                        yield return f(Expression.Constant(v, n), t);
                        yield return f(Expression.Constant(v, n), n);
                        yield return f(Expression.Constant(null, n), t);
                        yield return f(Expression.Constant(null, n), n);

                        yield return f(Expression.Constant(v, typeof(object)), t);
                        yield return f(Expression.Constant(v, typeof(object)), n);
                        yield return f(Expression.Constant(null, typeof(object)), t);
                        yield return f(Expression.Constant(null, typeof(object)), n);
                        yield return f(Expression.Constant(v, t), typeof(object));
                        yield return f(Expression.Constant(v, n), typeof(object));
                        yield return f(Expression.Constant(null, n), typeof(object));
                    }
                }
            }

            var convertEnumTypes = new[]
            {
                typeof(E),
            };

            foreach (var t in convertEnumTypes)
            {
                var tn = typeof(Nullable<>).MakeGenericType(t);
                var u = Enum.GetUnderlyingType(t);
                var un = typeof(Nullable<>).MakeGenericType(u);

                foreach (var v in s_values[t])
                {
                    var i = System.Convert.ChangeType(v, u);

                    foreach (var f in factories)
                    {
                        yield return f(Expression.Constant(v, t), u);
                        yield return f(Expression.Constant(v, tn), u);
                        yield return f(Expression.Constant(null, tn), u);

                        yield return f(Expression.Constant(v, t), un);
                        yield return f(Expression.Constant(v, tn), un);
                        yield return f(Expression.Constant(null, tn), un);

                        yield return f(Expression.Constant(i, u), t);
                        yield return f(Expression.Constant(i, un), t);
                        yield return f(Expression.Constant(null, un), t);

                        yield return f(Expression.Constant(i, u), tn);
                        yield return f(Expression.Constant(i, un), tn);
                        yield return f(Expression.Constant(null, un), tn);
                    }
                }
            }

            foreach (var tf in convertValueTypes)
            {
                var tfn = typeof(Nullable<>).MakeGenericType(tf);

                foreach (var tt in convertValueTypes)
                {
                    if (tt == typeof(bool))
                    {
                        continue;
                    }

                    var ttn = typeof(Nullable<>).MakeGenericType(tt);

                    foreach (var v in s_values[tf])
                    {
                        foreach (var f in factories)
                        {
                            yield return f(Expression.Constant(v, tf), tt);
                            yield return f(Expression.Constant(v, tf), ttn);
                            yield return f(Expression.Constant(v, tfn), tt);
                            yield return f(Expression.Constant(v, tfn), ttn);
                            yield return f(Expression.Constant(null, tfn), tt);
                            yield return f(Expression.Constant(null, tfn), ttn);
                        }
                    }
                }
            }

            foreach (var f in factories)
            {
                yield return f(Expression.Constant(null, typeof(string)), typeof(string));
                yield return f(Expression.Constant(null, typeof(string)), typeof(object));
                yield return f(Expression.Constant(null, typeof(object)), typeof(string));

                yield return f(Expression.Constant("bar", typeof(string)), typeof(string));
                yield return f(Expression.Constant("bar", typeof(string)), typeof(object));
                yield return f(Expression.Constant("bar", typeof(object)), typeof(string));

                yield return f(Expression.Constant("bar", typeof(object)), typeof(int));
                yield return f(Expression.Constant("bar", typeof(object)), typeof(int?));
                yield return f(Expression.Constant("bar", typeof(object)), typeof(Array));

                yield return f(Expression.Constant("bar", typeof(object)), typeof(IComparable));
                yield return f(Expression.Constant("bar", typeof(object)), typeof(IEnumerable));
                yield return f(Expression.Constant("bar", typeof(object)), typeof(IEnumerable<char>));

                yield return f(Expression.Constant("bar", typeof(string)), typeof(IComparable));
                yield return f(Expression.Constant("bar", typeof(string)), typeof(IEnumerable));
                yield return f(Expression.Constant("bar", typeof(string)), typeof(IEnumerable<char>));
            }

            foreach (var f in factories)
            {
                yield return f(Expression.Constant(DateTime.Now, typeof(DateTime)), typeof(DateTimeOffset));
                yield return f(Expression.Constant(DateTime.Now, typeof(DateTime)), typeof(DateTimeOffset?));
                yield return f(Expression.Constant(DateTime.Now, typeof(DateTime?)), typeof(DateTimeOffset));
                yield return f(Expression.Constant(DateTime.Now, typeof(DateTime?)), typeof(DateTimeOffset?));
                yield return f(Expression.Constant(null, typeof(DateTime?)), typeof(DateTimeOffset));
                yield return f(Expression.Constant(null, typeof(DateTime?)), typeof(DateTimeOffset?));
            }
        }

        private static IEnumerable<Expression> TypeAs()
        {
            var convertValueTypes = new[]
            {
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(ushort),
                typeof(short),
                typeof(uint),
                typeof(int),
                typeof(ulong),
                typeof(long),
                typeof(float),
                typeof(double),
                typeof(char),
                typeof(E),
            };

            foreach (var t in convertValueTypes)
            {
                var n = typeof(Nullable<>).MakeGenericType(t);

                foreach (var v in s_values[t])
                {
                    yield return Expression.TypeAs(Expression.Constant(v, t), n);
                    yield return Expression.TypeAs(Expression.Constant(v, n), n);
                    yield return Expression.TypeAs(Expression.Constant(null, n), n);

                    yield return Expression.TypeAs(Expression.Constant(v, typeof(object)), n);
                    yield return Expression.TypeAs(Expression.Constant(null, typeof(object)), n);
                    yield return Expression.TypeAs(Expression.Constant(v, t), typeof(object));
                    yield return Expression.TypeAs(Expression.Constant(v, n), typeof(object));
                    yield return Expression.TypeAs(Expression.Constant(null, n), typeof(object));
                }
            }

            var convertEnumTypes = new[]
            {
                typeof(E),
            };

            foreach (var t in convertEnumTypes)
            {
                var tn = typeof(Nullable<>).MakeGenericType(t);
                var u = Enum.GetUnderlyingType(t);
                var un = typeof(Nullable<>).MakeGenericType(u);

                foreach (var v in s_values[t])
                {
                    var i = System.Convert.ChangeType(v, u);

                    yield return Expression.TypeAs(Expression.Constant(v, t), un);
                    yield return Expression.TypeAs(Expression.Constant(v, tn), un);
                    yield return Expression.TypeAs(Expression.Constant(null, tn), un);

                    yield return Expression.TypeAs(Expression.Constant(i, u), tn);
                    yield return Expression.TypeAs(Expression.Constant(i, un), tn);
                    yield return Expression.TypeAs(Expression.Constant(null, un), tn);
                }
            }

            foreach (var tf in convertValueTypes)
            {
                var tfn = typeof(Nullable<>).MakeGenericType(tf);

                foreach (var tt in convertValueTypes)
                {
                    if (tt == typeof(bool))
                    {
                        continue;
                    }

                    var ttn = typeof(Nullable<>).MakeGenericType(tt);

                    foreach (var v in s_values[tf])
                    {
                        yield return Expression.TypeAs(Expression.Constant(v, tf), ttn);
                        yield return Expression.TypeAs(Expression.Constant(v, tfn), ttn);
                        yield return Expression.TypeAs(Expression.Constant(null, tfn), ttn);
                    }
                }
            }

            {
                yield return Expression.TypeAs(Expression.Constant(null, typeof(string)), typeof(string));
                yield return Expression.TypeAs(Expression.Constant(null, typeof(string)), typeof(object));
                yield return Expression.TypeAs(Expression.Constant(null, typeof(object)), typeof(string));

                yield return Expression.TypeAs(Expression.Constant("bar", typeof(string)), typeof(string));
                yield return Expression.TypeAs(Expression.Constant("bar", typeof(string)), typeof(object));
                yield return Expression.TypeAs(Expression.Constant("bar", typeof(object)), typeof(string));

                yield return Expression.TypeAs(Expression.Constant("bar", typeof(object)), typeof(int?));
                yield return Expression.TypeAs(Expression.Constant("bar", typeof(object)), typeof(Array));

                yield return Expression.TypeAs(Expression.Constant("bar", typeof(object)), typeof(IComparable));
                yield return Expression.TypeAs(Expression.Constant("bar", typeof(object)), typeof(IEnumerable));
                yield return Expression.TypeAs(Expression.Constant("bar", typeof(object)), typeof(IEnumerable<char>));

                yield return Expression.TypeAs(Expression.Constant("bar", typeof(string)), typeof(IComparable));
                yield return Expression.TypeAs(Expression.Constant("bar", typeof(string)), typeof(IEnumerable));
                yield return Expression.TypeAs(Expression.Constant("bar", typeof(string)), typeof(IEnumerable<char>));
            }

            {
                yield return Expression.TypeAs(Expression.Constant(DateTime.Now, typeof(DateTime)), typeof(DateTimeOffset?));
                yield return Expression.TypeAs(Expression.Constant(DateTime.Now, typeof(DateTime?)), typeof(DateTimeOffset?));
                yield return Expression.TypeAs(Expression.Constant(null, typeof(DateTime?)), typeof(DateTimeOffset?));
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Quote()
        {
            var expr = (Expression<Func<int>>)(() => Quoted.F(() => 42));

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Quote, expr.Body);

            var f = ((MethodCallExpression)expr.Body).Method;
            var x = Expression.Parameter(typeof(int));
            var withClosure = Expression.Block(new[] { x }, Expression.Assign(x, Expression.Constant(42)), Expression.Call(f, Expression.Lambda(x)));

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Quote, withClosure);
        }
    }

    static class Quoted
    {
        public static int F(Expression<Func<int>> f)
        {
            return f.Compile()();
        }
    }
}