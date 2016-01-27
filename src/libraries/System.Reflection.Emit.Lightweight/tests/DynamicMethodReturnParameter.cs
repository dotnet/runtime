// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
