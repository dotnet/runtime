// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class CallTests
    {
        private struct Mutable
        {
            private int x;

            public int X
            {
                get { return x; }
                set { x = value; }
            }

            public int this[int i]
            {
                get { return x; }
                set { x = value; }
            }

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

        private static class Methods
        {
            public static void ByRef(ref int x) { ++x; }
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

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallByRefMutableStructPropertyWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Mutable));
            var x = Expression.Property(p, "X");
            var call = Expression.Call(typeof(Methods).GetMethod("ByRef"), x);
            var body = Expression.Block(call, x);
            var lambda = Expression.Lambda<Func<Mutable, int>>(body, p).Compile(useInterpreter);

            var m = new Mutable() { X = 41 };
            Assert.Equal(42, lambda(m));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallByRefMutableStructIndexWriteBack(bool useInterpreter)
        {
            var p = Expression.Parameter(typeof(Mutable));
            var x = Expression.MakeIndex(p, typeof(Mutable).GetProperty("Item"), new[] { Expression.Constant(0) });
            var call = Expression.Call(typeof(Methods).GetMethod("ByRef"), x);
            var body = Expression.Block(call, x);
            var lambda = Expression.Lambda<Func<Mutable, int>>(body, p).Compile(useInterpreter);

            var m = new Mutable() { X = 41 };
            Assert.Equal(42, lambda(m));
        }

        [Fact]
        public static void ToStringTest()
        {
            // NB: Static methods are inconsistent compared to static members; the declaring type is not included

            var e1 = Expression.Call(null, typeof(SomeMethods).GetMethod(nameof(SomeMethods.S0), BindingFlags.Static | BindingFlags.Public));
            Assert.Equal("S0()", e1.ToString());

            var e2 = Expression.Call(null, typeof(SomeMethods).GetMethod(nameof(SomeMethods.S1), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"));
            Assert.Equal("S1(x)", e2.ToString());

            var e3 = Expression.Call(null, typeof(SomeMethods).GetMethod(nameof(SomeMethods.S2), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"));
            Assert.Equal("S2(x, y)", e3.ToString());

            var e4 = Expression.Call(Expression.Parameter(typeof(SomeMethods), "o"), typeof(SomeMethods).GetMethod(nameof(SomeMethods.I0), BindingFlags.Instance | BindingFlags.Public));
            Assert.Equal("o.I0()", e4.ToString());

            var e5 = Expression.Call(Expression.Parameter(typeof(SomeMethods), "o"), typeof(SomeMethods).GetMethod(nameof(SomeMethods.I1), BindingFlags.Instance | BindingFlags.Public), Expression.Parameter(typeof(int), "x"));
            Assert.Equal("o.I1(x)", e5.ToString());

            var e6 = Expression.Call(Expression.Parameter(typeof(SomeMethods), "o"), typeof(SomeMethods).GetMethod(nameof(SomeMethods.I2), BindingFlags.Instance | BindingFlags.Public), Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"));
            Assert.Equal("o.I2(x, y)", e6.ToString());

            var e7 = Expression.Call(null, typeof(ExtensionMethods).GetMethod(nameof(ExtensionMethods.E0), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"));
            Assert.Equal("x.E0()", e7.ToString());

            var e8 = Expression.Call(null, typeof(ExtensionMethods).GetMethod(nameof(ExtensionMethods.E1), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"));
            Assert.Equal("x.E1(y)", e8.ToString());

            var e9 = Expression.Call(null, typeof(ExtensionMethods).GetMethod(nameof(ExtensionMethods.E2), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"), Expression.Parameter(typeof(int), "z"));
            Assert.Equal("x.E2(y, z)", e9.ToString());
        }
    }

    class SomeMethods
    {
        public static void S0() {}
        public static void S1(int x) {}
        public static void S2(int x, int y) {}

        public void I0() {}
        public void I1(int x) {}
        public void I2(int x, int y) {}
    }

    static class ExtensionMethods
    {
         public static void E0(this int x) {}
         public static void E1(this int x, int y) {}
         public static void E2(this int x, int y, int z) {}
    }
}
