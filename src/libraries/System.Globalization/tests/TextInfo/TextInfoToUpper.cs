// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoToUpper
    {
        private static readonly string [] s_cultureNames = new string[] { "", "en-US", "fr", "fr-FR" };

        // ToUpper_TestData_netcore has the data which is specific to netcore framework 
        public static IEnumerable<object[]> ToUpper_TestData_netcore()
        {
            foreach (string cultureName in s_cultureNames)
            {
                // DESERT SMALL LETTER LONG I has an upper case variant (but not on Windows 7).
                yield return new object[] { cultureName, "\U00010428", PlatformDetection.IsWindows7 ? "\U00010428" : "\U00010400" };
            }
        }

        // ToUpper_TestData_net46 has the data which is specific to the full framework 
        public static IEnumerable<object[]> ToUpper_TestData_net46()
        {
            foreach (string cultureName in s_cultureNames)
            {
                if (PlatformDetection.IsWindows7)
                {
                    // on Windows 7, Desktop framework is using its own sorting DLL and not calling the OS except with Invariant culture
                    yield return new object[] { cultureName, "\U00010428", cultureName == "" ? "\U00010428" : "\U00010400" };
                }
                else 
                {
                    yield return new object[] { cultureName, "\U00010428", "\U00010400" };
                }
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
            foreach (string cultureName in new string[] { "tr", "tr-TR", "az", "az-Latn-AZ" })
            {
                yield return new object[] { cultureName, "i", "\u0130" };
                yield return new object[] { cultureName, "\u0130", "\u0130" };
                yield return new object[] { cultureName, "\u0131", "I" };
                yield return new object[] { cultureName, "I", "I" };
                yield return new object[] { cultureName, "H\u0131\n\0Hi\u0009!", "HI\n\0H\u0130\t!" };
            }

            // ICU has special tailoring for the en-US-POSIX locale which treats "i" and "I" as different letters
            // instead of two letters with a case difference during collation.  Make sure this doesn't confuse our
            // casing implementation, which uses collation to understand if we need to do Turkish casing or not.
            if (!PlatformDetection.IsWindows)
            {
                yield return new object[] { "en-US-POSIX", "i", "I" };
            }
        }

        public void TestToUpper(string name, string str, string expected)
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void ToUpper_netcore(string name, string str, string expected)
        {
            TestToUpper(name, str, expected);
        }

        [Theory]
        [MemberData(nameof(ToUpper_TestData_net46))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Netcoreapp | TargetFrameworkMonikers.Uap)]
        public void ToUpper_net46(string name, string str, string expected)
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
    }
}
