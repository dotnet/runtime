// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoParent
    {
        [Theory]
        [InlineData("en-US", "en")]
        [InlineData("en", "")]
        [InlineData("", "")]
        public void Parent(string name, string expectedParentName)
        {
            CultureInfo culture = new CultureInfo(name);
            Assert.Equal(new CultureInfo(expectedParentName), culture.Parent);
        }

        [Fact]
        public void Parent_ParentChain()
        {
            CultureInfo myExpectParentCulture = new CultureInfo("uz-Cyrl-UZ");
            Assert.Equal("uz-Cyrl", myExpectParentCulture.Parent.Name);
            Assert.Equal("uz", myExpectParentCulture.Parent.Parent.Name);
            Assert.Equal("", myExpectParentCulture.Parent.Parent.Parent.Name);
        }
    }
}
