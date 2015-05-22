// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodGetParameters
    {
        public const string c_DYNAMICMETHODNAME = "MethodName";

        [Fact]
        public void PosTest1()
        {
            Type[] typeparameters = { typeof(string), typeof(int), typeof(TestGetParametersClass) };
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(string), typeparameters, typeof(TestGetParametersClass).GetTypeInfo().Module);
            ParameterInfo[] parameterinfo = dynamicmethod.GetParameters();
            Assert.False(parameterinfo.Length != 3);
            Assert.False((parameterinfo[0].ParameterType != typeof(String)) || (parameterinfo[1].ParameterType != typeof(int)) || (parameterinfo[2].ParameterType != typeof(TestGetParametersClass)));
        }

        [Fact]
        public void PosTest2()
        {
            Type[] typeparameters = { typeof(char?) };
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(string), typeparameters, typeof(TestGetParametersClass).GetTypeInfo().Module);
            ParameterInfo[] parameterinfo = dynamicmethod.GetParameters();
            Assert.False((parameterinfo.Length != 1) || (parameterinfo[0].ParameterType != typeof(char?)));
        }

        [Fact]
        public void PosTest3()
        {
            Type[] typeparameters = { typeof(GetParametersGenClass1<>), typeof(GetParametersGenClass2<,>) };
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(string), typeparameters, typeof(TestGetParametersClass).GetTypeInfo().Module);
            ParameterInfo[] parameterinfo = dynamicmethod.GetParameters();
            Assert.False((parameterinfo.Length != 2) || (parameterinfo[0].ParameterType != typeof(GetParametersGenClass1<>)) || (parameterinfo[1].ParameterType != typeof(GetParametersGenClass2<,>)));
        }

        [Fact]
        public void PosTest4()
        {
            Type[] typeparameters = { typeof(GetParametersTestInter) };
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(string), typeparameters, typeof(TestGetParametersClass).GetTypeInfo().Module);
            ParameterInfo[] parameterinfo = dynamicmethod.GetParameters();
            Assert.False((parameterinfo.Length != 1) || (parameterinfo[0].ParameterType != typeof(GetParametersTestInter)));
        }

        [Fact]
        public void PosTest5()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, typeof(string), null, typeof(TestGetParametersClass).GetTypeInfo().Module);
            ParameterInfo[] parameterinfo = dynamicmethod.GetParameters();
            Assert.False(parameterinfo.Length != 0);
        }
    }


    public class TestGetParametersClass
    {
    }

    public class GetParametersGenClass1<T>
    {
    }

    public class GetParametersGenClass2<T, K>
    {
    }

    public interface GetParametersTestInter
    {
    }
}