// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGUID
    {
        [Fact]
        public void Guid_TypeCreated_NotEmpty()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.CreateTypeInfo().AsType();
            Assert.NotEqual(Guid.Empty, type.GUID);
        }

        [Fact]
        public void Guid_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.Throws<NotSupportedException>(() => type.GUID);
        }
    }
}
