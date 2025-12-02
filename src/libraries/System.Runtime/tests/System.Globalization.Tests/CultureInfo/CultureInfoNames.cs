// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Tests;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoNames
    {
        // Android has its own ICU, which doesn't 100% map to UsingLimitedCultures
        // Browser uses JS to get the NativeName that is missing in ICU (in the singlethreaded runtime only)
        private static bool SupportFullIcuResources =>
            !PlatformDetection.IsWasi && !PlatformDetection.IsAndroid && PlatformDetection.IsIcuGlobalization && !PlatformDetection.IsWasmThreadingSupported;
        
        public static IEnumerable<object[]> SupportedCultures_TestData()
        {
            // Browser does not support all ICU locales but it uses JS to get the correct native name
            if (!PlatformDetection.IsBrowser)
            {
                yield return new object[] { "aa", "aa", "Afar", "Afar" };
                yield return new object[] { "aa-ER", "aa-ER", "Afar (Eritrea)", "Afar (Eritrea)" };
            }
            yield return new object[] { "en", "en", "English", "English" };
            yield return new object[] { "en", "fr", "English", "anglais" };
            yield return new object[] { "en-US", "en-US", "English (United States)", "English (United States)" };
            yield return new object[] { "en-US", "fr-FR", "English (United States)", "anglais (\u00C9tats-Unis)" };
            yield return new object[] { "en-US", "de-DE", "English (United States)", "Englisch (Vereinigte Staaten)" };
            yield return new object[] { "", "en-US", "Invariant Language (Invariant Country)", "Invariant Language (Invariant Country)" };
            yield return new object[] { "", "fr-FR", "Invariant Language (Invariant Country)", "Invariant Language (Invariant Country)" };
            yield return new object[] { "", "", "Invariant Language (Invariant Country)", "Invariant Language (Invariant Country)" };
        }

        [ConditionalTheory(nameof(SupportFullIcuResources))]
        [MemberData(nameof(SupportedCultures_TestData))]
        public void TestDisplayName(string cultureName, string uiCultureName, string nativeName, string displayName)
        {
            using (new ThreadCultureChange(null, CultureInfo.GetCultureInfo(uiCultureName)))
            {
                CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
                Assert.Equal(nativeName, ci.NativeName);
                Assert.Equal(displayName, ci.DisplayName);
            }
        }

        [ConditionalFact(nameof(SupportFullIcuResources))]
        public void TestDisplayNameWithSettingUICultureMultipleTime()
        {
            using (new ThreadCultureChange(null, CultureInfo.GetCultureInfo("en-US")))
            {
                CultureInfo ci = new CultureInfo("en-US");
                Assert.Equal("English (United States)", ci.DisplayName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.Equal("anglais (\u00C9tats-Unis)", ci.DisplayName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
                Assert.Equal("Englisch (Vereinigte Staaten)", ci.DisplayName);
            }
        }
    }
}
