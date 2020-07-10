// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Tests;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public unsafe partial class Utf8StringTests
    {
        public static IEnumerable<object[]> NormalizationData() => Utf8SpanTests.NormalizationData();

        [Theory]
        [MemberData(nameof(NormalizationData))]
        public static void Normalize(string utf16Source, string utf16Expected, NormalizationForm normalizationForm)
        {
            Utf8String utf8Source = u8(utf16Source);

            // Quick IsNormalized tests

            Assert.Equal(utf16Source == utf16Expected, utf8Source.IsNormalized(normalizationForm));

            // Normalize and return new Utf8String instances

            Utf8String utf8Normalized = utf8Source.Normalize(normalizationForm);
            Assert.True(Utf8String.AreEquivalent(utf8Normalized, utf16Expected));
        }

        public static IEnumerable<object[]> CaseConversionData() => Utf8SpanTests.CaseConversionData();

        [Theory]
        [MemberData(nameof(CaseConversionData))]
        public static void ToLower(string testData)
        {
            static void RunTest(string testData, string expected, CultureInfo culture)
            {
                if (culture is null)
                {
                    Assert.Equal(u8(expected), u8(testData).ToLowerInvariant());
                }
                else
                {
                    Assert.Equal(u8(expected), u8(testData).ToLower(culture));
                }
            }

            if (testData is null)
            {
                return; // no point in testing null "this" objects; we'll just null-ref
            }

            RunTest(testData, testData.ToLowerInvariant(), null);
            RunTest(testData, testData.ToLower(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            RunTest(testData, testData.ToLower(CultureInfo.GetCultureInfo("en-US")), CultureInfo.GetCultureInfo("en-US"));
            RunTest(testData, testData.ToLower(CultureInfo.GetCultureInfo("tr-TR")), CultureInfo.GetCultureInfo("tr-TR"));
        }

        [Theory]
        [MemberData(nameof(CaseConversionData))]
        public static void ToUpper(string testData)
        {
            static void RunTest(string testData, string expected, CultureInfo culture)
            {
                if (culture is null)
                {
                    Assert.Equal(u8(expected), u8(testData).ToUpperInvariant());
                }
                else
                {
                    Assert.Equal(u8(expected), u8(testData).ToUpper(culture));
                }
            }

            if (testData is null)
            {
                return; // no point in testing null "this" objects; we'll just null-ref
            }

            RunTest(testData, testData.ToUpperInvariant(), null);
            RunTest(testData, testData.ToUpper(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            RunTest(testData, testData.ToUpper(CultureInfo.GetCultureInfo("en-US")), CultureInfo.GetCultureInfo("en-US"));
            RunTest(testData, testData.ToUpper(CultureInfo.GetCultureInfo("tr-TR")), CultureInfo.GetCultureInfo("tr-TR"));
        }
    }
}
