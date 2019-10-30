// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class AttributeUsageAttributeTests
    {
        [Theory]
        [InlineData(AttributeTargets.Delegate | AttributeTargets.GenericParameter)]
        [InlineData((AttributeTargets)(AttributeTargets.All + 1))]
        public void Ctor_AttributeTargets(AttributeTargets validOn)
        {
            var attribute = new AttributeUsageAttribute(validOn);
            Assert.Equal(validOn, attribute.ValidOn);
            Assert.False(attribute.AllowMultiple);
            Assert.True(attribute.Inherited);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AllowMultiple_Set_GetReturnsExpected(bool value)
        {
            var attribute = new AttributeUsageAttribute(AttributeTargets.All)
            {
                AllowMultiple = value
            };
            Assert.Equal(value, attribute.AllowMultiple);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Inherited_Set_GetReturnsExpected(bool value)
        {
            var attribute = new AttributeUsageAttribute(AttributeTargets.All)
            {
                Inherited = value
            };
            Assert.Equal(value, attribute.Inherited);
        }
    }
}
