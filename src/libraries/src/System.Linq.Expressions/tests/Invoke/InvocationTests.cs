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

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void UnboxReturnsReference(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(object));
            var unbox = Expression.Unbox(p, typeof(Mutable));
            var call = Expression.Call(unbox, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<object, int>>(call, p).Compile(useInterpreter);

            object boxed = new Mutable();
            Assert.Equal(0, lambda(boxed));
            Assert.Equal(1, lambda(boxed));
            Assert.Equal(2, lambda(boxed));
            Assert.Equal(3, lambda(boxed));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Mutable[]));
            var indexed = Expression.ArrayIndex(p, Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[], int>>(call, p).Compile(useInterpreter);

            var array = new Mutable[1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MultiRankArrayWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Mutable[,]));
            var indexed = Expression.ArrayIndex(p, Expression.Constant(0), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[,], int>>(call, p).Compile(useInterpreter);

            var array = new Mutable[1, 1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayAccessWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Mutable[]));
            var indexed = Expression.ArrayAccess(p, Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[], int>>(call, p).Compile(useInterpreter);

            var array = new Mutable[1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MultiRankArrayAccessWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Mutable[,]));
            var indexed = Expression.ArrayAccess(p, Expression.Constant(0), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Mutable[,], int>>(call, p).Compile(useInterpreter);

            var array = new Mutable[1, 1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void IndexedPropertyAccessNoWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(List<Mutable>));
            var indexed = Expression.Property(p, typeof(List<Mutable>).GetProperty("Item"), Expression.Constant(0));
            var call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<List<Mutable>, int>>(call, p).Compile(useInterpreter);

            var list = new List<Mutable> { new Mutable() };
            Assert.Equal(0, lambda(list));
            Assert.Equal(0, lambda(list));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void FieldAccessWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Field(p, typeof(Wrapper<Mutable>).GetField("Field"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(useInterpreter);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(1, lambda(wrapper));
            Assert.Equal(2, lambda(wrapper));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void PropertyAccessNoWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Property(p, typeof(Wrapper<Mutable>).GetProperty("Property"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(useInterpreter);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ReadonlyFieldAccessWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Wrapper<Mutable>));
            var member = Expression.Field(p, typeof(Wrapper<Mutable>).GetField("ReadOnlyField"));
            var call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            var lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(useInterpreter);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConstFieldAccessWriteBack(bool useInterpreter)
        {
            var member = Expression.Field(null, typeof(Wrapper<Mutable>).GetField("Zero"));
            var call = Expression.Call(member, typeof(int).GetMethod("GetType"));
            var lambda = Expression.Lambda<Func<Type>>(call).Compile(useInterpreter);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(typeof(int), lambda());
        }
    }
}
