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
        public class CustomComparer : StringComparer
        {
            private readonly CompareInfo _compareInfo;
            private readonly CompareOptions _compareOptions;

            public CustomComparer(CompareInfo cmpInfo, CompareOptions cmpOptions)
            {
                _compareInfo = cmpInfo;
                _compareOptions = cmpOptions;
            }

            public override int Compare(string x, string y) =>
                _compareInfo.Compare(x, y, _compareOptions);

            public override bool Equals(string x, string y) =>
                _compareInfo.Compare(x, y, _compareOptions) == 0;

            public override int GetHashCode(string obj)
            {
                return _compareInfo.GetHashCode(obj, _compareOptions);
            }
        }

        public static IEnumerable<object[]> HashCodeLocalized_TestData()
        {
            yield return new object[] { s_invariantCompare, "foo", "Foo", CompareOptions.IgnoreCase };
            yield return new object[] { s_invariantCompare, "igloo", "\u0130GLOO", CompareOptions.IgnoreCase }; // FAILS
            yield return new object[] { s_invariantCompare, "igloo", "IGLOO", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("pl-PL").CompareInfo, "igloo", "\u0130GLOO", CompareOptions.IgnoreCase }; // FAILS
            yield return new object[] { new CultureInfo("pl-PL").CompareInfo, "igloo", "IGLOO", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("tr-TR").CompareInfo, "igloo", "\u0130GLOO", CompareOptions.IgnoreCase };
            yield return new object[] { new CultureInfo("tr-TR").CompareInfo, "igloo", "IGLOO", CompareOptions.IgnoreCase }; // FAILS

            if (!PlatformDetection.IsHybridGlobalizationOnBrowser)
            {
                // ActiveIssue: https://github.com/dotnet/runtime/issues/96400
                yield return new object[] { new CultureInfo("en-GB").CompareInfo, "100", "100!", CompareOptions.IgnoreSymbols }; // HG: equal: True, hashCodesEqual: False
                yield return new object[] { new CultureInfo("ja-JP").CompareInfo, "\u30A2", "\u3042", CompareOptions.IgnoreKanaType }; // HG: equal: True, hashCodesEqual: False
                yield return new object[] { new CultureInfo("en-GB").CompareInfo, "caf\u00E9", "cafe\u0301", CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreKanaType }; // HG: equal: True, hashCodesEqual: False
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
    }
}
