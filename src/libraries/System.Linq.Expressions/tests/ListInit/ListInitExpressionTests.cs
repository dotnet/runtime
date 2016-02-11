// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ListInitExpressionTests
    {
        private class NonEnumerableAddable
        {
            public readonly List<int> Store = new List<int>();

            public void Add(int value)
            {
                Store.Add(value);
            }
        }

        [Fact]
        public void NullNewMethod()
        {
            var validExpression = Expression.Constant(1);
            Assert.Throws<ArgumentNullException>("newExpression", () => Expression.ListInit(null, validExpression));
            Assert.Throws<ArgumentNullException>("newExpression", () => Expression.ListInit(null, Enumerable.Repeat(validExpression, 1)));

            var validMethod = typeof(List<int>).GetMethod("Add");
            Assert.Throws<ArgumentNullException>("newExpression", () => Expression.ListInit(null, validMethod, validExpression));
            Assert.Throws<ArgumentNullException>("newExpression", () => Expression.ListInit(null, validMethod, Enumerable.Repeat(validExpression, 1)));

            Assert.Throws<ArgumentNullException>("newExpression", () => Expression.ListInit(null, default(MethodInfo), validExpression));
            Assert.Throws<ArgumentNullException>("newExpression", () => Expression.ListInit(null, null, Enumerable.Repeat(validExpression, 1)));

            var validElementInit = Expression.ElementInit(validMethod, validExpression);
            Assert.Throws<ArgumentNullException>("newExpression", () => Expression.ListInit(null, validElementInit));
            Assert.Throws<ArgumentNullException>("newExpression", () => Expression.ListInit(null, Enumerable.Repeat(validElementInit, 1)));
        }

        [Fact]
        public void NullInitializers()
        {
            var validNew = Expression.New(typeof(List<int>));
            Assert.Throws<ArgumentNullException>("initializers", () => Expression.ListInit(validNew, default(Expression[])));
            Assert.Throws<ArgumentNullException>("initializers", () => Expression.ListInit(validNew, default(IEnumerable<Expression>)));
            Assert.Throws<ArgumentNullException>("initializers", () => Expression.ListInit(validNew, default(ElementInit[])));
            Assert.Throws<ArgumentNullException>("initializers", () => Expression.ListInit(validNew, default(IEnumerable<ElementInit>)));

            var validMethod = typeof(List<int>).GetMethod("Add");
            Assert.Throws<ArgumentNullException>("initializers", () => Expression.ListInit(validNew, validMethod, default(Expression[])));
            Assert.Throws<ArgumentNullException>("initializers", () => Expression.ListInit(validNew, validMethod, default(IEnumerable<Expression>)));

            Assert.Throws<ArgumentNullException>("initializers", () => Expression.ListInit(validNew, null, default(Expression[])));
            Assert.Throws<ArgumentNullException>("initializers", () => Expression.ListInit(validNew, null, default(IEnumerable<Expression>)));
        }

        [Fact]
        public void ZeroInitializers()
        {
            var validNew = Expression.New(typeof(List<int>));
            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(validNew, new Expression[0]));
            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(validNew, Enumerable.Empty<Expression>()));
            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(validNew, new ElementInit[0]));
            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(validNew, Enumerable.Empty<ElementInit>()));

            var validMethod = typeof(List<int>).GetMethod("Add");
            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(validNew, validMethod, new Expression[0]));
            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(validNew, validMethod, Enumerable.Empty<Expression>()));

            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(validNew, null, new Expression[0]));
            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(validNew, null, Enumerable.Empty<Expression>()));
        }

        [Fact]
        public void TypeWithoutAdd()
        {
            var newExp = Expression.New(typeof(string).GetConstructor(new[] { typeof(char[]) }), Expression.Constant("aaaa".ToCharArray()));
            Assert.Throws<InvalidOperationException>(() => Expression.ListInit(newExp, Expression.Constant('a')));
            Assert.Throws<InvalidOperationException>(() => Expression.ListInit(newExp, Enumerable.Repeat(Expression.Constant('a'), 1)));
            Assert.Throws<InvalidOperationException>(() => Expression.ListInit(newExp, default(MethodInfo), Expression.Constant('a')));
            Assert.Throws<InvalidOperationException>(() => Expression.ListInit(newExp, default(MethodInfo), Enumerable.Repeat(Expression.Constant('a'), 1)));
        }

        [Fact]
        public void InitializeNonEnumerable()
        {
            // () => new NonEnumerableAddable { 1, 2, 4, 16, 42 } isn't allowed because list initialization
            // is allowed only with enumerable types.
            Assert.Throws<ArgumentException>(null, () => Expression.ListInit(Expression.New(typeof(NonEnumerableAddable)), Expression.Constant(1)));
        }

        [Fact]
        public void InitializersWrappedExactly()
        {
            var newExp = Expression.New(typeof(List<int>));
            var expressions = new[] { Expression.Constant(1), Expression.Constant(2), Expression.Constant(int.MaxValue) };
            var listInit = Expression.ListInit(newExp, expressions);
            Assert.Equal(expressions, listInit.Initializers.Select(i => i.GetArgument(0)));
        }

        [Fact]
        public void CanReduce()
        {
            var listInit = Expression.ListInit(
                Expression.New(typeof(List<int>)),
                Expression.Constant(0)
                );
            Assert.True(listInit.CanReduce);
            Assert.NotSame(listInit, listInit.ReduceAndCheck());
        }

        [Fact]
        public void InitializeVoidAddCompiler()
        {
            Expression<Func<List<int>>> listInit = () => new List<int> { 1, 2, 4, 16, 42 };
            Func<List<int>> func = listInit.Compile(false);
            Assert.Equal(new[] { 1, 2, 4, 16, 42 }, func());
        }

        [Fact]
        public void InitializeVoidAddInterpreter()
        {
            Expression<Func<List<int>>> listInit = () => new List<int> { 1, 2, 4, 16, 42 };
            Func<List<int>> func = listInit.Compile(true);
            Assert.Equal(new[] { 1, 2, 4, 16, 42 }, func());
        }

        [Fact]
        public void InitializeNonVoidAddCompiler()
        {
            Expression<Func<HashSet<int>>> hashInit = () => new HashSet<int> { 1, 2, 4, 16, 42 };
            Func<HashSet<int>> func = hashInit.Compile(false);
            Assert.Equal(new[] { 1, 2, 4, 16, 42 }, func().OrderBy(i => i));
        }

        [Fact]
        public void InitializeNonVoidAddInterpreter()
        {
            Expression<Func<HashSet<int>>> hashInit = () => new HashSet<int> { 1, 2, 4, 16, 42 };
            Func<HashSet<int>> func = hashInit.Compile(true);
            Assert.Equal(new[] { 1, 2, 4, 16, 42 }, func().OrderBy(i => i));
        }

        [Fact]
        public void InitializeTwoParameterAddCompiler()
        {
            Expression<Func<Dictionary<string, int>>> dictInit = () => new Dictionary<string, int>
            {
                { "a", 1 }, {"b", 2 }, {"c", 3 }
            };
            Func<Dictionary<string, int>> func = dictInit.Compile(false);
            var expected = new Dictionary<string, int>
            {
                { "a", 1 }, {"b", 2 }, {"c", 3 }
            };
            Assert.Equal(expected.OrderBy(kvp => kvp.Key), func().OrderBy(kvp => kvp.Key));
        }

        [Fact]
        public void InitializeTwoParameterAddInterpreter()
        {
            Expression<Func<Dictionary<string, int>>> dictInit = () => new Dictionary<string, int>
            {
                { "a", 1 }, {"b", 2 }, {"c", 3 }
            };
            Func<Dictionary<string, int>> func = dictInit.Compile(true);
            var expected = new Dictionary<string, int>
            {
                { "a", 1 }, {"b", 2 }, {"c", 3 }
            };
            Assert.Equal(expected.OrderBy(kvp => kvp.Key), func().OrderBy(kvp => kvp.Key));
        }
    }
}
