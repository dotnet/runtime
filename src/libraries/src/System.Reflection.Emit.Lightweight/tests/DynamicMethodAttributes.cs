// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodAttributesTests
    {
        public const string c_DYNAMICMETHODNAME = "MethodName";

        [Fact]
        public void PosTest1()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, null, null, typeof(TestClass).GetTypeInfo().Module, true);
            MethodAttributes methodattribute = dynamicmethod.Attributes;
            Assert.False(methodattribute != (MethodAttributes.Public | MethodAttributes.Static));
        }

        [Fact]
        public void PosTest2()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, MethodAttributes.Static | MethodAttributes.Family | MethodAttributes.FamANDAssem, CallingConventions.Standard, null, null, typeof(TestClass).GetTypeInfo().Module, true);
            MethodAttributes methodattribute = dynamicmethod.Attributes;
            Assert.False(methodattribute != (MethodAttributes.Public | MethodAttributes.Static));
        }
    }

    public class TestClass
    {
    }

    public class GenClass1<T>
    {
    }

    public class GenClass2<T, K>
    {
    }

    public interface TestInter
    {
    }
}