// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDeclaringType
    {
        [Fact]
        public void DeclaringType_RootClass_ReturnsNull()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.Null(type.DeclaringType);
        }

        [Fact]
        public void DeclaringType_NestedClass_ReturnsNull()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            TypeBuilder nestedType = type.DefineNestedType("NestedType");
            Assert.Equal(type.Name, nestedType.DeclaringType.Name);
        }
    }
}
