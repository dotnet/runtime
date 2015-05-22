// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodReturnParameter
    {
        public const string c_DYNAMICMETHODNAME = "MethodName";

        [Fact]
        public void PosTest1()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, null, null, typeof(ReturnParameterTestClass).GetTypeInfo().Module);
            ParameterInfo parameterinfo = dynamicmethod.ReturnParameter;
            Assert.Null(parameterinfo);
        }
    }

    public class ReturnParameterTestClass
    {
    }
}
