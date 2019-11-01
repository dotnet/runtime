// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest3
    {
        [Theory]
        [InlineData(PropertyAttributes.HasDefault)]
        [InlineData(PropertyAttributes.None)]
        [InlineData(PropertyAttributes.RTSpecialName)]
        [InlineData(PropertyAttributes.SpecialName)]
        [InlineData(PropertyAttributes.SpecialName | PropertyAttributes.RTSpecialName | PropertyAttributes.None | PropertyAttributes.HasDefault)]
        public void ExecutePosTest(PropertyAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", attributes, typeof(int), null);
            Assert.Equal(attributes, property.Attributes);
        }
    }
}
