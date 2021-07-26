// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoEnglishName
    {
        public static IEnumerable<object[]> EnglishName_TestData()
        {
            yield return new object[] { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.EnglishName };

            // Android has its own ICU, which doesn't 100% map to UsingLimitedCultures
            if (PlatformDetection.IsNotUsingLimitedCultures || PlatformDetection.IsAndroid)
            {
                yield return new object[] { "en-US", "English (United States)" };
                yield return new object[] { "fr-FR", "French (France)" };
                yield return new object[] { "zh-Hant", "Chinese (Traditional)" };
                yield return new object[] { "zh-Hans", "Chinese (Simplified)" };
                yield return new object[] { "uz-Arab", "Uzbek (Arabic)" };
                yield return new object[] { "uz-Cyrl", "Uzbek (Cyrillic)" };
            }
            else
            {
                // Mobile / Browser ICU doesn't contain CultureInfo.EnglishName
                yield return new object[] { "en-US", "en (US)" };
                yield return new object[] { "fr-FR", "fr (FR)" };
            }
        }

        [Theory]
        [MemberData(nameof(EnglishName_TestData))]
        public void EnglishName(string name, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(name);
            Assert.Equal(expected, myTestCulture.EnglishName);
        }
    }
}
