// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Tests;
using Xunit;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Text.Tests
{
    public partial class Utf8SpanTests
    {
        [Theory]
        [MemberData(nameof(NormalizationData))]
        public static void Normalize(string utf16Source, string utf16Expected, NormalizationForm normalizationForm)
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(utf16Source);
            Utf8Span utf8Source = boundedSpan.Span;

            // Quick IsNormalized tests

            Assert.Equal(utf16Source == utf16Expected, utf8Source.IsNormalized(normalizationForm));

            // Normalize and return new Utf8String instances

            ustring utf8Normalized = utf8Source.Normalize(normalizationForm);
            Assert.True(ustring.AreEquivalent(utf8Normalized, utf16Expected));

            // Normalize to byte arrays which are too small, expect -1 (failure)

            Assert.Equal(-1, utf8Source.Normalize(new byte[utf8Normalized.Length - 1], normalizationForm));

            // Normalize to byte arrays which are the correct length, expect success,
            // then compare buffer contents for ordinal equality.

            foreach (int bufferLength in new int[] { utf8Normalized.Length /* just right */, utf8Normalized.Length + 1 /* with extra room */})
            {
                byte[] dest = new byte[bufferLength];
                Assert.Equal(utf8Normalized.Length, utf8Source.Normalize(dest, normalizationForm));
                Utf8Span normalizedSpan = Utf8Span.UnsafeCreateWithoutValidation(dest[..utf8Normalized.Length]);
                Assert.True(utf8Normalized.AsSpan() == normalizedSpan); // ordinal equality
                Assert.True(normalizedSpan.IsNormalized(normalizationForm));
            }
        }

        [Theory]
        [MemberData(nameof(CaseConversionData))]
        public static void ToLower(string testData)
        {
            static void RunTest(string testData, string expected, CultureInfo culture)
            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(testData);
                Utf8Span inputSpan = boundedSpan.Span;

                // First try the allocating APIs

                ustring expectedUtf8 = u8(expected) ?? ustring.Empty;
                ustring actualUtf8;

                if (culture is null)
                {
                    actualUtf8 = inputSpan.ToLowerInvariant();
                }
                else
                {
                    actualUtf8 = inputSpan.ToLower(culture);
                }

                Assert.Equal(expectedUtf8, actualUtf8);

                // Next, try the non-allocating APIs with too small a buffer

                if (expectedUtf8.Length > 0)
                {
                    byte[] bufferTooSmall = new byte[expectedUtf8.Length - 1];

                    if (culture is null)
                    {
                        Assert.Equal(-1, inputSpan.ToLowerInvariant(bufferTooSmall));
                    }
                    else
                    {
                        Assert.Equal(-1, inputSpan.ToLower(bufferTooSmall, culture));
                    }
                }

                // Then the non-allocating APIs with a properly sized buffer

                foreach (int bufferSize in new[] { expectedUtf8.Length, expectedUtf8.Length + 1 })
                {
                    byte[] buffer = new byte[expectedUtf8.Length];

                    if (culture is null)
                    {
                        Assert.Equal(expectedUtf8.Length, inputSpan.ToLowerInvariant(buffer));
                    }
                    else
                    {
                        Assert.Equal(expectedUtf8.Length, inputSpan.ToLower(buffer, culture));
                    }

                    Assert.True(expectedUtf8.AsBytes().SequenceEqual(buffer));
                }
            }

            RunTest(testData, testData?.ToLowerInvariant(), null);
            RunTest(testData, testData?.ToLower(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            RunTest(testData, testData?.ToLower(CultureInfo.GetCultureInfo("en-US")), CultureInfo.GetCultureInfo("en-US"));
            RunTest(testData, testData?.ToLower(CultureInfo.GetCultureInfo("tr-TR")), CultureInfo.GetCultureInfo("tr-TR"));
        }

        [Theory]
        [MemberData(nameof(CaseConversionData))]
        public static void ToUpper(string testData)
        {
            static void RunTest(string testData, string expected, CultureInfo culture)
            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(testData);
                Utf8Span inputSpan = boundedSpan.Span;

                // First try the allocating APIs

                ustring expectedUtf8 = u8(expected) ?? ustring.Empty;
                ustring actualUtf8;

                if (culture is null)
                {
                    actualUtf8 = inputSpan.ToUpperInvariant();
                }
                else
                {
                    actualUtf8 = inputSpan.ToUpper(culture);
                }

                Assert.Equal(expectedUtf8, actualUtf8);

                // Next, try the non-allocating APIs with too small a buffer

                if (expectedUtf8.Length > 0)
                {
                    byte[] bufferTooSmall = new byte[expectedUtf8.Length - 1];

                    if (culture is null)
                    {
                        Assert.Equal(-1, inputSpan.ToUpperInvariant(bufferTooSmall));
                    }
                    else
                    {
                        Assert.Equal(-1, inputSpan.ToUpper(bufferTooSmall, culture));
                    }
                }

                // Then the non-allocating APIs with a properly sized buffer

                foreach (int bufferSize in new[] { expectedUtf8.Length, expectedUtf8.Length + 1 })
                {
                    byte[] buffer = new byte[expectedUtf8.Length];

                    if (culture is null)
                    {
                        Assert.Equal(expectedUtf8.Length, inputSpan.ToUpperInvariant(buffer));
                    }
                    else
                    {
                        Assert.Equal(expectedUtf8.Length, inputSpan.ToUpper(buffer, culture));
                    }

                    Assert.True(expectedUtf8.AsBytes().SequenceEqual(buffer));
                }
            }

            RunTest(testData, testData?.ToUpperInvariant(), null);
            RunTest(testData, testData?.ToUpper(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            RunTest(testData, testData?.ToUpper(CultureInfo.GetCultureInfo("en-US")), CultureInfo.GetCultureInfo("en-US"));
            RunTest(testData, testData?.ToUpper(CultureInfo.GetCultureInfo("tr-TR")), CultureInfo.GetCultureInfo("tr-TR"));
        }

        public static IEnumerable<object[]> CaseConversionData()
        {
            string[] testCases = new string[]
            {
                null,
                string.Empty,
                "Hello",
                "iı", // dotted and dotless I
                "İI", // dotted and dotless I
            };

            foreach (string testCase in testCases)
            {
                yield return new object[] { testCase };
            }
        }

        public static IEnumerable<object[]> NormalizationData()
        {
            // These test cases are from the Unicode Standard Annex #15, Figure 6
            // https://unicode.org/reports/tr15/

            var testCases = new[]
            {
                new
                {
                    Source = "\ufb01", // "ﬁ" (LATIN SMALL LIGATURE FI)
                    NFD = "\ufb01", // same as source
                    NFC = "\ufb01", // same as source
                    NFKD = "fi", // compatibility decomposed into ASCII chars
                    NFKC = "fi", // compatibility decomposed into ASCII chars
                },
                new
                {
                    Source = "2\u2075", // "2⁵" (SUPERSCRIPT FIVE)
                    NFD = "2\u2075", // same as source
                    NFC = "2\u2075", // same as source
                    NFKD = "25", // compatibility decomposed into ASCII chars
                    NFKC = "25", // compatibility decomposed into ASCII chars
                },
                new
                {
                    Source = "\u1e9b\u0323", // 'ẛ' (LATIN SMALL LETTER LONG S WITH DOT ABOVE) + COMBINING DOT BELOW
                    NFD = "\u017f\u0323\u0307", // 'ſ' (LATIN SMALL LETTER LONG S) + COMBINING DOT BELOW + COMBINING DOT ABOVE
                    NFC = "\u1e9b\u0323", // same as source
                    NFKD = "s\u0323\u0307", // ASCII 's' + COMBINING DOT BELOW + COMBINING DOT ABOVE
                    NFKC = "\u1e69", // "ṩ" (LATIN SMALL LETTER S WITH DOT BELOW AND DOT ABOVE)
                }
            };

            foreach (var testCase in testCases)
            {
                yield return new object[] { testCase.Source, testCase.NFD, NormalizationForm.FormD };
                yield return new object[] { testCase.Source, testCase.NFC, NormalizationForm.FormC };
                yield return new object[] { testCase.Source, testCase.NFKD, NormalizationForm.FormKD };
                yield return new object[] { testCase.Source, testCase.NFKC, NormalizationForm.FormKC };
            }
        }
    }
}
