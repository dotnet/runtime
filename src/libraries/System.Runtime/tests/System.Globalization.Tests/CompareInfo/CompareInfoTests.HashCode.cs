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

        public static IEnumerable<object[]> CharsIgnoredByEqualFunction()
        {
            string str1 = "ab";
            // browser supports 240 cultures (e.g. "en-US"), out of which 54 neutral cultures (e.g. "en")
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            List<int> ignoredCodepoints = new();
            foreach(var culture in cultures)
            {
                // japanese with None is not supported, JS always ignores Kana when localeCompare is used
                CompareOptions options = culture.Name == "ja" ?
                    CompareOptions.IgnoreKanaType :
                    CompareOptions.None;
                CompareInfo cmpInfo = culture.CompareInfo;
                var hashCode1 = cmpInfo.GetHashCode(str1, options);
                for(int codePoint = 0; codePoint < 0x10FFFF; codePoint++)
                {
                    char character = (char)codePoint;
					string str2 = $"a{character}b";
                    // in HybridGlobalization CompareInfo.Compare uses JS's localeCompare()
                    if (cmpInfo.Compare(str1, str2, options) == 0)
                    {
                        // do not test same codepoint with different cultures
                        if (!ignoredCodepoints.Contains(codePoint))
                        {
                            ignoredCodepoints.Add(codePoint);
                            yield return new object[] { hashCode1, str2, cmpInfo, options };
                        }
                    }
                }
            }
        }

        // In non-hybrid we have hashing and Equal function from the same source - ICU4C, so this test is not necessary
        // Hybrid has Equal function from JS and hashing from managed invariant algorithm, they might start diverging at some point
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnBrowser))]
        [MemberData(nameof(CharsIgnoredByEqualFunction))]
        public void CheckHashingOfSkippedChars(int hashCode1, string str2, CompareInfo cmpInfo, CompareOptions options)
        {
            var hashCode2 = cmpInfo.GetHashCode(str2, options);
            bool areHashCodesEqual = hashCode1 == hashCode2;
            Assert.True(areHashCodesEqual);
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
