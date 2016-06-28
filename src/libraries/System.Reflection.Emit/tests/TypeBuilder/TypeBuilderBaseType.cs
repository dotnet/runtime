// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderBaseType
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(BaseTypeClass))]
        [InlineData(typeof(BaseTypeGenericClass<>))]
        [InlineData(typeof(int?))]
        public void BaseType(Type parent)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.SetParent(parent);
            Assert.Equal(parent, type.BaseType);
        }

        [Fact]
        public void BaseType_InterfaceType_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.Throws<ArgumentException>(null, () => type.SetParent(typeof(BaseTypeInterface)));
        }

        [Fact]
        public void BaseType_NoParentSet_ReturnsObject()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.Equal(typeof(object), type.BaseType);
        }
    }

    public class BaseTypeClass { }
    public class BaseTypeGenericClass<T> { }
    public interface BaseTypeInterface { }
}
