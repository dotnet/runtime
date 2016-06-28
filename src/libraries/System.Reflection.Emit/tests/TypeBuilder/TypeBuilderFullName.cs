// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderFullName
    {
        [Theory]
        [InlineData("TestType")]
        [InlineData("testype")]
        [InlineData("Test Type")]
        public void FullName(string typeName)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic, typeName: typeName);
            Assert.Equal(typeName, type.FullName);
        }

        [Fact]
        public void FullName_NestedType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic, typeName: "Parent");
            TypeBuilder nestedType = type.DefineNestedType("Nested");
            Assert.Equal("Parent+Nested", nestedType.FullName);
        }
    }
}
