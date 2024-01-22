// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class CompareInfoHashCodeTests : CompareInfoTestsBase
    {
        public static IEnumerable<object[]> HashCodeLocalized_TestData()
        {
            yield return new object[] { s_invariantCompare, "foo", "Foo", CompareOptions.IgnoreCase };
            yield return new object[] { s_invariantCompare, "igloo", "\u0130GLOO", CompareOptions.IgnoreCase };
            yield return new object[] { s_invariantCompare, "igloo", "\u0130GLOO", CompareOptions.None };
            yield return new object[] { s_invariantCompare, "igloo", "IGLOO", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("pl-PL").CompareInfo, "igloo", "\u0130GLOO", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("pl-PL").CompareInfo, "igloo", "IGLOO", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("tr-TR").CompareInfo, "igloo", "\u0130GLOO", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("tr-TR").CompareInfo, "igloo", "IGLOO", CompareOptions.IgnoreCase };

            if (!PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                yield return new object[] { new CultureInfo("en-GB").CompareInfo, "100", "100!", CompareOptions.IgnoreSymbols }; // HG: equal: True, hashCodesEqual: False
                yield return new object[] { new CultureInfo("ja-JP").CompareInfo, "\u30A2", "\u3042", CompareOptions.IgnoreKanaType }; // HG: equal: True, hashCodesEqual: False
                yield return new object[] { new CultureInfo("en-GB").CompareInfo, "caf\u00E9", "cafe\u0301", CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreKanaType }; // HG: equal: True, hashCodesEqual: False
            }
        }

        [Theory]
        [MemberData(nameof(HashCodeLocalized_TestData))]
        public void HashCodeLocalized(CompareInfo cmpInfo, string str1, string str2, CompareOptions options)
        {
            bool areEqual = cmpInfo.Compare(str1, str2, options) == 0;
            var hashCode1 = cmpInfo.GetHashCode(str1, options);
            var hashCode2 = cmpInfo.GetHashCode(str2, options);
            bool areHashCodesEqual = hashCode1 == hashCode2;

            if (areEqual)
            {
                Assert.True(areHashCodesEqual);
            }
            else
            {
                Assert.False(areHashCodesEqual);
            }

            // implication of the above behavior:
            StringComparer stringComparer = new CustomComparer(cmpInfo, options);
            TryAddToCustomDictionary(stringComparer, str1, str2, areHashCodesEqual);
        }

        private void TryAddToCustomDictionary(StringComparer comparer, string str1, string str2, bool shouldFail)
        {
            Dictionary<string, int> customDictionary = new Dictionary<string, int>(comparer);
            customDictionary.Add(str1, 0);
            try
            {
                customDictionary.Add(str2, 1);
                Assert.False(shouldFail);
            }
            catch (ArgumentException ex)
            {
                Assert.True(shouldFail);
                Assert.Contains("An item with the same key has already been added.", ex.Message);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception thrown: {ex}");
            }
        }

        public static IEnumerable<object[]> CheckHashingOfSkippedChars_TestData()
        {
            // one char from each ignored category that is skipped on ICU
            yield return new object[] { '\u0008', s_invariantCompare }; // Control: BACKSPACE
            yield return new object[] { '\u200B', s_invariantCompare }; // Format: ZERO WIDTH SPACE
            yield return new object[] { '\u180A', s_invariantCompare }; // OtherPunctuation: MONGOLIAN NIRUGU
            yield return new object[] { '\uFE73', s_invariantCompare }; // OtherLetter: THAI CHARACTER PAIYANNOI
            yield return new object[] { '\u0F3E', s_invariantCompare }; // SpacingCombiningMark: "TIBETAN MARK GTER YIG MGO UM RTAGS GNYIS
            yield return new object[] { '\u0640', s_invariantCompare }; // ModifierLetter: ARABIC TATWEEL
            yield return new object[] { '\u0488', s_invariantCompare }; // EnclosingMark: COMBINING CYRILLIC HUNDRED THOUSANDS SIGN
            yield return new object[] { '\u034F', s_invariantCompare }; // NonSpacingMark: DIAERESIS
            CompareInfo thaiCmpInfo = new CultureInfo("th-TH").CompareInfo;
            yield return new object[] { '\u0020', thaiCmpInfo }; // SpaceSeparator: SPACE
            yield return new object[] { '\u0028', thaiCmpInfo }; // OpenPunctuation: LEFT PARENTHESIS
            yield return new object[] { '\u007D', thaiCmpInfo }; // ClosePunctuation: RIGHT PARENTHESIS
            yield return new object[] { '\u2013', thaiCmpInfo }; // DashPunctuation: EN DASH
            yield return new object[] { '\u005F', thaiCmpInfo }; // ConnectorPunctuation: LOW LINE
            yield return new object[] { '\u2018', thaiCmpInfo }; // InitialQuotePunctuation: LEFT SINGLE QUOTATION MARK
            yield return new object[] { '\u2019', thaiCmpInfo }; // FinalQuotePunctuation: RIGHT SINGLE QUOTATION MARK
            yield return new object[] { '\u2028', thaiCmpInfo }; // LineSeparator: LINE SEPARATOR
            yield return new object[] { '\u2029', thaiCmpInfo }; // ParagraphSeparator: PARAGRAPH SEPARATOR
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [MemberData(nameof(CheckHashingOfSkippedChars_TestData))]
        public void CheckHashingOfSkippedChars(char character, CompareInfo cmpInfo)
        {
            string str1 = $"a{character}b";
            string str2 = "ab";
            CompareOptions options = CompareOptions.None;
            var hashCode1 = cmpInfo.GetHashCode(str1, options);
            var hashCode2 = cmpInfo.GetHashCode(str2, options);
            bool areHashCodesEqual = hashCode1 == hashCode2;
            Assert.True(areHashCodesEqual);
            StringComparer stringComparer = new CustomComparer(cmpInfo, options);
            TryAddToCustomDictionary(stringComparer, str1, str2, areHashCodesEqual);
        }

        public static IEnumerable<object[]> GetHashCodeTestData => new[]
        {
            new object[] { "abc", CompareOptions.OrdinalIgnoreCase, "ABC", CompareOptions.OrdinalIgnoreCase, true },
            new object[] { "abc", CompareOptions.Ordinal, "ABC", CompareOptions.Ordinal, false },
            new object[] { "abc", CompareOptions.Ordinal, "abc", CompareOptions.Ordinal, true },
            new object[] { "abc", CompareOptions.None, "abc", CompareOptions.None, true },
            new object[] { "", CompareOptions.None, "\u200c", CompareOptions.None, true }, // see comment at bottom of SortKey_TestData
        };

        [Theory]
        [MemberData(nameof(GetHashCodeTestData))]
        public void GetHashCodeTest(string source1, CompareOptions options1, string source2, CompareOptions options2, bool expected)
        {
            CompareInfo invariantCompare = CultureInfo.InvariantCulture.CompareInfo;
            Assert.Equal(expected, invariantCompare.GetHashCode(source1, options1).Equals(invariantCompare.GetHashCode(source2, options2)));
        }

        [Theory]
        [MemberData(nameof(GetHashCodeTestData))]
        public void GetHashCode_Span(string source1, CompareOptions options1, string source2, CompareOptions options2, bool expectSameHashCode)
        {
            CompareInfo invariantCompare = CultureInfo.InvariantCulture.CompareInfo;

            int hashOfSource1AsString = invariantCompare.GetHashCode(source1, options1);
            int hashOfSource1AsSpan = invariantCompare.GetHashCode(source1.AsSpan(), options1);
            Assert.Equal(hashOfSource1AsString, hashOfSource1AsSpan);

            int hashOfSource2AsString = invariantCompare.GetHashCode(source2, options2);
            int hashOfSource2AsSpan = invariantCompare.GetHashCode(source2.AsSpan(), options2);
            Assert.Equal(hashOfSource2AsString, hashOfSource2AsSpan);

            Assert.Equal(expectSameHashCode, hashOfSource1AsSpan == hashOfSource2AsSpan);
        }

        [Fact]
        public void GetHashCode_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode(null, CompareOptions.None));

            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreCase));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", CompareOptions.Ordinal | CompareOptions.IgnoreSymbols));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test", (CompareOptions)(-1)));
        }

        [Fact]
        public void GetHashCode_NullAndEmptySpan()
        {
            // Ensure that null spans and non-null empty spans produce the same hash code.

            int hashCodeOfNullSpan = CultureInfo.InvariantCulture.CompareInfo.GetHashCode(ReadOnlySpan<char>.Empty, CompareOptions.None);
            int hashCodeOfNotNullEmptySpan = CultureInfo.InvariantCulture.CompareInfo.GetHashCode("".AsSpan(), CompareOptions.None);
            Assert.Equal(hashCodeOfNullSpan, hashCodeOfNotNullEmptySpan);
        }

        [Fact]
        public void GetHashCode_Span_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreCase));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), CompareOptions.Ordinal | CompareOptions.IgnoreSymbols));
            AssertExtensions.Throws<ArgumentException>("options", () => CultureInfo.InvariantCulture.CompareInfo.GetHashCode("Test".AsSpan(), (CompareOptions)(-1)));
        }
    }
}
