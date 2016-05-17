// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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

        [Theory]
        [MemberData(nameof(LocalizableAttributeData))]
        public void NameTests(LocalizableAttribute attribute, bool isLocalizable)
        {
            Assert.Equal(isLocalizable, attribute.IsLocalizable);
        }

        private static IEnumerable<object[]> LocalizableAttributeData()
        {
            yield return new object[] { LocalizableAttribute.Default, false };
            yield return new object[] { new LocalizableAttribute(true), true };
            yield return new object[] { new LocalizableAttribute(false), false };
        }
    }
}
