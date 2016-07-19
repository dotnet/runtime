// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetHashCode
    {
        public static IEnumerable<object[]> GetHashCode_TestData()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);

            MethodBuilder method1 = type.DefineMethod("TestMethod1", MethodAttributes.Public);
            MethodBuilder method2 = type.DefineMethod("TestMethod1", MethodAttributes.Public);

            MethodBuilder method3 = type.DefineMethod("TestMethod1", MethodAttributes.Public);
            method3.DefineGenericParameters("T", "U");

            MethodBuilder method4 = type.DefineMethod("TestMethod1", MethodAttributes.Public);
            method4.DefineGenericParameters("T", "U");

            MethodBuilder method5 = type.DefineMethod("TestMethod2", MethodAttributes.Public);

            yield return new object[] { method1, method2, true }; // Non-generic
            yield return new object[] { method3, method4, true }; // Generic
            yield return new object[] { method1, method5, false }; // Different names
        }

        [Theory]
        [MemberData(nameof(GetHashCode_TestData))]
        public void GetHashCode(MethodBuilder method1, MethodBuilder method2, bool expected)
        {
            Assert.Equal(expected, method1.GetHashCode().Equals(method2.GetHashCode()));
        }
    }
}
