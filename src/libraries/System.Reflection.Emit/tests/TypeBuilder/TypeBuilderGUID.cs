// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGUID
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void Guid_TypeCreated_NotEmpty()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.CreateType();
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
