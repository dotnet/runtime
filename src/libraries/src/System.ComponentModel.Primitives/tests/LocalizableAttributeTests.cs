// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class LocalizableAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(LocalizableAttribute.Yes.Equals(LocalizableAttribute.No));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(LocalizableAttribute.Yes.Equals(LocalizableAttribute.Yes));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetIsLocalizable(bool value)
        {
            var attribute = new LocalizableAttribute(value);

            Assert.Equal(value, attribute.IsLocalizable);
        }
    }
}
