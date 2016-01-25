// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class InvocationTests
    {
        public delegate void X(X a);

        private struct Mutable
        {
            private int x;
            public int Foo()
            {
                return x++;
            }
        }

        private class Wrapper<T>
        {
            public const int Zero = 0;
            public T Field;
#pragma warning disable 649 // For testing purposes
            public readonly T ReadOnlyField;
#pragma warning restore
            public T Property
            {
                get { return Field; }
                set { Field = value; }
            }
        }

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

        [Fact]
        public static void UnboxReturnsReferenceCompiled()
        {
            var p = Expression.Parameter(typeof(object));
            var unbox = Expression.Unbox(p, typeof(Mutable));
            var call = Expression.Call(unbox, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<object, int>>(call, p).Compile(false);

            object boxed = new Mutable();
            Assert.Equal(0, lambda(boxed));
            Assert.Equal(1, lambda(boxed));
            Assert.Equal(2, lambda(boxed));
            Assert.Equal(3, lambda(boxed));
        }

        [Fact]
        public static void UnboxReturnsReferenceInterpretted()
        {
            var p = Expression.Parameter(typeof(object));
            var unbox = Expression.Unbox(p, typeof(Mutable));
            var call = Expression.Call(unbox, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<object, int>>(call, p).Compile(true);

            object boxed = new Mutable();
            Assert.Equal(0, lambda(boxed));
            Assert.Equal(1, lambda(boxed));
            Assert.Equal(2, lambda(boxed));
            Assert.Equal(3, lambda(boxed));
        }

        [Fact]
        public static void ArrayWriteBackCompiled()
        {
            var p = Expression.Parameter(typeof(Mutable[]));
            var indexed = Expression.ArrayIndex(p, Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[], int>>(call, p).Compile(false);

            var array = new Mutable[1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Fact]
        public static void ArrayWriteBackInterpretted()
        {
            var p = Expression.Parameter(typeof(Mutable[]));
            var indexed = Expression.ArrayIndex(p, Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[], int>>(call, p).Compile(true);

            var array = new Mutable[1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Fact]
        public static void MultiRankArrayWriteBackCompiled()
        {
            var p = Expression.Parameter(typeof(Mutable[,]));
            var indexed = Expression.ArrayIndex(p, Expression.Constant(0), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[,], int>>(call, p).Compile(false);

            var array = new Mutable[1, 1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Fact]
        public static void MultiRankArrayWriteBackInterpretted()
        {
            var p = Expression.Parameter(typeof(Mutable[,]));
            var indexed = Expression.ArrayIndex(p, Expression.Constant(0), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[,], int>>(call, p).Compile(true);

            var array = new Mutable[1, 1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Fact]
        public static void ArrayAccessWriteBackCompiled()
        {
            var p = Expression.Parameter(typeof(Mutable[]));
            var indexed = Expression.ArrayAccess(p, Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[], int>>(call, p).Compile(false);

            var array = new Mutable[1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Fact]
        public static void ArrayAccessWriteBackInterpretted()
        {
            var p = Expression.Parameter(typeof(Mutable[]));
            var indexed = Expression.ArrayAccess(p, Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[], int>>(call, p).Compile(true);

            var array = new Mutable[1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Fact]
        public static void MultiRankArrayAccessWriteBackCompiled()
        {
            var p = Expression.Parameter(typeof(Mutable[,]));
            var indexed = Expression.ArrayAccess(p, Expression.Constant(0), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[,], int>>(call, p).Compile(false);

            var array = new Mutable[1, 1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Fact]
        public static void MultiRankArrayAccessWriteBackInterpretted()
        {
            var p = Expression.Parameter(typeof(Mutable[,]));
            var indexed = Expression.ArrayAccess(p, Expression.Constant(0), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[,], int>>(call, p).Compile(true);

            var array = new Mutable[1, 1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Fact]
        public static void IndexedPropertyAccessNoWriteBackCompiled()
        {
            var p = Expression.Parameter(typeof(List<Mutable>));
            var indexed = Expression.Property(p, typeof(List<Mutable>).GetProperty("Item"), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<List<Mutable>, int>>(call, p).Compile(false);

            var list = new List<Mutable> { new Mutable() };
            Assert.Equal(0, lambda(list));
            Assert.Equal(0, lambda(list));
        }

        [Fact]
        public static void IndexedPropertyAccessNoWriteBackInterpretted()
        {
            var p = Expression.Parameter(typeof(List<Mutable>));
            var indexed = Expression.Property(p, typeof(List<Mutable>).GetProperty("Item"), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<List<Mutable>, int>>(call, p).Compile(true);

            var list = new List<Mutable> { new Mutable() };
            Assert.Equal(0, lambda(list));
            Assert.Equal(0, lambda(list));
        }

        [Fact]
        public static void FieldAccessWriteBackCompiled()
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Field(p, typeof(Wrapper<Mutable>).GetField("Field"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(false);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(1, lambda(wrapper));
            Assert.Equal(2, lambda(wrapper));
        }

        [Fact]
        public static void FieldAccessWriteBackIntepretted()
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Field(p, typeof(Wrapper<Mutable>).GetField("Field"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(true);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(1, lambda(wrapper));
            Assert.Equal(2, lambda(wrapper));
        }

        [Fact]
        public static void PropertyAccessNoWriteBackCompiled()
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Property(p, typeof(Wrapper<Mutable>).GetProperty("Property"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(false);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
        }

        [Fact]
        public static void PropertyAccessNoWriteBackIntepretted()
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Property(p, typeof(Wrapper<Mutable>).GetProperty("Property"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(true);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
        }

        [Fact]
        public static void ReadonlyFieldAccessWriteBackCompiled()
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Field(p, typeof(Wrapper<Mutable>).GetField("ReadOnlyField"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(false);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
        }

        [Fact]
        public static void ReadonlyFieldAccessWriteBackInterpreted()
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Field(p, typeof(Wrapper<Mutable>).GetField("ReadOnlyField"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(true);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
        }

        [Fact]
        public static void ConstFieldAccessWriteBackCompiled()
        {
            var member = Expression.Field(null, typeof(Wrapper<Mutable>).GetField("Zero"));
            var call = Expression.Call(member, typeof(int).GetMethod("GetType"));
            var lambda = Expression.Lambda<Func<Type>>(call).Compile(false);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(typeof(int), lambda());
        }

        [Fact]
        public static void ConstFieldAccessWriteBackInterpreted()
        {
            var member = Expression.Field(null, typeof(Wrapper<Mutable>).GetField("Zero"));
            var call = Expression.Call(member, typeof(int).GetMethod("GetType"));
            var lambda = Expression.Lambda<Func<Type>>(call).Compile(true);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(typeof(int), lambda());
        }
    }
}
