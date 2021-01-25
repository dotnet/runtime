// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderIsGenericType
    {
        [Fact]
        public void IsGenericType_NonGenericType_ReturnsTrye()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.False(type.IsGenericType);
        }
    }
}
