// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class ObfuscationAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new ObfuscationAttribute();
            Assert.True(attribute.ApplyToMembers);
            Assert.True(attribute.Exclude);
            Assert.Equal("all", attribute.Feature);
            Assert.True(attribute.StripAfterObfuscation);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ApplyToMembers_Set_GetReturnsExpected(bool value)
        {
            var attribute = new ObfuscationAttribute
            {
                ApplyToMembers = value
            };
            Assert.Equal(value, attribute.ApplyToMembers);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Exclude_Set_GetReturnsExpected(bool value)
        {
            var attribute = new ObfuscationAttribute
            {
                Exclude = value
            };
            Assert.Equal(value, attribute.Exclude);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("feature")]
        public void Feature_Set_GetReturnsExpected(string value)
        {
            var attribute = new ObfuscationAttribute
            {
                Feature = value
            };
            Assert.Equal(value, attribute.Feature);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StripAfterObfuscation_Set_GetReturnsExpected(bool value)
        {
            var attribute = new ObfuscationAttribute
            {
                StripAfterObfuscation = value
            };
            Assert.Equal(value, attribute.StripAfterObfuscation);
        }
    }
}
