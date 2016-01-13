// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> ListInit()
        {
            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new List<int> { 2 }.Sum()))).Body);
            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new List<int> { 2, 3 }.Sum()))).Body);
            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new List<int> { 2, 3, 5 }.Sum()))).Body);

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new List<int>(1) { 2 }.Sum()))).Body);
            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new List<int>(1) { 2, 3 }.Sum()))).Body);
            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new List<int>(1) { 2, 3, 5 }.Sum()))).Body);

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new List<int>(-1) { 2 }.Sum()))).Body);

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new Dictionary<string, int> { { "foo", 42 }, { "bar", 43 } }.Sum(kv => kv.Value)))).Body);
            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ListInit, (((Expression<Func<int>>)(() => new Dictionary<string, int> { { null, 42 } }.Sum(kv => kv.Value)))).Body);
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> ListInit_WithLog()
        {
            yield return WithLogExpr(ExpressionType.ListInit, (add, summarize) =>
            {
                var newTemplate = ((Expression<Func<Action<string>, LIWithLog>>)(addToLog => new LIWithLog(addToLog) { 2, 3, 5 }));
                var valueParam = Expression.Parameter(typeof(LIWithLog));
                var toStringTemplate = ((Expression<Func<LIWithLog, string>>)(mi => mi.ToString()));
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

        class LIWithLog : IEnumerable<int>
        {
            private readonly Action<string> _addToLog;
            private readonly List<int> _values;

            public LIWithLog(Action<string> addToLog)
            {
                _addToLog = addToLog;
                _values = new List<int>();

                _addToLog(".ctor");
            }

            public void Add(int x)
            {
                _addToLog("Add(" + x + ")");
                _values.Add(x);
            }

            public IEnumerator<int> GetEnumerator()
            {
                _addToLog("GetEnumerator");
                return _values.GetEnumerator();
            }

            public override string ToString()
            {
                return "{ " + string.Join(", ", _values) + " }";
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}