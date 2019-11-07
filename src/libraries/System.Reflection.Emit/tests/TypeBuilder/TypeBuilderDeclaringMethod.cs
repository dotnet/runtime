// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDeclaringMethod
    {
        [Fact]
        public void DeclaringMethod_TypeBuilder_ReturnsNull()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.DefineGenericParameters("T1", "T2");
            Assert.Null(type.DeclaringMethod);
        }
    }
}
