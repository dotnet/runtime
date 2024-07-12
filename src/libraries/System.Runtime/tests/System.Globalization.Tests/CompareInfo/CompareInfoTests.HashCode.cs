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

        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95338", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnApplePlatform))]
        public void CheckHashingInLineWithEqual()
        {
            int additionalCollisions = 0;
            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            foreach (CultureInfo culture in cultures)
            {
                // Japanese does not have "None" compare option, it always ignores Kana
                // HashCode is not available for options different than None or IgnoreCase
                if (culture.Name.Split('-')[0] == "ja")
                    continue;

                for (int i = 0; i <= 65535; i++)
                    CheckChar(i, culture);
            }

            void CheckChar(int charCode, CultureInfo culture)
            {
                var cmpInfo = culture.CompareInfo;
                char character = (char)charCode;
                string str1 = $"a{character}b";
                string str2 = "ab";
                CompareOptions options = CompareOptions.None;
                var hashCode1 = cmpInfo.GetHashCode(str1, options);
                var hashCode2 = cmpInfo.GetHashCode(str2, options);
                bool areHashCodesEqual = hashCode1 == hashCode2;
                StringComparer stringComparer = new CustomComparer(cmpInfo, options);
                // if equal => same, then expect hash => same  
                if (stringComparer.Compare(str1, str2) == 0)
                {
                    Assert.True(areHashCodesEqual, $"Expected equal hashes for equal strings. The check failed for culture {culture.Name}, character: {character}, code: {charCode}.");
                }
                // if equal => diff, then expect hash => diff
                else
                {
                    if (areHashCodesEqual)
                    {
                        additionalCollisions++; // this should be smallest possible, 11541466
                    }
                }
            }
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
