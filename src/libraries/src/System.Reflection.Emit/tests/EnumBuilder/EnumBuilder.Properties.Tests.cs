// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EnumBuilderPropertyTests
    {
        [Fact]
        public void Guid_TypeCreated()
        {
            EnumBuilder enumBuilder = Helpers.DynamicEnum(TypeAttributes.Public, typeof(int));
            enumBuilder.CreateTypeInfo().AsType();
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
    }
}
