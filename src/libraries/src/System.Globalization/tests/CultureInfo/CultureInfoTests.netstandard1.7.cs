// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoMiscTests
    {
        public static IEnumerable<object[]> CultureInfo_TestData()
        {
            yield return new object[] { "en"    , 0x0009, "en-US", "eng", "ENU", "en"         , "en-US" };
            yield return new object[] { "ar"    , 0x0001, "ar-SA", "ara", "ARA", "ar"         , "en-US" };
            yield return new object[] { "en-US" , 0x0409, "en-US", "eng", "ENU", "en-US"      , "en-US" };
            yield return new object[] { "ar-SA" , 0x0401, "ar-SA", "ara", "ARA", "ar-SA"      , "en-US" };
            yield return new object[] { "ja-JP" , 0x0411, "ja-JP", "jpn", "JPN", "ja-JP"      , "ja-JP" };
            yield return new object[] { "zh-CN" , 0x0804, "zh-CN", "zho", "CHS", "zh-Hans-CN" , "zh-CN" };
            yield return new object[] { "en-GB" , 0x0809, "en-GB", "eng", "ENG", "en-GB"      , "en-GB" };
            yield return new object[] { "tr-TR" , 0x041f, "tr-TR", "tur", "TRK", "tr-TR"      , "tr-TR" };
        }
        
        [Theory]
        [MemberData(nameof(CultureInfo_TestData))]
        public void LcidTest(string cultureName, int lcid, string specificCultureName, string threeLetterISOLanguageName, string threeLetterWindowsLanguageName, string alternativeCultureName, string consoleUICultureName)
        {
            CultureInfo ci = new CultureInfo(lcid);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);
            Assert.True(ci.UseUserOverride, "UseUserOverride for lcid created culture expected to be true");
            Assert.False(ci.IsReadOnly, "IsReadOnly for lcid created culture expected to be false");
            Assert.Equal(threeLetterISOLanguageName, ci.ThreeLetterISOLanguageName);
            Assert.Equal(threeLetterWindowsLanguageName, ci.ThreeLetterWindowsLanguageName);
            
            ci = new CultureInfo(cultureName);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);
            Assert.True(ci.UseUserOverride, "UseUserOverride for named created culture expected to be true");
            Assert.False(ci.IsReadOnly, "IsReadOnly for named created culture expected to be false");
            
            ci = new CultureInfo(lcid, false);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);
            Assert.False(ci.UseUserOverride, "UseUserOverride with false user override culture expected to be false");
            Assert.False(ci.IsReadOnly, "IsReadOnly with false user override culture expected to be false");

            ci = CultureInfo.GetCultureInfo(lcid);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);
            Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and lcid expected to be false");
            Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and lcid expected to be true");
            
            ci = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);
            Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and name expected to be false");
            Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and name expected to be true");

            ci = CultureInfo.GetCultureInfo(cultureName, "");
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(lcid, ci.LCID);
            Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and sort name expected to be false");
            Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and sort name expected to be true");
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo, ci.TextInfo);
            Assert.Equal(CultureInfo.InvariantCulture.CompareInfo, ci.CompareInfo);

            ci = CultureInfo.CreateSpecificCulture(cultureName);
            Assert.Equal(specificCultureName, ci.Name);

            ci = CultureInfo.GetCultureInfoByIetfLanguageTag(cultureName);            
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(ci.Name, ci.IetfLanguageTag);
            Assert.Equal(lcid, ci.KeyboardLayoutId);
            
            Assert.Equal(consoleUICultureName, ci.GetConsoleFallbackUICulture().Name);
        }

        [Fact]
        public void InstalledUICultureTest()
        {
            // as we didn't change current UI culture, Installed UI culture should match current UI culture
            Assert.Equal(CultureInfo.CurrentUICulture, CultureInfo.InstalledUICulture);
        }
        
        [Theory]
        [MemberData(nameof(CultureInfo_TestData))]
        public void GetCulturesTest(string cultureName, int lcid, string specificCultureName, string threeLetterISOLanguageName, string threeLetterWindowsLanguageName, string alternativeCultureName, string consoleUICultureName)
        {
            bool found = false;
            Assert.All(CultureInfo.GetCultures(CultureTypes.NeutralCultures), 
                       c => Assert.True( (c.IsNeutralCulture && ((c.CultureTypes & CultureTypes.NeutralCultures) != 0)) || c.Equals(CultureInfo.InvariantCulture)));
            found = CultureInfo.GetCultures(CultureTypes.NeutralCultures).Any(c => c.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase) || 
                                                                                   c.Name.Equals(alternativeCultureName, StringComparison.OrdinalIgnoreCase));
            Assert.All(CultureInfo.GetCultures(CultureTypes.SpecificCultures), c => Assert.True(!c.IsNeutralCulture && ((c.CultureTypes & CultureTypes.SpecificCultures) != 0)));
            if (!found)
            {
                found = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Any(c => c.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase) || 
                                                                                       c.Name.Equals(alternativeCultureName, StringComparison.OrdinalIgnoreCase));
            }
            
            Assert.True(found, $"Expected to find the culture {cultureName} in the enumerated list");
        }

        [Fact]
        public void ClearCachedDataTest()
        {
            CultureInfo ci = CultureInfo.GetCultureInfo("ja-JP");
            Assert.True((object) ci == (object) CultureInfo.GetCultureInfo("ja-JP"), "Expected getting same object reference");
            ci.ClearCachedData();
            Assert.False((object) ci == (object) CultureInfo.GetCultureInfo("ja-JP"), "expected to get a new object reference");
        }

        [Fact]
        public void CultureNotFoundExceptionTest()
        {
            Assert.Throws<CultureNotFoundException>("name", () => new CultureInfo("!@#$%^&*()"));
            Assert.Throws<CultureNotFoundException>("name", () => new CultureInfo("This is invalid culture"));
            Assert.Throws<CultureNotFoundException>("name", () => new CultureInfo("longCulture" + new string('a', 100)));
            Assert.Throws<CultureNotFoundException>("culture", () => new CultureInfo(0x1000));

            CultureNotFoundException e = Assert.Throws<CultureNotFoundException>("name", () => new CultureInfo("This is invalid culture"));
            Assert.Equal("This is invalid culture", e.InvalidCultureName);

            e = Assert.Throws<CultureNotFoundException>("culture", () => new CultureInfo(0x1000));
            Assert.Equal(0x1000, e.InvalidCultureId);
        }
    }
}
