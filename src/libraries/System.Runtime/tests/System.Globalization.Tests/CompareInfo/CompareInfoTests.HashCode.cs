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

        [Theory]
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
    }
}
