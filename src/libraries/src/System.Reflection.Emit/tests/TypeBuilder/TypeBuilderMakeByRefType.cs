// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderMakeByRefType
    {
        [Theory]
        [InlineData(TypeAttributes.Public | TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.NotPublic)]
        public void MakeByRefType(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(attributes);
            Type arrayType = type.MakeByRefType();
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("TestType&", arrayType.Name);
        }
    }
}
