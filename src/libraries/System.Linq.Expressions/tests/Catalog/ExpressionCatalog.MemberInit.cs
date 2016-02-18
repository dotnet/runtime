// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> MemberInit()
        {
            yield return ((Expression<Func<MI1>>)(() => new MI1 { })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1 { X = 1 })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1 { X = 1, Y = "bar" })).Body;

            yield return ((Expression<Func<MI1>>)(() => new MI1(true) { })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1(true) { X = 1 })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1(true) { X = 1, Y = "bar" })).Body;

            yield return ((Expression<Func<MI1>>)(() => new MI1(false) { })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1(false) { X = 1 })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1(false) { X = 1, Y = "bar" })).Body;

            var g1 = new Func<string>(() =>
            {
                throw new InvalidOperationException("Oops I did it again.");
            });

            var g2 = new Func<int>(() =>
            {
                throw new InvalidOperationException("Oops you did it again.");
            });

            yield return ((Expression<Func<MI1>>)(() => new MI1 { X = 0, Y = g1() })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1 { X = 1, Y = g1() })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1 { Y = g1(), X = 0 })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1 { Y = g1(), X = 1 })).Body;

            yield return ((Expression<Func<MI1>>)(() => new MI1 { Y = null, X = g2() })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1 { Y = "bar", X = g2() })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1 { X = g2(), Y = null })).Body;
            yield return ((Expression<Func<MI1>>)(() => new MI1 { X = g2(), Y = "bar" })).Body;

            yield return ((Expression<Func<MI2>>)(() => new MI2 { MI1 = { X = 42, Y = "qux" }, Bars = { 2, 3, 5 } })).Body;
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> MemberInit_WithLog()
        {
            yield return WithLogExpr(ExpressionType.MemberInit, (add, summarize) =>
            {
                var newTemplate = ((Expression<Func<Action<string>, MIWithLog1>>)(addToLog => new MIWithLog1(addToLog) { Bar = 42, Foo = "qux", Quxs = { 2, 3, 5 }, Baz = { Bar = 43, Foo = "baz" } }));
                var valueParam = Expression.Parameter(typeof(MIWithLog1));
                var toStringTemplate = ((Expression<Func<MIWithLog1, string>>)(mi => mi.ToString()));
                var concatTemplate = (Expression<Func<string, string, string>>)((s1, s2) => s1 + s2);

                return
                    Expression.Block(
                        new[] { valueParam },
                        Expression.Assign(
                            valueParam,
                            Expression.Invoke(
                                ReturnAllConstants(add, newTemplate),
                                add
                            )
                        ),
                        Expression.Invoke(
                            concatTemplate,
                            Expression.Invoke(toStringTemplate, valueParam),
                            Expression.Invoke(summarize)
                        )
                    );
            });
        }

        public class MI1 : IEquatable<MI1>
        {
            private int _x;
            private string _y;

            public MI1()
            {
            }

            public MI1(bool b)
            {
                if (!b)
                    throw new ArgumentException("Can't be false.", nameof(b));
            }

            public int X
            {
                get { return _x; }
                set
                {
                    if (_x == 0)
                        throw new ArgumentException("Can't be zero.", nameof(value));

                    _x = value;
                }
            }
            public string Y
            {
                get { return _y; }
                set
                {
                    if (_y == null)
                        throw new ArgumentNullException(nameof(value), "Can't be null.");

                    _y = value;
                }
            }

            public bool Equals(MI1 other)
            {
                if (other == null)
                {
                    return false;
                }

                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as MI1);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        public class MI2 : IEquatable<MI2>
        {
            private readonly MI1 _mi1 = new MI1();
            private readonly List<int> _bars = new List<int>();

            public MI1 MI1
            {
                get
                {
                    return _mi1;
                }
            }

            public List<int> Bars
            {
                get
                {
                    return _bars;
                }
            }

            public bool Equals(MI2 other)
            {
                if (other == null)
                {
                    return false;
                }

                return other.MI1.Equals(MI1) && other.Bars.SequenceEqual(Bars);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as MI2);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        class MIWithLog1
        {
            private readonly Action<string> _addToLog;
            private readonly LIWithLog _quxs;
            private readonly MIWithLog2 _child;
            private int _bar;
            private string _foo;

            public MIWithLog1(Action<string> addToLog)
            {
                _addToLog = addToLog;
                _quxs = new LIWithLog(addToLog);
                _child = new MIWithLog2(addToLog);

                _addToLog(".ctor");
            }

            public int Bar
            {
                get
                {
                    _addToLog("get_Bar");
                    return _bar;
                }

                set
                {
                    _addToLog("set_Bar(" + value + ")");
                    _bar = value;
                }
            }

            public string Foo
            {
                get
                {
                    _addToLog("get_Foo");
                    return _foo;
                }

                set
                {
                    _addToLog("set_Foo(" + value + ")");
                    _foo = value;
                }
            }

            public LIWithLog Quxs
            {
                get
                {
                    _addToLog("get_Quxs");
                    return _quxs;
                }
            }

            public MIWithLog2 Baz
            {
                get
                {
                    _addToLog("get_Baz");
                    return _child;
                }
            }

            public override string ToString()
            {
                return "{ Bar = " + Bar + ", Foo = " + Foo + ", Baz = " + Baz.ToString() + ", Quxs = " + Quxs.ToString() + " }";
            }
        }

        class MIWithLog2
        {
            private readonly Action<string> _addToLog;
            private int _bar;
            private string _foo;

            public MIWithLog2(Action<string> addToLog)
            {
                _addToLog = addToLog;

                _addToLog(".ctor");
            }

            public int Bar
            {
                get
                {
                    _addToLog("get_Bar");
                    return _bar;
                }

                set
                {
                    _addToLog("set_Bar(" + value + ")");
                    _bar = value;
                }
            }

            public string Foo
            {
                get
                {
                    _addToLog("get_Foo");
                    return _foo;
                }

                set
                {
                    _addToLog("set_Foo(" + value + ")");
                    _foo = value;
                }
            }

            public override string ToString()
            {
                return "{ Bar = " + Bar + ", Foo = " + Foo + " }";
            }
        }
    }
}
