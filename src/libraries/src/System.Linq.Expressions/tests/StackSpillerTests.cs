// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if FEATURE_COMPILE

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class StackSpillerTests
    {
        [Fact]
        public static void Spill_Binary()
        {
            Test((l, r) => Expression.Add(l, r), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Binary_IndexAssign()
        {
            var item = typeof(Dictionary<int, int>).GetProperty("Item");

            Test((d, i, v) =>
            {
                var index = Expression.Property(d, item, i);

                return
                    Expression.Block(
                        Expression.Assign(index, v),
                        index
                    );
            }, Expression.Constant(new Dictionary<int, int>()), Expression.Constant(0), Expression.Constant(1));
        }

        [Fact]
        public static void Spill_Binary_MemberAssign()
        {
            var baz = typeof(Bar).GetProperty(nameof(Bar.Baz));

            Test((l, r) =>
            {
                var prop = Expression.Property(l, baz);

                return
                    Expression.Block(
                        Expression.Assign(prop, r),
                        prop
                    );
            }, Expression.Constant(new Bar()), Expression.Constant(42));
        }

        [Fact]
        public static void Spill_TryFinally()
        {
            Test(a => Expression.TryFinally(a, Expression.Empty()), Expression.Constant(1));
        }

        [Fact]
        public static void Spill_TryCatch()
        {
            foreach (var div in new[] { 1, 0 })
            {
                Test((n, d, a) => Expression.TryCatch(Expression.Divide(n, d), Expression.Catch(typeof(DivideByZeroException), a)), Expression.Constant(1), Expression.Constant(div), Expression.Constant(42));
            }
        }

        [Fact]
        public static void Spill_Loop()
        {
            Test((a, b) =>
            {
                var @break = Expression.Label(typeof(int));
                return Expression.Add(a, Expression.Loop(Expression.Break(@break, b), @break));
            }, Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Member()
        {
            Test(s => Expression.Property(s, nameof(string.Length)), Expression.Constant("bar"));
        }

        [Fact]
        public static void Spill_TypeBinary()
        {
            Test(s => Expression.TypeIs(s, typeof(string)), Expression.Constant("bar", typeof(object)));
            Test(s => Expression.TypeEqual(s, typeof(string)), Expression.Constant("bar", typeof(object)));
        }

        [Fact]
        public static void Spill_Binary_Logical()
        {
            Test((l, r) => Expression.AndAlso(l, r), Expression.Constant(false), Expression.Constant(false));
            Test((l, r) => Expression.AndAlso(l, r), Expression.Constant(false), Expression.Constant(true));
            Test((l, r) => Expression.AndAlso(l, r), Expression.Constant(true), Expression.Constant(false));
            Test((l, r) => Expression.AndAlso(l, r), Expression.Constant(true), Expression.Constant(true));
        }

        [Fact]
        public static void Spill_Conditional()
        {
            Test((t, l, r) => Expression.Condition(t, l, r), Expression.Constant(false), Expression.Constant(1), Expression.Constant(2));
            Test((t, l, r) => Expression.Condition(t, l, r), Expression.Constant(true), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Index()
        {
            var item = typeof(Dictionary<int, int>).GetProperty("Item");

            Test((d, i) => Expression.MakeIndex(d, item, new[] { i }), Expression.Constant(new Dictionary<int, int> { { 1, 2 } }), Expression.Constant(1));
        }

        [Fact]
        public static void Spill_Call_Static()
        {
            var max = typeof(Math).GetMethod(nameof(Math.Max), new[] { typeof(int), typeof(int) });

            Test((x, y) => Expression.Call(max, x, y), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Call_Instance()
        {
            var substring = typeof(string).GetMethod(nameof(string.Substring), new[] { typeof(int), typeof(int) });

            Test((s, i, j) => Expression.Call(s, substring, i, j), Expression.Constant("foobar"), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Invocation()
        {
            Test((d, a, b) => Expression.Invoke(d, a, b), Expression.Constant(new Func<int, int, int>((x, y) => x + y)), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_Invocation_Inline()
        {
            var x = Expression.Parameter(typeof(int));
            var y = Expression.Parameter(typeof(int));

            Test((a, b) => Expression.Invoke(Expression.Lambda(Expression.Subtract(x, y), x, y), a, b), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_New()
        {
            var ctor = typeof(TimeSpan).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });

            Test((h, m, s) => Expression.New(ctor, h, m, s), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
        }

        [Fact]
        public static void Spill_NewArrayInit()
        {
            Test((a, b, c) =>
            {
                var p = Expression.Parameter(typeof(int[]));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.NewArrayInit(typeof(int), a, b, c)
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.ArrayIndex(p, Expression.Constant(0)),
                                Expression.ArrayIndex(p, Expression.Constant(1))
                            ),
                            Expression.ArrayIndex(p, Expression.Constant(2))
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_NewArrayBounds()
        {
            var getUpperBound = typeof(Array).GetMethod(nameof(Array.GetUpperBound));

            Test((a, b, c) =>
            {
                var p = Expression.Parameter(typeof(int[,,]));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.NewArrayBounds(typeof(int), a, b, c)
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.Call(p, getUpperBound, Expression.Constant(0)),
                                Expression.Call(p, getUpperBound, Expression.Constant(1))
                            ),
                            Expression.Call(p, getUpperBound, Expression.Constant(2))
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_ListInit()
        {
            var item = typeof(List<int>).GetProperty("Item");

            Test((a, b, c) =>
            {
                var p = Expression.Parameter(typeof(List<int>));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.ListInit(Expression.New(typeof(List<int>)), a, b, c)
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.MakeIndex(p, item, new[] { Expression.Constant(0) }),
                                Expression.MakeIndex(p, item, new[] { Expression.Constant(1) })
                            ),
                            Expression.MakeIndex(p, item, new[] { Expression.Constant(2) })
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_MemberInit_Bind()
        {
            var baz = typeof(Bar).GetProperty(nameof(Bar.Baz));
            var foo = typeof(Bar).GetProperty(nameof(Bar.Foo));
            var qux = typeof(Bar).GetProperty(nameof(Bar.Qux));

            Test((a, b, c) =>
            {
                var p = Expression.Parameter(typeof(Bar));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.MemberInit(
                                Expression.New(typeof(Bar)),
                                Expression.Bind(baz, a),
                                Expression.Bind(foo, b),
                                Expression.Bind(qux, c)
                            )
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.Property(p, baz),
                                Expression.Property(p, foo)
                            ),
                            Expression.Property(p, qux)
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_MemberInit_MemberBind()
        {
            var bar = typeof(BarHolder).GetProperty(nameof(BarHolder.Bar));
            var baz = typeof(Bar).GetProperty(nameof(Bar.Baz));
            var foo = typeof(Bar).GetProperty(nameof(Bar.Foo));
            var qux = typeof(Bar).GetProperty(nameof(Bar.Qux));

            Test((a, b, c) =>
            {
                var p = Expression.Parameter(typeof(BarHolder));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.MemberInit(
                                Expression.New(typeof(BarHolder)),
                                Expression.MemberBind(
                                    bar, 
                                    Expression.Bind(baz, a),
                                    Expression.Bind(foo, b),
                                    Expression.Bind(qux, c)
                                )
                            )
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.Property(Expression.Property(p, bar), baz),
                                Expression.Property(Expression.Property(p, bar), foo)
                            ),
                            Expression.Property(Expression.Property(p, bar), qux)
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_MemberInit_ListBind()
        {
            var xs = typeof(ListHolder).GetProperty(nameof(ListHolder.Xs));
            var add = typeof(List<int>).GetMethod(nameof(List<int>.Add));
            var item = typeof(List<int>).GetProperty("Item");

            Test((a, b, c) =>
            {
                var p = Expression.Parameter(typeof(ListHolder));

                return
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(
                            p,
                            Expression.MemberInit(
                                Expression.New(typeof(ListHolder)),
                                Expression.ListBind(
                                    xs,
                                    Expression.ElementInit(add, a),
                                    Expression.ElementInit(add, b),
                                    Expression.ElementInit(add, c)
                                )
                            )
                        ),
                        Expression.Multiply(
                            Expression.Add(
                                Expression.Property(Expression.Property(p, xs), item, Expression.Constant(0)),
                                Expression.Property(Expression.Property(p, xs), item, Expression.Constant(1))
                            ),
                            Expression.Property(Expression.Property(p, xs), item, Expression.Constant(2))
                        )
                    );
            }, Expression.Constant(2), Expression.Constant(3), Expression.Constant(5));
        }

        [Fact]
        public static void Spill_Switch()
        {
            Test((v, d, a) => Expression.Switch(v, d, Expression.SwitchCase(a, Expression.Constant(1))), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
            Test((v, d, a) => Expression.Switch(v, d, Expression.SwitchCase(a, Expression.Constant(7))), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
            Test((v, d, a) => Expression.Switch(v, d, Expression.SwitchCase(Expression.Constant(7), a)), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));
            Test((v, d, a) => Expression.Switch(v, d, Expression.SwitchCase(Expression.Constant(7), a)), Expression.Constant(1), Expression.Constant(2), Expression.Constant(1));
        }

        [Fact]
        public static void Spill_Block()
        {
            Test((a, b) => Expression.Block(a, b), Expression.Constant(1), Expression.Constant(2));
        }

        [Fact]
        public static void Spill_LabelGoto()
        {
            foreach (var t in new[] { true, false })
            {
                Test((c, a, b) =>
                {
                    var lbl = Expression.Label(typeof(int));

                    return
                        Expression.Block(
                            Expression.IfThen(c, Expression.Goto(lbl, a)),
                            Expression.Label(lbl, b)
                        );
                }, Expression.Constant(t), Expression.Constant(1), Expression.Constant(2));
            }
        }

        [Fact]
        public static void Spill_NotRefInstance_IndexAssignment()
        {
            // See https://github.com/dotnet/corefx/issues/11740 for documented limitation

            var v = Expression.Constant(new ValueVector());
            var item = typeof(ValueVector).GetProperty("Item");
            var i = Expression.Constant(0);
            var x = Expression.Constant(42);

            NotSupported(Expression.Assign(Expression.Property(v, item, i), Spill(x)));
        }

        [Fact]
        public static void Spill_NotRefInstance_Index()
        {
            // See https://github.com/dotnet/corefx/issues/11740 for documented limitation

            var v = Expression.Constant(new ValueVector());
            var item = typeof(ValueVector).GetProperty("Item");
            var i = Expression.Constant(0);
            
            NotSupported(Expression.Property(v, item, Spill(i)));
        }

        [Fact]
        public static void Spill_NotRefInstance_MemberAssignment()
        {
            // See https://github.com/dotnet/corefx/issues/11740 for documented limitation

            var v = Expression.Constant(new ValueBar());
            var foo = typeof(ValueBar).GetProperty(nameof(ValueBar.Foo));
            var x = Expression.Constant(42);

            NotSupported(Expression.Assign(Expression.Property(v, foo), Spill(x)));
        }

        [Fact]
        public static void Spill_NotRefInstance_Call()
        {
            // See https://github.com/dotnet/corefx/issues/11740 for documented limitation

            var v = Expression.Constant(new ValueBar());
            var qux = typeof(ValueBar).GetMethod(nameof(ValueBar.Qux));
            var x = Expression.Constant(42);

            NotSupported(Expression.Call(v, qux, Spill(x)));
        }

        [Fact]
        public static void Spill_RequireNoRefArgs_Call()
        {
            // See https://github.com/dotnet/corefx/issues/11740 for documented limitation

            var assign = typeof(ByRefs).GetMethod(nameof(ByRefs.Assign));
            var x = Expression.Parameter(typeof(int));
            var v = Expression.Constant(42);
            var b = Expression.Block(new[] { x }, Expression.Call(assign, x, Spill(v)), x);

            NotSupported(b);
        }

        [Fact]
        public static void Spill_RequireNoRefArgs_New()
        {
            // See https://github.com/dotnet/corefx/issues/11740 for documented limitation

            var ctor = typeof(ByRefs).GetConstructors()[0];
            var x = Expression.Parameter(typeof(int));
            var v = Expression.Constant(42);
            var b = Expression.Block(new[] { x }, Expression.New(ctor, x, Spill(v)), x);

            NotSupported(b);
        }

        private static void NotSupported(Expression expression)
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile();
            });
        }

        private static void Test(Func<Expression, Expression> factory, Expression arg1)
        {
            Test(args => factory(args[0]), new[] { arg1 });
        }

        private static void Test(Func<Expression, Expression, Expression> factory, Expression arg1, Expression arg2)
        {
            Test(args => factory(args[0], args[1]), new[] { arg1, arg2 });
        }

        private static void Test(Func<Expression, Expression, Expression, Expression> factory, Expression arg1, Expression arg2, Expression arg3)
        {
            Test(args => factory(args[0], args[1], args[2]), new[] { arg1, arg2, arg3 });
        }

        private static void Test(Func<Expression[], Expression> factory, Expression[] args)
        {
            var expected = Eval(factory(args));

            for (var i = 0; i < args.Length; i++)
            {
                var newArgs = args.Select((arg, j) => j == i ? Spill(arg) : arg).ToArray();
                Assert.Equal(expected, Eval(factory(newArgs)));
            }

            for (var i = 0; i < args.Length; i++)
            {
                var newArgs = args.Select((arg, j) => j == i ? new Extension(arg) : arg).ToArray();
                Assert.Equal(expected, Eval(factory(newArgs)));
            }
        }

        private static object Eval(Expression expression)
        {
            return Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()();
        }

        private static Expression Spill(Expression expression)
        {
            return Expression.TryFinally(expression, Expression.Empty());
        }

        class BarHolder
        {
            public Bar Bar { get; } = new Bar();
        }

        class ListHolder
        {
            public List<int> Xs { get; } = new List<int>();
        }

        class Bar
        {
            public int Baz { get; set; }
            public int Foo { get; set; }
            public int Qux { get; set; }
        }

        class Extension : Expression
        {
            private readonly Expression _reduced;

            public Extension(Expression reduced)
            {
                _reduced = reduced;
            }

            public override bool CanReduce => true;
            public override ExpressionType NodeType => ExpressionType.Extension;
            public override Type Type => _reduced.Type;
            public override Expression Reduce() => _reduced;
        }

        struct ValueVector
        {
            private int v0;

            public int this[int x]
            {
                get { return v0; }
                set { v0 = value; }
            }
        }

        struct ValueBar
        {
            public int Foo { get; set; }

            public int Qux(int x) => x + 1;
        }

        class ByRefs
        {
            public ByRefs(ref int x, int v)
            {
                x = v;
            }

            public static void Assign(ref int x, int v)
            {
                x = v;
            }
        }
    }
}

#endif
