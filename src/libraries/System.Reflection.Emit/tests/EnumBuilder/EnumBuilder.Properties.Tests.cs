// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EnumBuilderPropertyTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void Guid_TypeCreated()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.CreateType();
            Assert.NotEqual(Guid.Empty, enumBuilder.GUID);
        }

        [Fact]
        public void Guid_TypeNotCreated_ThrowsNotSupportedException()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.Throws<NotSupportedException>(() => enumBuilder.GUID);
        }

        [Fact]
        public void Namespace()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.AsType();
            Assert.Empty(enumBuilder.Namespace);
        }

        [Fact]
        public void IsArray()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.False(enumBuilder.IsArray);
            Assert.False(enumBuilder.IsSZArray);

            Type asType = enumBuilder.AsType();
            Assert.False(asType.IsArray);
            Assert.False(asType.IsSZArray);

            Type arrType = enumBuilder.MakeArrayType();
            Assert.True(arrType.IsArray);
            Assert.True(arrType.IsSZArray);

            arrType = enumBuilder.MakeArrayType(1);
            Assert.True(arrType.IsArray);
            Assert.False(arrType.IsSZArray);

            arrType = enumBuilder.MakeArrayType(2);
            Assert.True(arrType.IsArray);
            Assert.False(arrType.IsSZArray);
        }

        [Fact]
        public void IsByRefLikeReturnsFalse()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            Assert.False(enumBuilder.IsByRefLike);
        }
    }
}
