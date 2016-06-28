// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderMakeArrayType
    {
        [Theory]
        [InlineData(TypeAttributes.Public | TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.NotPublic)]
        public void MakeArrayType(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(attributes);
            Type arrayType = type.MakeArrayType();
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("TestType[]", arrayType.Name);
        }

        [Theory]
        [InlineData(TypeAttributes.Public | TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.NotPublic)]
        public void MakeArrayType_Int_One(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(attributes);
            Type arrayType = type.MakeArrayType(1);
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("TestType[*]", arrayType.Name);
        }

        [Theory]
        [InlineData(TypeAttributes.Public | TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.NotPublic)]
        public void MakeArrayType_Int_Three(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(attributes);
            Type arrayType = type.MakeArrayType(3);
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("TestType[,,]", arrayType.Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void MakeArrayType_Int_InvalidRank_ThrowsIndexOutOfRangeException(int rank)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public | TypeAttributes.Abstract);
            Assert.Throws<IndexOutOfRangeException>(() => type.MakeArrayType(rank));
        }
    }
}
