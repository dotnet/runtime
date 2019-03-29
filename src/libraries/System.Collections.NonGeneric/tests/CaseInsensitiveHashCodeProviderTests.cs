// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

#pragma warning disable 618 // obsolete types

namespace System.Collections.Tests
{
    public class CaseInsensitiveHashCodeProviderTests
    {
        [Theory]
        [InlineData("hello", "HELLO", true)]
        [InlineData("hello", "hello", true)]
        [InlineData("HELLO", "HELLO", true)]
        [InlineData("hello", "goodbye", false)]
        [InlineData(5, 5, true)]
        [InlineData(10, 5, false)]
        [InlineData(5, 10, false)]
        public void Ctor_Empty_GetHashCodeCompare(object a, object b, bool expected)
        {
            var provider = new CaseInsensitiveHashCodeProvider();
            Assert.Equal(provider.GetHashCode(a), provider.GetHashCode(a));
            Assert.Equal(provider.GetHashCode(b), provider.GetHashCode(b));
            Assert.Equal(expected, provider.GetHashCode(a) == provider.GetHashCode(b));
        }

        [Theory]
        [InlineData("hello", "HELLO", true)]
        [InlineData("hello", "hello", true)]
        [InlineData("HELLO", "HELLO", true)]
        [InlineData("hello", "goodbye", false)]
        [InlineData(5, 5, true)]
        [InlineData(10, 5, false)]
        [InlineData(5, 10, false)]
        public void Ctor_Empty_ChangeCurrentCulture_GetHashCodeCompare(object a, object b, bool expected)
        {
            RemoteExecutor.Invoke((ra, rb, rexpected) =>
            {
                var cultureNames = new string[]
                {
                    "cs-CZ","da-DK","de-DE","el-GR","en-US",
                    "es-ES","fi-FI","fr-FR","hu-HU","it-IT",
                    "ja-JP","ko-KR","nb-NO","nl-NL","pl-PL",
                    "pt-BR","pt-PT","ru-RU","sv-SE","tr-TR",
                    "zh-CN","zh-HK","zh-TW"
                };

                bool.TryParse(rexpected, out bool expectedResult);

                foreach (string cultureName in cultureNames)
                {
                    CultureInfo newCulture;
                    try
                    {
                        newCulture = new CultureInfo(cultureName);
                    }
                    catch (CultureNotFoundException)
                    {
                        continue;
                    }

                    CultureInfo.CurrentCulture = newCulture;
                    var provider = new CaseInsensitiveHashCodeProvider();
                    Assert.Equal(provider.GetHashCode(ra), provider.GetHashCode(ra));
                    Assert.Equal(provider.GetHashCode(rb), provider.GetHashCode(rb));
                    Assert.Equal(expectedResult, provider.GetHashCode(ra) == provider.GetHashCode(rb));
                }
                return RemoteExecutor.SuccessExitCode;
            }, a.ToString(), b.ToString(), expected.ToString()).Dispose();
        }

        [Theory]
        [InlineData("hello", "HELLO", true)]
        [InlineData("hello", "hello", true)]
        [InlineData("HELLO", "HELLO", true)]
        [InlineData("hello", "goodbye", false)]
        [InlineData(5, 5, true)]
        [InlineData(10, 5, false)]
        [InlineData(5, 10, false)]
        public void Ctor_CultureInfo_ChangeCurrentCulture_GetHashCodeCompare(object a, object b, bool expected)
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

                var provider = new CaseInsensitiveHashCodeProvider(culture);
                Assert.Equal(provider.GetHashCode(a), provider.GetHashCode(a));
                Assert.Equal(provider.GetHashCode(b), provider.GetHashCode(b));
                Assert.Equal(expected, provider.GetHashCode(a) == provider.GetHashCode(b));
            }
        }

        [Fact]
        public void Ctor_CultureInfo_GetHashCodeCompare_TurkishI()
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

                var provider = new CaseInsensitiveHashCodeProvider(culture);

                // Turkish has lower-case and upper-case version of the dotted "i", so the upper case of "i" (U+0069) isn't "I" (U+0049)
                // but rather "İ" (U+0130)
                Assert.Equal(
                    culture.Name != "tr-TR",
                    provider.GetHashCode("file") == provider.GetHashCode("FILE"));
            }
        }

        [Fact]
        public void Ctor_CultureInfo_NullCulture_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("culture", () => new CaseInsensitiveHashCodeProvider(null));
        }

        [Fact]
        public void GetHashCode_NullObj_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("obj", () => new CaseInsensitiveHashCodeProvider().GetHashCode(null));
        }

        [Theory]
        [InlineData("hello", "HELLO", true)]
        [InlineData("hello", "hello", true)]
        [InlineData("HELLO", "HELLO", true)]
        [InlineData("hello", "goodbye", false)]
        [InlineData(5, 5, true)]
        [InlineData(10, 5, false)]
        [InlineData(5, 10, false)]
        public void Default_GetHashCodeCompare(object a, object b, bool expected)
        {
            Assert.Equal(expected,
                CaseInsensitiveHashCodeProvider.Default.GetHashCode(a) == CaseInsensitiveHashCodeProvider.Default.GetHashCode(b));
            Assert.Equal(expected,
                CaseInsensitiveHashCodeProvider.DefaultInvariant.GetHashCode(a) == CaseInsensitiveHashCodeProvider.DefaultInvariant.GetHashCode(b));
        }

        [Fact]
        public void Default_Compare_TurkishI()
        {
            // Turkish has lower-case and upper-case version of the dotted "i", so the upper case of "i" (U+0069) isn't "I" (U+0049)
            // but rather "İ" (U+0130)
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                Assert.False(CaseInsensitiveHashCodeProvider.Default.GetHashCode("file") == CaseInsensitiveHashCodeProvider.Default.GetHashCode("FILE"));
                Assert.True(CaseInsensitiveHashCodeProvider.DefaultInvariant.GetHashCode("file") == CaseInsensitiveHashCodeProvider.DefaultInvariant.GetHashCode("FILE"));

                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Assert.True(CaseInsensitiveHashCodeProvider.Default.GetHashCode("file") == CaseInsensitiveHashCodeProvider.Default.GetHashCode("FILE"));
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }
    }
}
