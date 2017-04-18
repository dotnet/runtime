// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoConstructor
    {
        public static IEnumerable<object[]> Ctor_String_TestData()
        {
            yield return new object[] { "", new string[] { "" } };
            yield return new object[] { "en", new string[] { "en" } };
            yield return new object[] { "de-DE", new string[] { "de-DE" } };
            yield return new object[] { "de-DE_phoneb", new string[] { "de-DE", "de-DE_phoneb" } };
            yield return new object[] { CultureInfo.CurrentCulture.Name, new string[] { CultureInfo.CurrentCulture.Name } };

            if (!PlatformDetection.IsWindows || PlatformDetection.WindowsVersion >= 10)
            {
                yield return new object[] { "en-US-CUSTOM", new string[] { "en-US-CUSTOM", "en-US-custom" } };
                yield return new object[] { "xx-XX", new string[] { "xx-XX" } };
            }
        }

        [Theory]
        [MemberData(nameof(Ctor_String_TestData))]
        public void Ctor_String(string name, string[] expectedNames)
        {
            CultureInfo culture = new CultureInfo(name);
            Assert.Contains(culture.Name, expectedNames);
            Assert.Equal(name, culture.ToString(), ignoreCase: true);
        }

        [Fact]
        public void Ctor_String_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new CultureInfo(null)); // Name is null
            Assert.Throws<CultureNotFoundException>(() => new CultureInfo("en-US@x=1")); // Name doesn't support ICU keywords
            Assert.Throws<CultureNotFoundException>(() => new CultureInfo("NotAValidCulture")); // Name is invalid

            if (PlatformDetection.IsWindows && PlatformDetection.WindowsVersion < 10)
            {
            	Assert.Throws<CultureNotFoundException>(() => new CultureInfo("no-such-culture"));
                Assert.Throws<CultureNotFoundException>(() => new CultureInfo("en-US-CUSTOM"));
                Assert.Throws<CultureNotFoundException>(() => new CultureInfo("xx-XX"));
            }
        }
    }
}
