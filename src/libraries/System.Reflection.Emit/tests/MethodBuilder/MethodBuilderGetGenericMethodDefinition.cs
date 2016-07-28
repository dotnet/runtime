// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetGenericMethodDefinition
    {
        [Fact]
        public void GetGenericMethodDefinition()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            method.DefineGenericParameters("T", "U");
            Assert.True(method.GetGenericMethodDefinition().Equals(method));
        }

        [Fact]
        public void GetGenericMethodDefinition_NonGenericMethod_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            Assert.Throws<InvalidOperationException>(() => method.GetGenericMethodDefinition());
        }
    }
}
