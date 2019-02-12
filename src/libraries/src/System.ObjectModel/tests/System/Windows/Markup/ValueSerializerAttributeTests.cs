// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests
{
    public class ValueSerializerAttributeTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("propertyName", null)]
        [InlineData("System.Int32", typeof(int))]
        [InlineData("System.int32", null)]
        public void Ctor_String(string valueSerializerTypeName, Type expectedValueSerializerType)
        {
            var attribute = new ValueSerializerAttribute(valueSerializerTypeName);
            Assert.Equal(valueSerializerTypeName, attribute.ValueSerializerTypeName);
            Assert.Equal(expectedValueSerializerType, attribute.ValueSerializerType);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void Ctor_Type(Type valueSerializerType)
        {
            var attribute = new ValueSerializerAttribute(valueSerializerType);
            Assert.Equal(valueSerializerType?.AssemblyQualifiedName, attribute.ValueSerializerTypeName);
            Assert.Equal(valueSerializerType, attribute.ValueSerializerType);
        }
    }
}

