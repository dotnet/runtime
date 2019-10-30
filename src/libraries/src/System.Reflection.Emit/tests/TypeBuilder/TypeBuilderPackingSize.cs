// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderPackingSize
    {
        [Fact]
        public void PackagingSize_NoneSet_ReturnsUnspecified()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            Assert.Equal(PackingSize.Unspecified, type.PackingSize);

            type.DefineGenericParameters("T", "U");
            Assert.Equal(PackingSize.Unspecified, type.PackingSize);
        }

        [Theory]
        [InlineData(PackingSize.Size1)]
        [InlineData(PackingSize.Size128)]
        public void PackagingSize_Set_ReturnsExpected(PackingSize packingSize)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("TestType", TypeAttributes.Class | TypeAttributes.Public, null, packingSize);
            Assert.Equal(packingSize, type.PackingSize);

            type.DefineGenericParameters("T", "U");
            Assert.Equal(packingSize, type.PackingSize);
        }
    }
}
