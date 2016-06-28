// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderIsGenericParameter
    {
        [Fact]
        public void IsGenericParameter_GenericType_ReturnsTrue()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters("T1", "T2", "T3");
            for (int i = 0; i < 3; i++)
            {
                Assert.True(typeParams[i].IsGenericParameter);
            }
        }

        [Fact]
        public void IsGenericParameter_NonGenericType_ReturnsFalse()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.False(type.IsGenericParameter);
        }
    }
}
