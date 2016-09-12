// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoMiscTests
    {
        public static IEnumerable<object[]> CultureInfo_TestData()
        {
            yield return new object[] { "en"    , 0x0009, "en-US", "eng", "ENU" };
            yield return new object[] { "ar"    , 0x0001, "ar-SA", "ara", "ARA" };
            yield return new object[] { "en-US" , 0x0409, "en-US", "eng", "ENU" };
            yield return new object[] { "ar-SA" , 0x0401, "ar-SA", "ara", "ARA" };
            yield return new object[] { "ja-JP" , 0x0411, "ja-JP", "jpn", "JPN" };
            yield return new object[] { "zh-CN" , 0x0804, "zh-CN", "zho", "CHS" };
            yield return new object[] { "en-GB" , 0x0809, "en-GB", "eng", "ENG" };
            yield return new object[] { "tr-TR" , 0x041f, "tr-TR", "tur", "TRK" };
        }

        [Theory]
        [MemberData(nameof(CultureInfo_TestData))]
        [ActiveIssue(11609, Xunit.PlatformID.AnyUnix)]
        public void LcidTest(string cultureName, int lcid, string specificCultureName, string threeLetterISOLanguageName, string threeLetterWindowsLanguageName)
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
        }

        [Fact]
        [ActiveIssue(11609, Xunit.PlatformID.AnyUnix)]
        public void InstalledUICultureTest()
        {
            // as we didn't change current UI culture, Installed UI culture should match current UI culture
            Assert.Equal(CultureInfo.CurrentUICulture, CultureInfo.InstalledUICulture);
        }

        [Fact]
        [ActiveIssue(11609, Xunit.PlatformID.AnyUnix)]
        public void GetCulturesTest()
        {
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
                Assert.True(ci.IsNeutralCulture || ci.Equals(CultureInfo.InvariantCulture), "Expected Neutral Cultures or invariant");

            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                Assert.False(ci.IsNeutralCulture, "Expected specific cultures only");
        }

        [Fact]
        [ActiveIssue(11609, Xunit.PlatformID.AnyUnix)]
        public void ClearCachedDataTest()
        {
            CultureInfo ci = CultureInfo.GetCultureInfo("ja-JP");
            Assert.True((object) ci == (object) CultureInfo.GetCultureInfo("ja-JP"), "Expected getting same object reference");
            ci.ClearCachedData();
            Assert.False((object) ci == (object) CultureInfo.GetCultureInfo("ja-JP"), "expected to get a new object reference");
        }

        [Fact]
        [ActiveIssue(11609, Xunit.PlatformID.AnyUnix)]
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
