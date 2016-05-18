// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Primitives.Tests
{
    public class RefreshPropertiesAttributeTests
    {
        [Fact]
        public void Equals_DifferentValues()
        {
            Assert.False(RefreshPropertiesAttribute.All.Equals(RefreshPropertiesAttribute.Repaint));
        }

        [Fact]
        public void Equals_Null()
        {
            Assert.False(RefreshPropertiesAttribute.All.Equals(null));
        }

        [Fact]
        public void Equals_SameValue()
        {
            Assert.True(RefreshPropertiesAttribute.Default.Equals(RefreshPropertiesAttribute.Default));
        }

        [Theory]
        [InlineData(RefreshProperties.All)]
        [InlineData(RefreshProperties.None)]
        [InlineData(RefreshProperties.Repaint)]
        public void GetRefreshProperties(RefreshProperties value)
        {
            var attribute = new RefreshPropertiesAttribute(value);

            Assert.Equal(value, attribute.RefreshProperties);
        }

        [Theory]
        [MemberData(nameof(RefreshPropertiesAttributeData))]
        public void NameTests(RefreshPropertiesAttribute attribute, RefreshProperties refreshProperties)
        {
            Assert.Equal(refreshProperties, attribute.RefreshProperties);
        }

        private static IEnumerable<object[]> RefreshPropertiesAttributeData()
        {
            yield return new object[] { RefreshPropertiesAttribute.Default, RefreshProperties.None };
            yield return new object[] { RefreshPropertiesAttribute.All, RefreshProperties.All };
            yield return new object[] { RefreshPropertiesAttribute.Repaint, RefreshProperties.Repaint };
        }
    }
}
