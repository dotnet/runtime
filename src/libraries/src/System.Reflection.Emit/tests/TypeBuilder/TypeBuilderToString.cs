// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderToString
    {
        [Theory]
        [InlineData("TestType")]
        [InlineData("Test-Type")]
        [InlineData("Test_Type")]
        [InlineData("Test Type")]
        [InlineData("   ")]
        public void ToString(string typeName)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic, typeName: typeName);
            Assert.Equal(typeName, type.ToString());
        }
    }
}
