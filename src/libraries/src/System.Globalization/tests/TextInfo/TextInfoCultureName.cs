// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoCultureName
    {
        // TestTextInfoCultureName: Verify the TextInfo from specific locales
        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        public void TestTextInfoCultureName(string localeName)
        {
            Assert.Equal(localeName, new CultureInfo(localeName).TextInfo.CultureName);
        }

        // TestTextInfoCasingCultureName: Verify the TextInfo from mismatched casing
        [Theory]
        [InlineData("EN-us", "en-US")]
        [InlineData("FR-fr", "fr-FR")]
        public void TestTextInfoCasingCultureName(string localeName, string expectedLocaleName)
        {
            Assert.Equal(expectedLocaleName, new CultureInfo(localeName).TextInfo.CultureName);
        }

        // TestInvariantCultureName: Verify the invariant TextInfo
        [Fact]
        public void TestInvariantCultureName()
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            string localeName = ci.Name;
            Assert.Equal(ci.TextInfo.CultureName, localeName);
        }
    }
}

