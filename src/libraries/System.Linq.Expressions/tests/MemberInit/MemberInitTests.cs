// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class MemberInitTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckMemberInitTest(bool useInterpreter)
        {
            VerifyMemberInit(() => new X { Y = { Z = 42, YS = { 2, 3 } }, XS = { 5, 7 } }, x => x.Y.Z == 42 && x.XS.Sum() == 5 + 7 && x.Y.YS.Sum() == 2 + 3, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void Reduce(bool useInterpreter)
        {
            Expression<Func<X>> l = () => new X {Y = {Z = 42, YS = {2, 3}}, XS = {5, 7}};
            MemberInitExpression e = l.Body as MemberInitExpression;
            l = Expression.Lambda<Func<X>>(e.ReduceAndCheck());
            VerifyMemberInit(l, x => x.Y.Z == 42 && x.XS.Sum() == 5 + 7 && x.Y.YS.Sum() == 2 + 3, useInterpreter);
        }

        [Fact]
        public static void ToStringTest()
        {
            MemberInitExpression e1 = Expression.MemberInit(Expression.New(typeof(Y)), Expression.Bind(typeof(Y).GetProperty(nameof(Y.Z)), Expression.Parameter(typeof(int), "z")));
            Assert.Equal("new Y() {Z = z}", e1.ToString());

            MemberInitExpression e2 = Expression.MemberInit(Expression.New(typeof(Y)), Expression.Bind(typeof(Y).GetProperty(nameof(Y.Z)), Expression.Parameter(typeof(int), "z")), Expression.Bind(typeof(Y).GetProperty(nameof(Y.A)), Expression.Parameter(typeof(int), "a")));
            Assert.Equal("new Y() {Z = z, A = a}", e2.ToString());

            MemberInitExpression e3 = Expression.MemberInit(Expression.New(typeof(X)), Expression.MemberBind(typeof(X).GetProperty(nameof(X.Y)), Expression.Bind(typeof(Y).GetProperty(nameof(Y.Z)), Expression.Parameter(typeof(int), "z"))));
            Assert.Equal("new X() {Y = {Z = z}}", e3.ToString());

            MemberInitExpression e4 = Expression.MemberInit(Expression.New(typeof(X)), Expression.MemberBind(typeof(X).GetProperty(nameof(X.Y)), Expression.Bind(typeof(Y).GetProperty(nameof(Y.Z)), Expression.Parameter(typeof(int), "z")), Expression.Bind(typeof(Y).GetProperty(nameof(Y.A)), Expression.Parameter(typeof(int), "a"))));
            Assert.Equal("new X() {Y = {Z = z, A = a}}", e4.ToString());

            Reflection.MethodInfo add = typeof(List<int>).GetMethod(nameof(List<int>.Add));

            MemberInitExpression e5 = Expression.MemberInit(Expression.New(typeof(X)), Expression.ListBind(typeof(X).GetProperty(nameof(X.XS)), Expression.ElementInit(add, Expression.Parameter(typeof(int), "a"))));
            Assert.Equal("new X() {XS = {Void Add(Int32)(a)}}", e5.ToString());

            MemberInitExpression e6 = Expression.MemberInit(Expression.New(typeof(X)), Expression.ListBind(typeof(X).GetProperty(nameof(X.XS)), Expression.ElementInit(add, Expression.Parameter(typeof(int), "a")), Expression.ElementInit(add, Expression.Parameter(typeof(int), "b"))));
            Assert.Equal("new X() {XS = {Void Add(Int32)(a), Void Add(Int32)(b)}}", e6.ToString());
        }

        [Fact]
        public static void UpdateSameReturnsSame()
        {
            MemberAssignment bind0 = Expression.Bind(typeof(Y).GetProperty(nameof(Y.Z)), Expression.Parameter(typeof(int), "z"));
            MemberAssignment bind1 = Expression.Bind(typeof(Y).GetProperty(nameof(Y.A)), Expression.Parameter(typeof(int), "a"));
            NewExpression newExp = Expression.New(typeof(Y));
            MemberInitExpression init = Expression.MemberInit(newExp, bind0, bind1);
            Assert.Same(init, init.Update(newExp, new [] {bind0, bind1}));
        }

        [Fact]
        public static void UpdateDifferentBindingsReturnsDifferent()
        {
            MemberAssignment bind0 = Expression.Bind(typeof(Y).GetProperty(nameof(Y.Z)), Expression.Parameter(typeof(int), "z"));
            MemberAssignment bind1 = Expression.Bind(typeof(Y).GetProperty(nameof(Y.A)), Expression.Parameter(typeof(int), "a"));
            NewExpression newExp = Expression.New(typeof(Y));
            MemberInitExpression init = Expression.MemberInit(newExp, bind0, bind1);
            Assert.NotSame(init, init.Update(newExp, new[] { bind0, bind1, bind0 }));
            Assert.NotSame(init, init.Update(newExp, new[] { bind1, bind0 }));
        }

        [Fact]
        public static void UpdateDifferentNewReturnsDifferent()
        {
            MemberAssignment bind0 = Expression.Bind(typeof(Y).GetProperty(nameof(Y.Z)), Expression.Parameter(typeof(int), "z"));
            MemberAssignment bind1 = Expression.Bind(typeof(Y).GetProperty(nameof(Y.A)), Expression.Parameter(typeof(int), "a"));
            NewExpression newExp = Expression.New(typeof(Y));
            MemberInitExpression init = Expression.MemberInit(newExp, bind0, bind1);
            Assert.NotSame(init, init.Update(Expression.New(typeof(Y)), new[] { bind0, bind1 }));
        }

        #endregion

        #region Test verifiers

        private static void VerifyMemberInit<T>(Expression<Func<T>> expr, Func<T, bool> check, bool useInterpreter)
        {
            Func<T> c = expr.Compile(useInterpreter);
            Assert.True(check(c()));
        }

        #endregion

        #region Helpers

        class X
        {
            private readonly Y _y = new Y();
            private readonly List<int> _xs = new List<int>();

            public Y Y
            {
                get { return _y; }
            }

            public List<int> XS { get { return _xs; } }
        }

        class Y
        {
            private readonly List<int> _ys = new List<int>();

            public int Z { get; set; }
            public int A { get; set; }

            public List<int> YS { get { return _ys; } }
        }

        #endregion
    }
}

