// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodToString
    {
        public const string c_DYNAMICMETHODNAME = "MethodName";

        [Fact]
        public void PosTest1()
        {
            Type[] typeparameters = { typeof(string), typeof(int), typeof(ToStringTestClass) };
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(string), typeparameters, typeof(ToStringTestClass).GetTypeInfo().Module);
            string strvalue = dynamicmethod.ToString();
            Assert.Equal(strvalue, "System.String MethodName(System.String, Int32, System.Reflection.Emit.Lightweight.Tests.ToStringTestClass)");
        }

        [Fact]
        public void PosTest2()
        {
            string method_name = "TestDynamicMethodName";
            Type[] typeparameters = { typeof(ToStringGenClass1<>) };
            DynamicMethod dynamicmethod = new DynamicMethod(method_name, typeof(string), typeparameters, typeof(ToStringTestClass).GetTypeInfo().Module);
            string strvalue = dynamicmethod.ToString();
            Assert.NotNull(strvalue);
        }

        [Fact]
        public void PosTest3()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, null, null, typeof(ToStringTestClass).GetTypeInfo().Module);
            string strvalue = dynamicmethod.ToString();
            Assert.Equal(strvalue, "Void MethodName()");
        }
    }

    public class ToStringTestClass
    {
    }

    public class ToStringGenClass1<T>
    {
    }

    public class ToStringGenClass2<T, K>
    {
    }

    public interface ToStringTestInter
    {
    }
}