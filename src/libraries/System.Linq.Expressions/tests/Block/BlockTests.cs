// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BlockTests
    {
        #region Test methods

        [Fact] // [Issue(4020, "https://github.com/dotnet/corefx/issues/4020")]
        public static void CheckBlockClosureVariableInitializationTest()
        {
            foreach (var kv in BlockClosureVariableInitialization())
            {
                VerifyBlockClosureVariableInitialization(kv.Key, kv.Value);
            }
        }

        private static IEnumerable<KeyValuePair<Expression, object>> BlockClosureVariableInitialization()
        {
            {
                var p = Expression.Parameter(typeof(int));
                var q = Expression.Parameter(typeof(Func<int>));
                var l = Expression.Lambda<Func<int>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(int));
            }

            {
                var p = Expression.Parameter(typeof(int));
                var q = Expression.Parameter(typeof(Action<int>));
                var x = Expression.Parameter(typeof(int));
                var l = Expression.Lambda<Action<int>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(int));
            }

            {
                var p = Expression.Parameter(typeof(TimeSpan));
                var q = Expression.Parameter(typeof(Func<TimeSpan>));
                var l = Expression.Lambda<Func<TimeSpan>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(TimeSpan));
            }

            {
                var p = Expression.Parameter(typeof(TimeSpan));
                var q = Expression.Parameter(typeof(Action<TimeSpan>));
                var x = Expression.Parameter(typeof(TimeSpan));
                var l = Expression.Lambda<Action<TimeSpan>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(TimeSpan));
            }

            {
                var p = Expression.Parameter(typeof(string));
                var q = Expression.Parameter(typeof(Func<string>));
                var l = Expression.Lambda<Func<string>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(string));
            }

            {
                var p = Expression.Parameter(typeof(string));
                var q = Expression.Parameter(typeof(Action<string>));
                var x = Expression.Parameter(typeof(string));
                var l = Expression.Lambda<Action<string>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(string));
            }

            {
                var p = Expression.Parameter(typeof(int?));
                var q = Expression.Parameter(typeof(Func<int?>));
                var l = Expression.Lambda<Func<int?>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(int?));
            }

            {
                var p = Expression.Parameter(typeof(int?));
                var q = Expression.Parameter(typeof(Action<int?>));
                var x = Expression.Parameter(typeof(int?));
                var l = Expression.Lambda<Action<int?>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(int?));
            }

            {
                var p = Expression.Parameter(typeof(TimeSpan?));
                var q = Expression.Parameter(typeof(Func<TimeSpan?>));
                var l = Expression.Lambda<Func<TimeSpan?>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(TimeSpan?));
            }

            {
                var p = Expression.Parameter(typeof(TimeSpan?));
                var q = Expression.Parameter(typeof(Action<TimeSpan?>));
                var x = Expression.Parameter(typeof(TimeSpan?));
                var l = Expression.Lambda<Action<TimeSpan?>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(TimeSpan?));
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBlockClosureVariableInitialization(Expression e, object o)
        {
            Expression<Func<object>> f =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(e, typeof(object)));

            Func<object> c = f.Compile();
            Assert.Equal(o, c());

#if FEATURE_INTERPRET
            Func<object> i = f.Compile(true);
            Assert.Equal(o, i());
#endif
        }

        #endregion

        private class ParameterChangingVisitor : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return Expression.Parameter(node.IsByRef ? node.Type.MakeByRefType() : node.Type, node.Name);
            }
        }

        [Fact]
        public static void VisitChangingOnlyParmeters()
        {
            var block = Expression.Block(
                new[] { Expression.Parameter(typeof(int)), Expression.Parameter(typeof(string)) },
                Expression.Empty()
                );
            Assert.NotSame(block, new ParameterChangingVisitor().Visit(block));
        }

        [Fact]
        public static void VisitChangingOnlyParmetersMultiStatementBody()
        {
            var block = Expression.Block(
                new[] { Expression.Parameter(typeof(int)), Expression.Parameter(typeof(string)) },
                Expression.Empty(),
                Expression.Empty()
                );
            Assert.NotSame(block, new ParameterChangingVisitor().Visit(block));
        }

        [Fact]
        public static void VisitChangingOnlyParmetersTyped()
        {
            var block = Expression.Block(
                typeof(object),
                new[] { Expression.Parameter(typeof(int)), Expression.Parameter(typeof(string)) },
                Expression.Constant("")
                );
            Assert.NotSame(block, new ParameterChangingVisitor().Visit(block));
        }
    }
}