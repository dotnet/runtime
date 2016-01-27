// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoConstructor2
    {
        [Theory]
        [InlineData("")]
        [InlineData("en")]
        [InlineData("de-DE")]
        [InlineData("de-DE_phoneb")]
        public void TestDiffCulture(string localeName)
        {
            CultureInfo myCulture = new CultureInfo(localeName);
        }

        [PlatformSpecific(PlatformID.AnyUnix)] //todo: Win10 also has these semantics
        [InlineData("en-US-CUSTOM")]
        [InlineData("xx-XX")]
        public void TestCustomCulture(string localeName)
        {
            CultureInfo myCulture = new CultureInfo(localeName);
        }

        [PlatformSpecific(PlatformID.Windows)] //todo: remove this test for Win10, as it should work with TestCustomCulture test above
        [InlineData("en-US-CUSTOM")]
        [InlineData("xx-XX")]
        public void TestCustomCultureWindows(string localeName)
        {
            Assert.Throws<ArgumentNullException>(() => new CultureInfo(localeName));
        }

        [Fact]
        public void TestNullCulture()
        {
            Assert.Throws<ArgumentNullException>(() => new CultureInfo(null));
        }

        [Theory]
        [InlineData("NotAValidCulture")]
        [InlineData("en-US@x=1")] // don't support ICU keywords
        public void TestInvalidCulture(string cultureName)
        {
            Assert.Throws<CultureNotFoundException>(() => new CultureInfo(cultureName));
        }
    }
}
