// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoEnglishName
    {
        // Android has its own ICU, which doesn't 100% map to UsingLimitedCultures
        // Browser uses JS to get the NativeName that is missing in ICU (in the singlethreaded runtime only)
        public static bool SupportFullGlobalizationData =>
            (!PlatformDetection.IsWasi || PlatformDetection.IsHybridGlobalizationOnApplePlatform) && !PlatformDetection.IsWasmThreadingSupported;

        public static IEnumerable<object[]> EnglishName_TestData()
        {
            yield return new object[] { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.EnglishName };

            if (SupportFullGlobalizationData)
            {
                yield return new object[] { "en-US", "English (United States)" };
                yield return new object[] { "fr-FR", "French (France)" };
                yield return new object[] { "uz-Cyrl", "Uzbek (Cyrillic)" };
            }
            else
            {
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

        [ConditionalFact(nameof(SupportFullGlobalizationData))]
        public void ChineseNeutralEnglishName()
        {
            CultureInfo ci = new CultureInfo("zh-Hans");
            Assert.True(ci.EnglishName == "Chinese (Simplified)" || ci.EnglishName == "Chinese, Simplified" || ci.EnglishName == "Simplified Chinese",
                        $"'{ci.EnglishName}' not equal to `Chinese (Simplified)` nor `Chinese, Simplified` nor `Simplified Chinese`");

            ci = new CultureInfo("zh-HanT");
            Assert.True(ci.EnglishName == "Chinese (Traditional)" || ci.EnglishName == "Chinese, Traditional" || ci.EnglishName == "Traditional Chinese",
                        $"'{ci.EnglishName}' not equal to `Chinese (Traditional)` nor `Chinese, Traditional` nor `Traditional Chinese`");
        }
    }
}
