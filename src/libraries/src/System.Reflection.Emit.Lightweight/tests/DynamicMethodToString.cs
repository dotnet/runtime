// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class DynamicMethodToString
    {
        [Theory]
        [InlineData(typeof(string), new Type[] { typeof(string), typeof(int), typeof(TestClass) }, "System.String MethodName(System.String, Int32, System.Reflection.Emit.Tests.TestClass)")]
        [InlineData(typeof(string), new Type[] { typeof(GenericClass<>) }, "System.String MethodName(System.Reflection.Emit.Tests.GenericClass`1[T])")]
        [InlineData(null, null, "Void MethodName()")]
        public void ToStringTest(Type returnType, Type[] parameterTypes, string expected)
        {
            DynamicMethod method = new DynamicMethod("MethodName", returnType, parameterTypes, typeof(TestClass).GetTypeInfo().Module);
            Assert.Equal(expected, method.ToString());
        }
    }
}
