// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoToLower
    {
        private static readonly string [] s_cultureNames = new string[] { "", "en-US", "fr", "fr-FR" };
        
        // ToLower_TestData_net46 has the data which is specific to the full framework 
        public static IEnumerable<object[]> ToLower_TestData_net46()
        {
            foreach (string cultureName in s_cultureNames)
            {
                if (PlatformDetection.IsWindows7)
                {
                    // on Windows 7, Desktop framework is using its own sorting DLL and not calling the OS except with Invariant culture
                    yield return new object[] { cultureName, "\U00010400", cultureName == "" ? "\U00010400" : "\U00010428" };
                }
                else 
                {
                    yield return new object[] { cultureName, "\U00010400", "\U00010428" };
                }
            }
        }

        // ToLower_TestData_netcore has the data which is specific to netcore framework 
        public static IEnumerable<object[]> ToLower_TestData_netcore()
        {
            foreach (string cultureName in s_cultureNames)
            {
                // DESERT CAPITAL LETTER LONG I has a lower case variant (but not on Windows 7).
                yield return new object[] { cultureName, "\U00010400", PlatformDetection.IsWindows7 ? "\U00010400" : "\U00010428" };
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

            foreach (string cultureName in new string[] { "tr", "tr-TR", "az", "az-Latn-AZ" })
            {
                yield return new object[] { cultureName, "\u0130", "i" };
                yield return new object[] { cultureName, "i", "i" };
                yield return new object[] { cultureName, "I", "\u0131" };
                yield return new object[] { cultureName, "HI!", "h\u0131!" };
                yield return new object[] { cultureName, "HI\n\0H\u0130\t!", "h\u0131\n\0hi\u0009!" };
            }

            // ICU has special tailoring for the en-US-POSIX locale which treats "i" and "I" as different letters
            // instead of two letters with a case difference during collation.  Make sure this doesn't confuse our
            // casing implementation, which uses collation to understand if we need to do Turkish casing or not.
            if (!PlatformDetection.IsWindows)
            {
                yield return new object[] { "en-US-POSIX", "I", "i" };
            }
        }

        public void TestToLower(string name, string str, string expected)
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void ToLower_Netcore(string name, string str, string expected)
        {
            TestToLower(name, str, expected);
        }

        [Theory]
        [MemberData(nameof(ToLower_TestData_net46))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Netcoreapp | TargetFrameworkMonikers.Uap)]
        public void ToLower_net46(string name, string str, string expected)
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
    }
}
