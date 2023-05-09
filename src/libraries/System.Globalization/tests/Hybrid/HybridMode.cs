// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class HybridModeTests
    {
        public static IEnumerable<object[]> EnglishName_TestData()
        {
            yield return new object[] { "en-US", "English (United States)" };
            yield return new object[] { "fr-FR", "French (France)" };
        }

        public static IEnumerable<object[]> NativeName_TestData()
        {
            yield return new object[] { "en-US", "English (United States)" };
            yield return new object[] { "fr-FR", "français (France)" };
            yield return new object[] { "en-CA", "English (Canada)" };
        }

        [Theory]
        [MemberData(nameof(EnglishName_TestData))]
        public void TestEnglishName(string cultureName, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(cultureName);
            Assert.Equal(expected, myTestCulture.EnglishName);
        }

        [Theory]
        [MemberData(nameof(NativeName_TestData))]
        public void TestNativeName(string cultureName, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(cultureName);
            Assert.Equal(expected, myTestCulture.NativeName);
        }

        [Theory]
        [InlineData("de-DE", "de")]
        [InlineData("en-US", "en")]
        public void TwoLetterISOLanguageName(string name, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TwoLetterISOLanguageName);
        }
    }
}
