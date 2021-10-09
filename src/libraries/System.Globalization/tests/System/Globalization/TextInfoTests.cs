// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoMiscTests
    {
        public static IEnumerable<object[]> TextInfo_TestData()
        {
            yield return new object[] { "", 0x7f, 0x4e4, 0x25, 0x2710, 0x1b5, false };
            yield return new object[] { "en-US", 0x409, 0x4e4, 0x25, 0x2710, 0x1b5, false };
            yield return new object[] { "ja-JP", 0x411, 0x3a4, 0x4f42, 0x2711, 0x3a4, false };
            yield return new object[] { "zh-CN", 0x804, 0x3a8, 0x1f4, 0x2718, 0x3a8, false };
            yield return new object[] { "ar-SA", 0x401, 0x4e8, 0x4fc4, 0x2714, 0x2d0, true };
            yield return new object[] { "ko-KR", 0x412, 0x3b5, 0x5161, 0x2713, 0x3b5, false };
            yield return new object[] { "he-IL", 0x40d, 0x4e7, 0x1f4, 0x2715, 0x35e, true };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // OS changes
        [MemberData(nameof(TextInfo_TestData))]
        public void MiscTest(string cultureName, int lcid, int ansiCodePage, int ebcdiCCodePage, int macCodePage, int oemCodePage, bool isRightToLeft)
        {
            TextInfo ti = CultureInfo.GetCultureInfo(cultureName).TextInfo;
            Assert.Equal(lcid, ti.LCID);
            Assert.Equal(ansiCodePage, ti.ANSICodePage);
            Assert.Equal(ebcdiCCodePage, ti.EBCDICCodePage);
            Assert.Equal(macCodePage, ti.MacCodePage);
            Assert.Equal(oemCodePage, ti.OEMCodePage);
            Assert.Equal(isRightToLeft, ti.IsRightToLeft);
        }

        [Fact]
        public void ReadOnlyTest()
        {
            TextInfo ti = CultureInfo.GetCultureInfo("en-US").TextInfo;
            Assert.True(ti.IsReadOnly, "IsReadOnly should be true with cached TextInfo object");

            ti = (TextInfo) ti.Clone();
            Assert.False(ti.IsReadOnly, "IsReadOnly should be false with cloned TextInfo object");

            ti = TextInfo.ReadOnly(ti);
            Assert.True(ti.IsReadOnly, "IsReadOnly should be true with created read-nly TextInfo object");
        }

        [Fact]
        public void ToTitleCaseTest()
        {
            TextInfo ti = CultureInfo.GetCultureInfo("en-US").TextInfo;
            Assert.Equal("A Tale Of Two Cities", ti.ToTitleCase("a tale of two cities"));
            Assert.Equal("Growl To The Rescue", ti.ToTitleCase("gROWL to the rescue"));
            Assert.Equal("Inside The US Government", ti.ToTitleCase("inside the US government"));
            Assert.Equal("Sports And MLB Baseball", ti.ToTitleCase("sports and MLB baseball"));
            Assert.Equal("The Return Of Sherlock Holmes", ti.ToTitleCase("The Return of Sherlock Holmes"));
            Assert.Equal("UNICEF And Children", ti.ToTitleCase("UNICEF and children"));

            AssertExtensions.Throws<ArgumentNullException>("str", () => ti.ToTitleCase(null));
        }

        public static IEnumerable<object[]> DutchTitleCaseInfo_TestData()
        {
            yield return new object[] { "nl-NL", "IJ IJ IJ IJ", "ij iJ Ij IJ" };
            yield return new object[] { "nl-be", "IJzeren Eigenschappen", "ijzeren eigenschappen" };
            yield return new object[] { "NL-NL", "Lake IJssel", "lake iJssel" };
            yield return new object[] { "NL-BE", "Boba N' IJango Fett PEW PEW", "Boba n' Ijango fett PEW PEW" };
            yield return new object[] { "en-us", "Ijill And Ijack", "ijill and ijack" };
            yield return new object[] { "de-DE", "Ij Ij IJ Ij", "ij ij IJ ij" };
            yield return new object[] { "he-il", "Ijon't Know What Will Happen.", "Ijon't know what Will happen." };
        }

        [Theory]
        [MemberData(nameof(DutchTitleCaseInfo_TestData))]
        public void ToTitleCaseDutchTest(string cultureName, string expected, string actual)
        {
            TextInfo ti = CultureInfo.GetCultureInfo(cultureName).TextInfo;
            Assert.Equal(expected, ti.ToTitleCase(actual));
        }

        public static IEnumerable<object[]> CultureName_TestData()
        {
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, "" };
            yield return new object[] { new CultureInfo("").TextInfo, "" };
            yield return new object[] { new CultureInfo("en-US").TextInfo, "en-US" };
            yield return new object[] { new CultureInfo("fr-FR").TextInfo, "fr-FR" };
            yield return new object[] { new CultureInfo("EN-us").TextInfo, "en-US" };
            yield return new object[] { new CultureInfo("FR-fr").TextInfo, "fr-FR" };
        }

        [Theory]
        [MemberData(nameof(CultureName_TestData))]
        public void CultureName(TextInfo textInfo, string expected)
        {
            Assert.Equal(expected, textInfo.CultureName);
        }

        public static IEnumerable<object[]> IsReadOnly_TestData()
        {
            yield return new object[] { CultureInfo.ReadOnly(new CultureInfo("en-US")).TextInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, true };
            yield return new object[] { new CultureInfo("").TextInfo, false };
            yield return new object[] { new CultureInfo("en-US").TextInfo, false };
            yield return new object[] { new CultureInfo("fr-FR").TextInfo, false };
        }

        [Theory]
        [MemberData(nameof(IsReadOnly_TestData))]
        public void IsReadOnly(TextInfo textInfo, bool expected)
        {
            Assert.Equal(expected, textInfo.IsReadOnly);
        }

        [Theory]
        [InlineData("en-US", false)]
        [InlineData("ar", true)]
        public void IsRightToLeft(string name, bool expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TextInfo.IsRightToLeft);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData("ar-SA", ";")]
        [InlineData("as-IN", ",")]
        [InlineData("ba-RU", ";")]
        [InlineData("bs-cyrl-BA", ";")]
        [InlineData("de-DE", ";")]
        [InlineData("dv-MV", "\u060C")]
        [InlineData("en-GB", ",")]
        [InlineData("en-US", ",")]
        [InlineData("es-ES", ";")]
        [InlineData("es-MX", ",")]
        [InlineData("fa-IR", "\u061B")]
        [InlineData("fr-FR", ";")]
        [InlineData("hr-HR", ";")]
        [InlineData("it-IT", ";")]
        [InlineData("ko-KR", ",")]
        [InlineData("ku-arab-iq", "\u061B")]
        [InlineData("nl-NL", ";")]
        [InlineData("pl-pl", ";")]
        [InlineData("pt-PT", ";")]
        [InlineData("ru-RU", ";")]
        [InlineData("sv-SE", ";")]
        [InlineData("th-TH", ",")]
        [InlineData("ja-jp", ",")]
        [InlineData("zh-CN", ",")]
        [InlineData("", ",")]
        public void ListSeparatorTest(string cultureName, string separator)
        {
            try
            {
                Assert.Equal(separator, CultureInfo.GetCultureInfo(cultureName).TextInfo.ListSeparator);
            }
            catch (CultureNotFoundException)
            {
                // Ignore the cultures we cannot create on downlevel versions.
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("abcdef")]
        public void ListSeparator_Set(string newListSeparator)
        {
            TextInfo textInfo = new CultureInfo("en-US").TextInfo;
            textInfo.ListSeparator = newListSeparator;
            Assert.Equal(newListSeparator, textInfo.ListSeparator);
        }

        [Fact]
        public void ListSeparator_Set_Invalid()
        {
            Assert.Throws<InvalidOperationException>(() => CultureInfo.InvariantCulture.TextInfo.ListSeparator = "");
            AssertExtensions.Throws<ArgumentNullException>("value", () => new CultureInfo("en-US").TextInfo.ListSeparator = null);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, CultureInfo.InvariantCulture.TextInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, new CultureInfo("").TextInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, new CultureInfo("en-US"), false };

            yield return new object[] { new CultureInfo("en-US").TextInfo, new CultureInfo("en-US").TextInfo, true };
            yield return new object[] { new CultureInfo("en-US").TextInfo, new CultureInfo("fr-FR").TextInfo, false };

            yield return new object[] { new CultureInfo("en-US").TextInfo, null, false };
            yield return new object[] { new CultureInfo("en-US").TextInfo, new object(), false };
            yield return new object[] { new CultureInfo("en-US").TextInfo, 123, false };
            yield return new object[] { new CultureInfo("en-US").TextInfo, "en-US", false };

        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(TextInfo textInfo, object obj, bool expected)
        {
            Assert.Equal(expected, textInfo.Equals(obj));
            if (obj is TextInfo)
            {
                Assert.Equal(expected, textInfo.GetHashCode().Equals(obj.GetHashCode()));
            }
        }

        private static readonly string [] s_cultureNames = new string[] { "en-US", "fr", "fr-FR" };

        // ToLower_TestData_netcore has the data which is specific to netcore framework
        public static IEnumerable<object[]> ToLower_TestData_netcore()
        {
            foreach (string cultureName in s_cultureNames)
            {
                // DESERT CAPITAL LETTER LONG I has a lower case variant (but not on Windows 7).
                yield return new object[] { cultureName, "\U00010400", PlatformDetection.IsWindows7 ? "\U00010400" : "\U00010428" };
            }

            if (!PlatformDetection.IsNlsGlobalization)
            {
                yield return new object[] { "", "\U00010400", PlatformDetection.IsWindows7 ? "\U00010400" : "\U00010428" };
            }
        }

        public static IEnumerable<string> GetTestLocales()
        {
            yield return "tr";
            yield return "tr-TR";

            if (PlatformDetection.IsNotUsingLimitedCultures)
            {
                // Mobile / Browser ICU doesn't contain these locales
                yield return "az";
                yield return "az-Latn-AZ";
            }
        }

        public static IEnumerable<object[]> ToLower_TestData()
        {
            foreach (string cultureName in s_cultureNames)
            {
                yield return new object[] { cultureName, "", "" };

                yield return new object[] { cultureName, "A", "a" };
                yield return new object[] { cultureName, "a", "a" };
                yield return new object[] { cultureName, "ABC", "abc" };
                yield return new object[] { cultureName, "abc", "abc" };

                yield return new object[] { cultureName, "1", "1" };
                yield return new object[] { cultureName, "123", "123" };
                yield return new object[] { cultureName, "!", "!" };

                yield return new object[] { cultureName, "HELLOWOR!LD123", "hellowor!ld123" };
                yield return new object[] { cultureName, "HelloWor!ld123", "hellowor!ld123" };
                yield return new object[] { cultureName, "Hello\n\0World\u0009!", "hello\n\0world\t!" };

                yield return new object[] { cultureName, "THIS IS A LONGER TEST CASE", "this is a longer test case" };
                yield return new object[] { cultureName, "this Is A LONGER mIXEd casE test case", "this is a longer mixed case test case" };

                yield return new object[] { cultureName, "THIS \t hAs \t SOMe \t tabs", "this \t has \t some \t tabs" };
                yield return new object[] { cultureName, "EMBEDDED\0NuLL\0Byte\0", "embedded\0null\0byte\0" };

                // LATIN CAPITAL LETTER O WITH ACUTE, which has a lower case variant.
                yield return new object[] { cultureName, "\u00D3", "\u00F3" };

                // SNOWMAN, which does not have a lower case variant.
                yield return new object[] { cultureName, "\u2603", "\u2603" };

                // RAINBOW (outside the BMP and does not case)
                yield return new object[] { cultureName, "\U0001F308", "\U0001F308" };

                // Unicode defines some codepoints which expand into multiple codepoints
                // when cased (see SpecialCasing.txt from UNIDATA for some examples). We have never done
                // these sorts of expansions, since it would cause string lengths to change when cased,
                // which is non-intuitive. In addition, there are some context sensitive mappings which
                // we also don't preform.
                // Greek Capital Letter Sigma (does not to case to U+03C2 with "final sigma" rule).
                yield return new object[] { cultureName, "\u03A3", "\u03C3" };
            }

            foreach (string cultureName in GetTestLocales())
            {
                // Android has its own ICU, which doesn't work well with tr
                if (!PlatformDetection.IsAndroid)
                {
                    yield return new object[] { cultureName, "I", "\u0131" };
                    yield return new object[] { cultureName, "HI!", "h\u0131!" };
                    yield return new object[] { cultureName, "HI\n\0H\u0130\t!", "h\u0131\n\0hi\u0009!" };
                }
                yield return new object[] { cultureName, "\u0130", "i" };
                yield return new object[] { cultureName, "i", "i" };
                
            }

            // ICU has special tailoring for the en-US-POSIX locale which treats "i" and "I" as different letters
            // instead of two letters with a case difference during collation.  Make sure this doesn't confuse our
            // casing implementation, which uses collation to understand if we need to do Turkish casing or not.
            if (!PlatformDetection.IsWindows && PlatformDetection.IsNotBrowser)
            {
                yield return new object[] { "en-US-POSIX", "I", "i" };
            }
        }

        private static void TestToLower(string name, string str, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TextInfo.ToLower(str));
            if (str.Length == 1)
            {
                Assert.Equal(expected[0], new CultureInfo(name).TextInfo.ToLower(str[0]));
            }
        }

        [Theory]
        [MemberData(nameof(ToLower_TestData))]
        public void ToLower(string name, string str, string expected)
        {
            TestToLower(name, str, expected);
        }

        [Theory]
        [MemberData(nameof(ToLower_TestData_netcore))]
        public void ToLower_Netcore(string name, string str, string expected)
        {
            TestToLower(name, str, expected);
        }

        [Fact]
        public void ToLower_InvalidSurrogates()
        {
            // Invalid UTF-16 in a string (mismatched surrogate pairs) should be unchanged.
            foreach (string cultureName in new string[] { "", "en-US", "fr" })
            {
                ToLower(cultureName, "BE CAREFUL, \uD83C\uD83C, THIS ONE IS TRICKY", "be careful, \uD83C\uD83C, this one is tricky");
                ToLower(cultureName, "BE CAREFUL, \uDF08\uD83C, THIS ONE IS TRICKY", "be careful, \uDF08\uD83C, this one is tricky");
                ToLower(cultureName, "BE CAREFUL, \uDF08\uDF08, THIS ONE IS TRICKY", "be careful, \uDF08\uDF08, this one is tricky");
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("en-US")]
        [InlineData("fr")]
        public void ToLower_Null_ThrowsArgumentNullException(string cultureName)
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => new CultureInfo(cultureName).TextInfo.ToLower(null));
        }

        // ToUpper_TestData_netcore has the data which is specific to netcore framework
        public static IEnumerable<object[]> ToUpper_TestData_netcore()
        {
            foreach (string cultureName in s_cultureNames)
            {
                // DESERT SMALL LETTER LONG I has an upper case variant (but not on Windows 7).
                yield return new object[] { cultureName, "\U00010428", PlatformDetection.IsWindows7 ? "\U00010428" : "\U00010400" };
            }
        }

        public static IEnumerable<object[]> ToUpper_TestData()
        {
            foreach (string cultureName in s_cultureNames)
            {
                yield return new object[] { cultureName, "", "" };

                yield return new object[] { cultureName, "a", "A" };
                yield return new object[] { cultureName, "abc", "ABC" };
                yield return new object[] { cultureName, "A", "A" };
                yield return new object[] { cultureName, "ABC", "ABC" };

                yield return new object[] { cultureName, "1", "1" };
                yield return new object[] { cultureName, "123", "123" };
                yield return new object[] { cultureName, "!", "!" };

                yield return new object[] { cultureName, "HelloWor!ld123", "HELLOWOR!LD123" };
                yield return new object[] { cultureName, "HELLOWOR!LD123", "HELLOWOR!LD123" };
                yield return new object[] { cultureName, "Hello\n\0World\u0009!", "HELLO\n\0WORLD\t!" };

                yield return new object[] { cultureName, "this is a longer test case", "THIS IS A LONGER TEST CASE" };
                yield return new object[] { cultureName, "this Is A LONGER mIXEd casE test case", "THIS IS A LONGER MIXED CASE TEST CASE" };
                yield return new object[] { cultureName, "this \t HaS \t somE \t TABS", "THIS \t HAS \t SOME \t TABS" };

                yield return new object[] { cultureName, "embedded\0NuLL\0Byte\0", "EMBEDDED\0NULL\0BYTE\0" };

                // LATIN SMALL LETTER O WITH ACUTE, which has an upper case variant.
                yield return new object[] { cultureName, "\u00F3", "\u00D3" };

                // SNOWMAN, which does not have an upper case variant.
                yield return new object[] { cultureName, "\u2603", "\u2603" };

                // RAINBOW (outside the BMP and does not case)
                yield return new object[] { cultureName, "\U0001F308", "\U0001F308" };

                // Unicode defines some codepoints which expand into multiple codepoints
                // when cased (see SpecialCasing.txt from UNIDATA for some examples). We have never done
                // these sorts of expansions, since it would cause string lengths to change when cased,
                // which is non-intuitive. In addition, there are some context sensitive mappings which
                // we also don't preform.
                // es-zed does not case to SS when uppercased.
                yield return new object[] { cultureName, "\u00DF", "\u00DF" };

                // Ligatures do not expand when cased.
                yield return new object[] { cultureName, "\uFB00", "\uFB00" };

                // Precomposed character with no uppercase variant, we don't want to "decompose" this
                // as part of casing.
                yield return new object[] { cultureName, "\u0149", "\u0149" };
            }

            // Turkish i
            foreach (string cultureName in GetTestLocales())
            {
                // Android has its own ICU, which doesn't work well with tr
                if (!PlatformDetection.IsAndroid)
                {
                    yield return new object[] { cultureName, "i", "\u0130" };
                    yield return new object[] { cultureName, "H\u0131\n\0Hi\u0009!", "HI\n\0H\u0130\t!" };
                }
                yield return new object[] { cultureName, "\u0130", "\u0130" };
                yield return new object[] { cultureName, "\u0131", "I" };
                yield return new object[] { cultureName, "I", "I" };
            }

            // ICU has special tailoring for the en-US-POSIX locale which treats "i" and "I" as different letters
            // instead of two letters with a case difference during collation.  Make sure this doesn't confuse our
            // casing implementation, which uses collation to understand if we need to do Turkish casing or not.
            if (!PlatformDetection.IsWindows && PlatformDetection.IsNotBrowser)
            {
                yield return new object[] { "en-US-POSIX", "i", "I" };
            }
        }

        private static void TestToUpper(string name, string str, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TextInfo.ToUpper(str));
            if (str.Length == 1)
            {
                Assert.Equal(expected[0], new CultureInfo(name).TextInfo.ToUpper(str[0]));
            }
        }

        [Theory]
        [MemberData(nameof(ToUpper_TestData))]
        public void ToUpper(string name, string str, string expected)
        {
            TestToUpper(name, str, expected);
        }

        [Theory]
        [MemberData(nameof(ToUpper_TestData_netcore))]
        public void ToUpper_netcore(string name, string str, string expected)
        {
            TestToUpper(name, str, expected);
        }

        [Fact]
        public void ToUpper_InvalidSurrogates()
        {
            // Invalid UTF-16 in a string (mismatched surrogate pairs) should be unchanged.
            foreach (string cultureName in new string[] { "", "en-US", "fr"})
            {
                ToUpper(cultureName, "be careful, \uD83C\uD83C, this one is tricky", "BE CAREFUL, \uD83C\uD83C, THIS ONE IS TRICKY");
                ToUpper(cultureName, "be careful, \uDF08\uD83C, this one is tricky", "BE CAREFUL, \uDF08\uD83C, THIS ONE IS TRICKY");
                ToUpper(cultureName, "be careful, \uDF08\uDF08, this one is tricky", "BE CAREFUL, \uDF08\uDF08, THIS ONE IS TRICKY");
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("en-US")]
        [InlineData("fr")]
        public void ToUpper_Null_ThrowsArgumentNullException(string cultureName)
        {
            AssertExtensions.Throws<ArgumentNullException>("str", () => new CultureInfo(cultureName).TextInfo.ToUpper(null));
        }

        [Theory]
        [InlineData("en-US", "TextInfo - en-US")]
        [InlineData("fr-FR", "TextInfo - fr-FR")]
        [InlineData("", "TextInfo - ")]
        public void ToStringTest(string name, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TextInfo.ToString());
        }
    }
}
