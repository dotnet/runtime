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
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Switch()
        {
            for (var i = -1; i <= 4; i++)
            {
                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Switch,
                    Expression.Switch(
                        Expression.Constant(i),
                        Expression.Constant("other"),
                        Expression.SwitchCase(Expression.Constant("one"), Expression.Constant(1)),
                        Expression.SwitchCase(Expression.Constant("two"), Expression.Constant(2)),
                        Expression.SwitchCase(Expression.Constant("thr"), Expression.Constant(3))
                    )
                );

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Switch,
                    Expression.Switch(
                        Expression.Constant(i),
                        Expression.Constant("?"),
                        Expression.SwitchCase(Expression.Constant("odd"), Expression.Constant(1), Expression.Constant(3)),
                        Expression.SwitchCase(Expression.Constant("even"), Expression.Constant(2), Expression.Constant(4))
                    )
                );

                yield return WithLog(ExpressionType.Switch, (add, summary) =>
                    Expression.Block(
                        Expression.Switch(
                            Expression.Block(add(Expression.Constant("T")), Expression.Constant(i)),
                            Expression.Block(add(Expression.Constant("D")), Expression.Constant("other")),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C1")), Expression.Constant("one")), Expression.Constant(1)),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C2")), Expression.Constant("two")), Expression.Constant(2)),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C3")), Expression.Constant("thr")), Expression.Constant(3))
                        ),
                        summary
                    )
                );

                yield return WithLog(ExpressionType.Switch, (add, summary) =>
                    Expression.Block(
                        Expression.Switch(
                            Expression.Block(add(Expression.Constant("T")), Expression.Constant(i)),
                            Expression.Block(add(Expression.Constant("D")), Expression.Constant("?")),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C1")), Expression.Constant("odd")), Expression.Constant(1), Expression.Constant(3)),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C2")), Expression.Constant("even")), Expression.Constant(2), Expression.Constant(4))
                        ),
                        summary
                    )
                );

                yield return WithLog(ExpressionType.Switch, (add, summary) =>
                    Expression.Block(
                        Expression.Switch(
                            Expression.Block(add(Expression.Constant("T")), Expression.Constant(i)),
                            Expression.Block(add(Expression.Constant("D")), Expression.Constant("other")),
                            Expression.SwitchCase(Expression.Constant("one"), Expression.Block(add(Expression.Constant("V1")), Expression.Constant(1))),
                            Expression.SwitchCase(Expression.Constant("two"), Expression.Block(add(Expression.Constant("V2")), Expression.Constant(2))),
                            Expression.SwitchCase(Expression.Constant("thr"), Expression.Block(add(Expression.Constant("V3")), Expression.Constant(3)))
                        ),
                        summary
                    )
                );

                yield return WithLog(ExpressionType.Switch, (add, summary) =>
                    Expression.Block(
                        Expression.Switch(
                            Expression.Block(add(Expression.Constant("T")), Expression.Constant(i)),
                            Expression.Block(add(Expression.Constant("D")), Expression.Constant("?")),
                            Expression.SwitchCase(Expression.Constant("odd"), Expression.Block(add(Expression.Constant("V1")), Expression.Constant(1)), Expression.Block(add(Expression.Constant("V3")), Expression.Constant(3))),
                            Expression.SwitchCase(Expression.Constant("even"), Expression.Block(add(Expression.Constant("V2")), Expression.Constant(2)), Expression.Block(add(Expression.Constant("V4")), Expression.Constant(4)))
                        ),
                        summary
                    )
                );
            }

            foreach (var s in new[] { default(string), "one", "two", "thr", "zer", "fiv", "for" })
            {
                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Switch,
                    Expression.Switch(
                        Expression.Constant(s, typeof(string)),
                        Expression.Constant(null, typeof(int?)),
                        Expression.SwitchCase(Expression.Constant(1, typeof(int?)), Expression.Constant("one")),
                        Expression.SwitchCase(Expression.Constant(2, typeof(int?)), Expression.Constant("two")),
                        Expression.SwitchCase(Expression.Constant(3, typeof(int?)), Expression.Constant("thr"))
                    )
                );

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Switch,
                    Expression.Switch(
                        Expression.Constant(s, typeof(string)),
                        Expression.Constant(int.MaxValue, typeof(int?)),
                        Expression.SwitchCase(Expression.Constant(1, typeof(int?)), Expression.Constant("one")),
                        Expression.SwitchCase(Expression.Constant(2, typeof(int?)), Expression.Constant("two")),
                        Expression.SwitchCase(Expression.Constant(3, typeof(int?)), Expression.Constant("thr"))
                    )
                );

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Switch,
                    Expression.Switch(
                        Expression.Constant(s, typeof(string)),
                        Expression.Constant(-1, typeof(int?)),
                        Expression.SwitchCase(Expression.Constant(null, typeof(int?)), Expression.Constant(null, typeof(string))),
                        Expression.SwitchCase(Expression.Constant(1, typeof(int?)), Expression.Constant("one")),
                        Expression.SwitchCase(Expression.Constant(2, typeof(int?)), Expression.Constant("two")),
                        Expression.SwitchCase(Expression.Constant(3, typeof(int?)), Expression.Constant("thr"))
                    )
                );

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Switch,
                    Expression.Switch(
                        Expression.Constant(s, typeof(string)),
                        Expression.Constant(-1, typeof(int?)),
                        Expression.SwitchCase(Expression.Constant(int.MaxValue, typeof(int?)), Expression.Constant(null, typeof(string))),
                        Expression.SwitchCase(Expression.Constant(1, typeof(int?)), Expression.Constant("one")),
                        Expression.SwitchCase(Expression.Constant(2, typeof(int?)), Expression.Constant("two")),
                        Expression.SwitchCase(Expression.Constant(3, typeof(int?)), Expression.Constant("thr"))
                    )
                );

                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Switch,
                    Expression.Switch(
                        Expression.Constant(s, typeof(string)),
                        Expression.Constant(null, typeof(int?)),
                        Expression.SwitchCase(Expression.Constant(1, typeof(int?)), Expression.Constant("one"), Expression.Constant("thr")),
                        Expression.SwitchCase(Expression.Constant(0, typeof(int?)), Expression.Constant("two"), Expression.Constant("for"))
                    )
                );

                yield return WithLog(ExpressionType.Switch, (add, summary) =>
                    Expression.Block(
                        Expression.Switch(
                            Expression.Block(add(Expression.Constant("T")), Expression.Constant(s, typeof(string))),
                            Expression.Block(add(Expression.Constant("D")), Expression.Constant(null, typeof(int?))),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C1")), Expression.Constant(1, typeof(int?))), Expression.Constant("one")),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C2")), Expression.Constant(2, typeof(int?))), Expression.Constant("two")),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C3")), Expression.Constant(3, typeof(int?))), Expression.Constant("thr"))
                        ),
                        summary
                    )
                );

                yield return WithLog(ExpressionType.Switch, (add, summary) =>
                    Expression.Block(
                        Expression.Switch(
                            Expression.Block(add(Expression.Constant("T")), Expression.Constant(s, typeof(string))),
                            Expression.Block(add(Expression.Constant("D")), Expression.Constant(null, typeof(int?))),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C1")), Expression.Constant(1, typeof(int?))), Expression.Constant("one"), Expression.Constant("thr")),
                            Expression.SwitchCase(Expression.Block(add(Expression.Constant("C2")), Expression.Constant(0, typeof(int?))), Expression.Constant("two"), Expression.Constant("for"))
                        ),
                        summary
                    )
                );

                yield return WithLog(ExpressionType.Switch, (add, summary) =>
                    Expression.Block(
                        Expression.Switch(
                            Expression.Block(add(Expression.Constant("T")), Expression.Constant(s, typeof(string))),
                            Expression.Block(add(Expression.Constant("D")), Expression.Constant(null, typeof(int?))),
                            Expression.SwitchCase(Expression.Constant(1, typeof(int?)), Expression.Block(add(Expression.Constant("V1")), Expression.Constant("one"))),
                            Expression.SwitchCase(Expression.Constant(2, typeof(int?)), Expression.Block(add(Expression.Constant("V2")), Expression.Constant("two"))),
                            Expression.SwitchCase(Expression.Constant(3, typeof(int?)), Expression.Block(add(Expression.Constant("V3")), Expression.Constant("thr")))
                        ),
                        summary
                    )
                );

                yield return WithLog(ExpressionType.Switch, (add, summary) =>
                    Expression.Block(
                        Expression.Switch(
                            Expression.Block(add(Expression.Constant("T")), Expression.Constant(s, typeof(string))),
                            Expression.Block(add(Expression.Constant("D")), Expression.Constant(null, typeof(int?))),
                            Expression.SwitchCase(Expression.Constant(1, typeof(int?)), Expression.Block(add(Expression.Constant("V1")), Expression.Constant("one")), Expression.Block(add(Expression.Constant("V3")), Expression.Constant("thr"))),
                            Expression.SwitchCase(Expression.Constant(0, typeof(int?)), Expression.Block(add(Expression.Constant("V2")), Expression.Constant("two")), Expression.Block(add(Expression.Constant("V4")), Expression.Constant("for")))
                        ),
                        summary
                    )
                );
            }

            var xs = Enumerable.Range(0, 10).ToArray();
            var ss = xs.Select(x => x.ToString()).ToArray();

            foreach (var t in new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) })
            {
                var vals = xs.Select(x => System.Convert.ChangeType(x, t)).ToArray();
                var cass = vals.Skip(1).Take(vals.Length - 2).ToArray();

                foreach (var v in vals)
                {
                    yield return WithLog(ExpressionType.Switch, (add, summary) =>
                        Expression.Block(
                            Expression.Switch(
                                Expression.Block(add(Expression.Constant("T")), Expression.Constant(v, t)),
                                Expression.Block(add(Expression.Constant("D")), Expression.Constant("", typeof(string))),
                                cass.Select((c, i) => Expression.SwitchCase(Expression.Block(add(Expression.Constant("C" + i)), Expression.Constant(ss[i], typeof(string))), Expression.Constant(c, t))).ToArray()
                            ),
                            summary
                        )
                    );

                    yield return WithLog(ExpressionType.Switch, (add, summary) =>
                        Expression.Block(
                            Expression.Switch(
                                Expression.Block(add(Expression.Constant("T")), Expression.Constant(v, t)),
                                Expression.Block(add(Expression.Constant("D")), Expression.Constant("", typeof(string))),
                                cass.Select((c, i) => Expression.SwitchCase(Expression.Constant(ss[i], typeof(string)), Expression.Block(add(Expression.Constant("V" + i)), Expression.Constant(c, t)))).ToArray()
                            ),
                            summary
                        )
                    );
                }
            }

            foreach (var t in new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) })
            {
                var n = typeof(Nullable<>).MakeGenericType(t);

                var vals = xs.Select(x => System.Convert.ChangeType(x, t)).Concat(new object[] { null }).ToArray();
                var cass = vals.Skip(1).Take(vals.Length - 3).ToArray();

                foreach (var v in vals)
                {
                    yield return WithLog(ExpressionType.Switch, (add, summary) =>
                        Expression.Block(
                            Expression.Switch(
                                Expression.Block(add(Expression.Constant("T")), Expression.Constant(v, n)),
                                Expression.Block(add(Expression.Constant("D")), Expression.Constant("", typeof(string))),
                                cass.Select((c, i) => Expression.SwitchCase(Expression.Block(add(Expression.Constant("C" + i)), Expression.Constant(ss[i], typeof(string))), Expression.Constant(c, n))).ToArray()
                            ),
                            summary
                        )
                    );

                    yield return WithLog(ExpressionType.Switch, (add, summary) =>
                        Expression.Block(
                            Expression.Switch(
                                Expression.Block(add(Expression.Constant("T")), Expression.Constant(v, n)),
                                Expression.Block(add(Expression.Constant("D")), Expression.Constant("", typeof(string))),
                                cass.Select((c, i) => Expression.SwitchCase(Expression.Block(add(Expression.Constant("C" + i)), Expression.Constant(ss[i], typeof(string))), Expression.Constant(c, n))).Concat(new[] {
                                    Expression.SwitchCase(Expression.Block(add(Expression.Constant("CN")), Expression.Constant("null", typeof(string))), Expression.Constant(null, n))
                                }).ToArray()
                            ),
                            summary
                        )
                    );

                    yield return WithLog(ExpressionType.Switch, (add, summary) =>
                        Expression.Block(
                            Expression.Switch(
                                Expression.Block(add(Expression.Constant("T")), Expression.Constant(v, n)),
                                Expression.Block(add(Expression.Constant("D")), Expression.Constant("", typeof(string))),
                                cass.Select((c, i) => Expression.SwitchCase(Expression.Constant(ss[i], typeof(string)), Expression.Block(add(Expression.Constant("V" + i)), Expression.Constant(c, n)))).ToArray()
                            ),
                            summary
                        )
                    );

                    yield return WithLog(ExpressionType.Switch, (add, summary) =>
                        Expression.Block(
                            Expression.Switch(
                                Expression.Block(add(Expression.Constant("T")), Expression.Constant(v, n)),
                                Expression.Block(add(Expression.Constant("D")), Expression.Constant("", typeof(string))),
                                cass.Select((c, i) => Expression.SwitchCase(Expression.Constant(ss[i], typeof(string)), Expression.Block(add(Expression.Constant("V" + i)), Expression.Constant(c, n)))).Concat(new[] {
                                    Expression.SwitchCase(Expression.Constant("null", typeof(string)), Expression.Block(add(Expression.Constant("VN")), Expression.Constant(null, n)))
                                }).ToArray()
                            ),
                            summary
                        )
                    );
                }
            }

            // TODO: comparison method
            // TODO: result type
            // TODO: lifted case
        }
    }
}