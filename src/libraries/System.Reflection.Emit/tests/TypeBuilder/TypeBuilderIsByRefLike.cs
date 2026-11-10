// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderIsByRefLike
    {
        [Fact]
        public void IsByRefLikeReturnsFalse()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.False(type.IsByRefLike);
        }

        [Fact]
        public void IsByRefLike_TypesConstructedFromTypeBuilder_ReturnsFalse()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters("T");

            Type[] constructedTypes =
            [
                type.MakeGenericType(typeof(int)),
                type.MakeArrayType(),
                type.MakeArrayType(2),
                type.MakePointerType(),
                type.MakeByRefType(),
                typeParams[0].MakeArrayType(),
                typeParams[0].MakePointerType(),
                typeParams[0].MakeByRefType(),
            ];

            Assert.All(constructedTypes, t => Assert.False(t.IsByRefLike));
        }

        [Theory]
        [InlineData(typeof(Span<>), true)]
        [InlineData(typeof(ReadOnlySpan<>), true)]
        [InlineData(typeof(List<>), false)]
        public void IsByRefLike_RuntimeGenericTypeInstantiatedOverTypeBuilder_MatchesGenericTypeDefinition(Type genericTypeDefinition, bool expected)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Equal(expected, genericTypeDefinition.MakeGenericType(type).IsByRefLike);
        }
    }
}
