// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class AsyncMethodBuilderAttributeTests
    {
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(AsyncValueTaskMethodBuilder))]
        [InlineData(typeof(AsyncValueTaskMethodBuilder<>))]
        [InlineData(typeof(AsyncValueTaskMethodBuilder<int>))]
        [InlineData(typeof(AsyncValueTaskMethodBuilder<string>))]
        public void Ctor_BuilderType_Roundtrip(Type builderType)
        {
            var amba = new AsyncMethodBuilderAttribute(builderType);
            Assert.Same(builderType, amba.BuilderType);
        }

        // No tests for the following, other than verifying that they successfully compile

        [AsyncMethodBuilder(typeof(string))]
        class MyClass { }

        [AsyncMethodBuilder(typeof(string))]
        struct MyStruct { }

        [AsyncMethodBuilder(typeof(string))]
        interface MyInterface { }

        [AsyncMethodBuilder(typeof(string))]
        enum MyEnum { }

        [AsyncMethodBuilder(typeof(string))]
        delegate void MyDelegate();
    }
}
