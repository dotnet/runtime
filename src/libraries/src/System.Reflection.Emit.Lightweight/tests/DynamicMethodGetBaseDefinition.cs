// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class DynamicMethodGetBaseDefinition
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateDynMethod_TypeOwner(bool skipVisibility)
        {
            Type[] parameterTypes = new Type[] { typeof(TestClass), typeof(int) };

            DynamicMethod method = new DynamicMethod("Method", typeof(int), parameterTypes, typeof(TestClass), skipVisibility);
            MethodInfo baseDefinition = method.GetBaseDefinition();
            Assert.Equal(method, baseDefinition);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateDynMethod_Module(bool skipVisibility)
        {
            Module module = typeof(TestClass).GetTypeInfo().Module;
            Type[] parameterTypes = new Type[] { typeof(TestClass), typeof(int) };

            DynamicMethod method = new DynamicMethod("Method", typeof(int), parameterTypes, module, skipVisibility);
            MethodInfo baseDefinition = method.GetBaseDefinition();
            Assert.Equal(method, baseDefinition);
        }
    }
}
