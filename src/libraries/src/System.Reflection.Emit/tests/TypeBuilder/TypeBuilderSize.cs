// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderSize
    {
        [Fact]
        public void Size_NoneSet_ReturnsUnspecified()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            Assert.Equal(0, type.Size);

            type.DefineGenericParameters("T", "U");
            Assert.Equal(0, type.Size);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(1024)]
        [InlineData(100)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Size_Set_ReturnsExpected(int size)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type = module.DefineType("TestType", TypeAttributes.Class | TypeAttributes.Public, null, size);
            Assert.Equal(size, type.Size);

            type.DefineGenericParameters("T", "U");
            Assert.Equal(size, type.Size);

            // We should be able to create the type no matter the size
            Assert.Equal(type.Name, type.CreateTypeInfo().Name);
        }
    }
}
