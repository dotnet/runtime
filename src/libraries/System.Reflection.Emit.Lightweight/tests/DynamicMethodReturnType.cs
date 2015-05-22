// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodReturnType
    {
        public const string c_DYNAMICMETHODNAME = "MethodName";

        [Fact]
        public void PosTest1()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(String), null, typeof(ReturnTypeTestClass).GetTypeInfo().Module);
            Type type = dynamicmethod.ReturnType;
            Assert.Equal(type, typeof(String));
        }

        [Fact]
        public void PosTest2()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(char?), null, typeof(ReturnTypeTestClass).GetTypeInfo().Module);
            Type type = dynamicmethod.ReturnType;
            Assert.Equal(type, typeof(char?));
        }

        [Fact]
        public void PosTest3()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(ReturnTypeGenClass<>), null, typeof(ReturnTypeTestClass).GetTypeInfo().Module);
            Type type = dynamicmethod.ReturnType;
            Assert.Equal(type, typeof(ReturnTypeGenClass<>));
        }

        [Fact]
        public void PosTest4()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(ReturnTypeTestClass), null, typeof(ReturnTypeTestClass).GetTypeInfo().Module);
            Type type = dynamicmethod.ReturnType;
            Assert.Equal(type, typeof(ReturnTypeTestClass));
        }

        [Fact]
        public void PosTest5()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(ReturnTypeTestInterface), null, typeof(ReturnTypeTestClass).GetTypeInfo().Module);
            Type type = dynamicmethod.ReturnType;
            Assert.Equal(type, typeof(ReturnTypeTestInterface));
        }

        [Fact]
        public void PosTest6()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, null, null, typeof(ReturnTypeTestClass).GetTypeInfo().Module);
            Type type = dynamicmethod.ReturnType;
            Assert.Equal(type, typeof(void));
        }
    }

    public class ReturnTypeTestClass
    {
    }

    public class ReturnTypeGenClass<T>
    {
    }

    public interface ReturnTypeTestInterface
    {
    }
}