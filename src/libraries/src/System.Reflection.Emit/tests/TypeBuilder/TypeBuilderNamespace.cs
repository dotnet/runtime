// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderNamespace
    {
        [Theory]
        [InlineData("TestType", "")]
        [InlineData("Namespace.TestType", "Namespace")]
        [InlineData("Namespace1.Namespace2.TestType", "Namespace1.Namespace2")]
        public void Namespace(string typeName, string expectedNamespace)
        {
            TypeBuilder nonGenericType = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public, typeName: typeName);
            Assert.Equal(expectedNamespace, nonGenericType.Namespace);

            nonGenericType.DefineGenericParameters("T", "U");
            Assert.Equal(expectedNamespace, nonGenericType.Namespace);
        }
    }
}
