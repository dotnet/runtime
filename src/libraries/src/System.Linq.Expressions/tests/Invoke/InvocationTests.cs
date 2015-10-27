// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class InvocationTests
    {
        public delegate void X(X a);

        [Fact] // [Issue(3224, "https://github.com/dotnet/corefx/issues/3224")]
        public static void SelfApplication()
        {
            // Expression<X> f = x => {};
            Expression<X> f = Expression.Lambda<X>(Expression.Empty(), Expression.Parameter(typeof(X)));
            var a = Expression.Lambda(Expression.Invoke(f, f));

            a.Compile().DynamicInvoke();

            var it = Expression.Parameter(f.Type);
            var b = Expression.Lambda(Expression.Invoke(Expression.Lambda(Expression.Invoke(it, it), it), f));

            b.Compile().DynamicInvoke();
        }

        [Fact(Skip = "4150")]
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

        [Fact]
        public static void InvocationDoesNotChangeFunctionInvokedCompiled()
        {
            FuncHolder holder = new FuncHolder();
            var fld = Expression.Field(Expression.Constant(holder), "Function");
            var inv = Expression.Invoke(fld);
            Func<int> act = (Func<int>)Expression.Lambda(inv).Compile(false);
            act();
            Assert.Equal(1, holder.Function());
        }

        [Fact]
        public static void InvocationDoesNotChangeFunctionInvokedInterpreted()
        {
            FuncHolder holder = new FuncHolder();
            var fld = Expression.Field(Expression.Constant(holder), "Function");
            var inv = Expression.Invoke(fld);
            Func<int> act = (Func<int>)Expression.Lambda(inv).Compile(true);
            act();
            Assert.Equal(1, holder.Function());
        }
    }
}