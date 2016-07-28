// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderIsGenericMethod
    {
        public static IEnumerable<object[]> IsGenericMethod_TestData()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);

            MethodBuilder method1 = type.DefineMethod("TestMethod1", MethodAttributes.Public);

            MethodBuilder method2 = type.DefineMethod("TestMethod2", MethodAttributes.Public);
            method2.DefineGenericParameters("T");

            MethodBuilder method3 = type.DefineMethod("TestMethod3", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(int) });
            method3.DefineGenericParameters("T");
            method3.DefineParameter(1, ParameterAttributes.HasDefault, "TestParam");

            MethodBuilder method4 = type.DefineMethod("TestMethod4", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(int) });
            method4.DefineParameter(1, ParameterAttributes.HasDefault, "TestParam");

            yield return new object[] { method1, false }; // Non-generic
            yield return new object[] { method2, true }; // Generic method
            yield return new object[] { method3, true }; // Generic parameter
            yield return new object[] { method4, false }; // Non-generic parameter
        }

        [Theory]
        [MemberData(nameof(IsGenericMethod_TestData))]
        public void IsGenericMethod(MethodBuilder method, bool expected)
        {
            Assert.Equal(expected, method.IsGenericMethod);
            Assert.Equal(expected, method.IsGenericMethodDefinition);
        }
    }
}
