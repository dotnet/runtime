// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    public class ConstructorInvokerTests
    {

        [Fact]
        public void Args_0()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(new Type[] { }));
            Assert.Equal("0", ((TestClass)invoker.Invoke())._args);
            Assert.Equal("0", ((TestClass)invoker.Invoke(new Span<object?>()))._args);
        }

        [Fact]
        public void Args_1()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(new Type[] { typeof(string) }));
            Assert.Equal("1", ((TestClass)invoker.Invoke("1"))._args);
            Assert.Equal("1", ((TestClass)invoker.Invoke(new Span<object?>(new object[] { "1" })))._args);
        }

        [Fact]
        public void Args_2()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(
                new Type[] { typeof(string), typeof(string) }));

            Assert.Equal("12", ((TestClass)invoker.Invoke("1", "2"))._args);
            Assert.Equal("12", ((TestClass)invoker.Invoke(new Span<object?>(new object[] { "1", "2" })))._args);
        }

        [Fact]
        public void Args_3()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(
                new Type[] { typeof(string), typeof(string), typeof(string) }));

            Assert.Equal("123", ((TestClass)invoker.Invoke("1", "2", "3"))._args);
            Assert.Equal("123", ((TestClass)invoker.Invoke(new Span<object?>(new object[] { "1", "2", "3" })))._args);
        }

        [Fact]
        public void Args_4()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(
                new Type[] { typeof(string), typeof(string), typeof(string), typeof(string) }));

            Assert.Equal("1234", ((TestClass)invoker.Invoke("1", "2", "3", "4"))._args);
            Assert.Equal("1234", ((TestClass)invoker.Invoke(new Span<object?>(new object[] { "1", "2", "3", "4" })))._args);
        }

        [Fact]
        public void Args_5()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(
                new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) }));

            Assert.Equal("12345", ((TestClass)invoker.Invoke(new Span<object?>(new object[] { "1", "2", "3", "4", "5" })))._args);
        }

        [Fact]
        public void ThrowsNonWrappedException_0()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClassThrowsOnCreate).GetConstructor(new Type[] { }));
            Assert.Throws<InvalidOperationException>(invoker.Invoke);
        }

        [Fact]
        public void ThrowsNonWrappedException_1()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClassThrowsOnCreate).GetConstructor(new Type[] { typeof(string) }));
            Assert.Throws<InvalidOperationException>(() => invoker.Invoke("0"));
        }

        [Fact]
        public void ThrowsNonWrappedException_5()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClassThrowsOnCreate).GetConstructor(
                new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string) }));

            Assert.Throws<InvalidOperationException>(() => invoker.Invoke(new Span<object?>(new object[] { "1", "2", "3", "4", "5" })));
        }

        [Fact]
        public void ExistingInstance()
        {
            ConstructorInfo ci = typeof(TestClass).GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
            TestClass tc = (TestClass)RuntimeHelpers.GetUninitializedObject(typeof(TestClass));
            Assert.Null(tc._args);

            MethodInvoker invoker = MethodInvoker.Create(ci);
            object? obj = invoker.Invoke(tc);
            Assert.Equal("0", tc._args);
            Assert.Null(obj);
        }

        private class TestClass
        {
            public string _args;

            public TestClass() { _args = "0"; }

            public void SomeMethod() { }

            public TestClass(string arg1)
            {
                _args = arg1;
            }

            public TestClass(string arg1, string arg2)
            {
                _args = arg1 + arg2;
            }

            public TestClass(string arg1, string arg2, string arg3)
            {
                _args = arg1 + arg2 + arg3;
            }

            public TestClass(string arg1, string arg2, string arg3, string arg4)
            {
                _args = arg1 + arg2 + arg3 + arg4;
            }

            public TestClass(string arg1, string arg2, string arg3, string arg4, string arg5)
            {
                _args = arg1 + arg2 + arg3 + arg4 + arg5;
            }
        }

        private class TestClassThrowsOnCreate
        {
            public TestClassThrowsOnCreate() =>
                throw new InvalidOperationException();
            public TestClassThrowsOnCreate(string arg1) =>
                throw new InvalidOperationException();
            public TestClassThrowsOnCreate(string arg1, string arg2, string arg3, string arg4, string arg5) =>
                throw new InvalidOperationException();
        }
    }
}
