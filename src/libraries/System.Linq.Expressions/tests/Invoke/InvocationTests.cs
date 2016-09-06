// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class InvocationTests
    {
        public delegate void X(X a);

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void SelfApplication(bool useInterpreter)
        {
            // Expression<X> f = x => {};
            Expression<X> f = Expression.Lambda<X>(Expression.Empty(), Expression.Parameter(typeof(X)));
            var a = Expression.Lambda(Expression.Invoke(f, f));

            a.Compile(useInterpreter).DynamicInvoke();

            var it = Expression.Parameter(f.Type);
            var b = Expression.Lambda(Expression.Invoke(Expression.Lambda(Expression.Invoke(it, it), it), f));

            b.Compile(useInterpreter).DynamicInvoke();
        }

        [Fact]
        public static void NoWriteBackToInstance()
        {
            new NoThread(false).DoTest();
            new NoThread(true).DoTest(); // This case fails
        }

        public class NoThread
        {
            private readonly bool _preferInterpretation;

            public NoThread(bool preferInterpretation)
            {
                _preferInterpretation = preferInterpretation;
            }

            public Func<NoThread, int> DoItA = (nt) =>
            {
                nt.DoItA = (nt0) => 1;
                return 0;
            };

            public Action Compile()
            {
                var ind0 = Expression.Constant(this);
                var fld = Expression.PropertyOrField(ind0, "DoItA");
                var block = Expression.Block(typeof(void), Expression.Invoke(fld, ind0));
                return Expression.Lambda<Action>(block).Compile(_preferInterpretation);
            }

            public void DoTest()
            {
                var act = Compile();
                act();
                Assert.Equal(1, DoItA(this));
            }
        }

        private class FuncHolder
        {
            public Func<int> Function;

            public FuncHolder()
            {
                Function = () =>
                {
                    Function = () => 1;
                    return 0;
                };
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void InvocationDoesNotChangeFunctionInvoked(bool useInterpreter)
        {
            FuncHolder holder = new FuncHolder();
            var fld = Expression.Field(Expression.Constant(holder), "Function");
            var inv = Expression.Invoke(fld);
            Func<int> act = (Func<int>)Expression.Lambda(inv).Compile(useInterpreter);
            act();
            Assert.Equal(1, holder.Function());
        }

        [Fact]
        public static void ToStringTest()
        {
            var e1 = Expression.Invoke(Expression.Parameter(typeof(Action), "f"));
            Assert.Equal("Invoke(f)", e1.ToString());

            var e2 = Expression.Invoke(Expression.Parameter(typeof(Action<int>), "f"), Expression.Parameter(typeof(int), "x"));
            Assert.Equal("Invoke(f, x)", e2.ToString());
        }
    }
}
