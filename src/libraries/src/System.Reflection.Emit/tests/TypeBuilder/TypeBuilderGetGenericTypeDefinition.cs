// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetGenericTypeDefinition
    {
        [Fact]
        public void GetGenericTypeDefinition()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");
            Type genericType = type.GetGenericTypeDefinition();
            Assert.Equal("TestType", genericType.Name);
        }

        [Fact]
        public void GetGenericTypeDefinition_NonGenericType_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            Assert.Throws<InvalidOperationException>(() => type.GetGenericTypeDefinition());
        }
    }
}
