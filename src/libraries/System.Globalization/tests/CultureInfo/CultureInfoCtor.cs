// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1903OrGreater))]
        [InlineData(0x2000)]
        [InlineData(0x2400)]
        [InlineData(0x2800)]
        [InlineData(0x2C00)]
        [InlineData(0x3000)]
        [InlineData(0x3400)]
        [InlineData(0x3800)]
        [InlineData(0x3C00)]
        [InlineData(0x4000)]
        [InlineData(0x4400)]
        [InlineData(0x4800)]
        [InlineData(0x4C00)]
        public void TestCreationWithTemporaryLCID(int lcid)
        {
            // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/926e694f-1797-4418-a922-343d1c5e91a6
            // If a temporary LCID is assigned it will be dynamically assigned at runtime to be
            // 0x2000, 0x2400, 0x2800, 0x2C00, 0x3000, 0x3400, 0x3800, 0x3C00, 0x4000, 0x4400, 0x4800, or 0x4C00,
            // for the valid language-script-region tags.

            Assert.NotEqual(lcid, new CultureInfo(lcid).LCID);
        }
    }
}
