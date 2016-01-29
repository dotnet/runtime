// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

using Xunit;

namespace System.Collections.Tests
{
    public static class CaseInsensitiveComparerTests
    {
        [Theory]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData("hello", null, 1)]
        [InlineData(null, "hello", -1)]
        [InlineData(5, 5, 0)]
        [InlineData(10, 5, 1)]
        [InlineData(5, 10, -1)]
        [InlineData(5, null, 1)]
        [InlineData(null, 5, -1)]
        [InlineData(null, null, 0)]
        public static void TestCtor_Empty_Compare(object a, object b, int expected)
        {
            CaseInsensitiveComparer comparer = new CaseInsensitiveComparer();
            Assert.Equal(expected, Helpers.NormalizeCompare(comparer.Compare(a, b)));
        }

        [Theory]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData("hello", null, 1)]
        [InlineData(null, "hello", -1)]
        [InlineData(5, 5, 0)]
        [InlineData(10, 5, 1)]
        [InlineData(5, 10, -1)]
        [InlineData(5, null, 1)]
        [InlineData(null, 5, -1)]
        [InlineData(null, null, 0)]
        public static void TestCtor_CultureInfo_Compare(object a, object b, int expected)
        {
            var cultureNames = new string[]
            {
                "cs-CZ","da-DK","de-DE","el-GR","en-US",
                "es-ES","fi-FI","fr-FR","hu-HU","it-IT",
                "ja-JP","ko-KR","nb-NO","nl-NL","pl-PL",
                "pt-BR","pt-PT","ru-RU","sv-SE","tr-TR",
                "zh-CN","zh-HK","zh-TW"
            };

            foreach (string cultureName in cultureNames)
            {
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }

                var comparer = new CaseInsensitiveComparer(culture);
                Assert.Equal(expected, Helpers.NormalizeCompare(comparer.Compare(a, b)));
            }
        }

        [Fact]
        public static void TestCtor_CultureInfo_Compare_TurkishI()
        {
            var cultureNames = new string[]
            {
                "cs-CZ","da-DK","de-DE","el-GR","en-US",
                "es-ES","fi-FI","fr-FR","hu-HU","it-IT",
                "ja-JP","ko-KR","nb-NO","nl-NL","pl-PL",
                "pt-BR","pt-PT","ru-RU","sv-SE","tr-TR",
                "zh-CN","zh-HK","zh-TW"
            };

            foreach (string cultureName in cultureNames)
            {
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }

                var comparer = new CaseInsensitiveComparer(culture);

                // Turkish has lower-case and upper-case version of the dotted "i", so the upper case of "i" (U+0069) isn't "I" (U+0049)
                // but rather "İ" (U+0130)
                if (culture.Name == "tr-TR")
                {
                    Assert.Equal(1, comparer.Compare("file", "FILE"));
                }
                else
                {
                    Assert.Equal(0, comparer.Compare("file", "FILE"));
                }
            }
        }

        [Fact]
        public static void TestCtor_CultureInfo_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new CaseInsensitiveComparer(null)); // Culture is null
        }

        [Theory]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData("hello", null, 1)]
        [InlineData(null, "hello", -1)]
        [InlineData("file", "FILE", 0)] // Turkey's comparing system is ignored as this is invariant
        [InlineData(5, 5, 0)]
        [InlineData(10, 5, 1)]
        [InlineData(5, 10, -1)]
        [InlineData(5, null, 1)]
        [InlineData(null, 5, -1)]
        [InlineData(null, null, 0)]
        public static void TestDefaultInvariant_Compare(object a, object b, int expected)
        {
            var cultureNames = new string[]
            {
                "cs-CZ","da-DK","de-DE","el-GR","en-US",
                "es-ES","fi-FI","fr-FR","hu-HU","it-IT",
                "ja-JP","ko-KR","nb-NO","nl-NL","pl-PL",
                "pt-BR","pt-PT","ru-RU","sv-SE","tr-TR",
                "zh-CN","zh-HK","zh-TW"
            };

            CultureInfo culture1 = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo culture2 = CultureInfo.DefaultThreadCurrentCulture;

            foreach (string cultureName in cultureNames)
            {
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }

                // Set current culture
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                // All cultures should sort the same way, irrespective of the thread's culture
                CaseInsensitiveComparer defaultInvComparer = CaseInsensitiveComparer.DefaultInvariant;
                Assert.Equal(expected, Helpers.NormalizeCompare(defaultInvComparer.Compare(a, b)));
            }

            CultureInfo.DefaultThreadCurrentCulture = culture1;
            CultureInfo.DefaultThreadCurrentUICulture = culture2;
        }

        [Theory]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData("hello", null, 1)]
        [InlineData(null, "hello", -1)]
        [InlineData(5, 5, 0)]
        [InlineData(10, 5, 1)]
        [InlineData(5, 10, -1)]
        [InlineData(5, null, 1)]
        [InlineData(null, 5, -1)]
        [InlineData(null, null, 0)]
        public static void TestDefault_Compare(object a, object b, int expected)
        {
            Assert.Equal(expected, Helpers.NormalizeCompare(CaseInsensitiveComparer.Default.Compare(a, b)));
        }

        [Fact]
        public static void TestDefault_Compare_TurkishI()
        {
            // Turkish has lower-case and upper-case version of the dotted "i", so the upper case of "i" (U+0069) isn't "I" (U+0049)
            // but rather "İ" (U+0130)
            CultureInfo culture = CultureInfo.CurrentCulture;
            CaseInsensitiveComparer comparer = CaseInsensitiveComparer.Default;
            if (culture.Name == "tr-TR")
            {
                Assert.Equal(1, comparer.Compare("file", "FILE"));
            }
            else
            {
                Assert.Equal(0, comparer.Compare("file", "FILE"));
            }
        }
    }
}
