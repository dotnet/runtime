// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    /// <summary>
    /// These tests use the shared tests from the base class with ConstructorInvoker.Invoke.
    /// </summary>
    public sealed class ConstructorInvokerTests : ConstructorCommonTests
    {
        public override object Invoke(ConstructorInfo constructorInfo, object?[]? parameters)
        {
            return ConstructorInvoker.Create(constructorInfo).Invoke(new Span<object>(parameters));
        }

        public override object? Invoke(ConstructorInfo constructorInfo, object obj, object?[]? parameters)
        {
            return MethodInvoker.Create(constructorInfo).Invoke(obj, new Span<object>(parameters));
        }

        protected override bool IsExceptionWrapped => false;

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
        public void Args_0_Extra_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(new Type[] { }));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(42));
        }

        [Fact]
        public void Args_1_Extra_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(new Type[] { typeof(string) }));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke("1", 42));
        }

        [Fact]
        public void Args_2_Extra_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(
                new Type[] { typeof(string), typeof(string) }));

            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke("1", "2", 42));
        }

        [Fact]
        public void Args_3_Extra_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(
                new Type[] { typeof(string), typeof(string), typeof(string) }));

            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke("1", "2", "3", 42));
        }

        [Fact]
        public void Args_Span_Extra_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(new Type[] { }));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(new Span<object?>(new object[]{"1", "2"})));
        }

        [Fact]
        public void Args_1_NotEnoughArgs_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(new Type[] { typeof(string) }));
            Assert.Throws<TargetParameterCountException>(invoker.Invoke);
        }

        [Fact]
        public void Args_2_NotEnoughArgs_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(
                new Type[] { typeof(string), typeof(string) }));

            Assert.Throws<TargetParameterCountException>(invoker.Invoke);
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke("1"));
        }

        [Fact]
        public void Args_3_NotEnoughArgs_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(
                new Type[] { typeof(string), typeof(string), typeof(string) }));

            Assert.Throws<TargetParameterCountException>(invoker.Invoke);
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke("1"));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke("1", "2"));
        }

        [Fact]
        public void Args_Span_NotEnoughArgs_Throws()
        {
            ConstructorInvoker invoker = ConstructorInvoker.Create(typeof(TestClass).GetConstructor(new Type[] { typeof(string) }));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(new Span<object?>()));
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
        public void Invoke_StaticConstructor_NullObject_NullParameters()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWithStaticConstructor));
            Assert.Equal(1, constructors.Length);

            // Invoker classes do not support calling class constructors; use standard reflection for that.
            Assert.Throws<MemberAccessException>(() => Invoke(constructors[0], null, new object[] { }));
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
