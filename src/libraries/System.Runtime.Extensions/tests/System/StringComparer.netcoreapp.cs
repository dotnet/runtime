// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static partial class StringComparerTests
    {
        public static readonly object[][] FromComparison_TestData =
        {
            //             StringComparison                 StringComparer
            new object[] { StringComparison.CurrentCulture, StringComparer.CurrentCulture },
            new object[] { StringComparison.CurrentCultureIgnoreCase, StringComparer.CurrentCultureIgnoreCase },
            new object[] { StringComparison.InvariantCulture, StringComparer.InvariantCulture },
            new object[] { StringComparison.InvariantCultureIgnoreCase, StringComparer.InvariantCultureIgnoreCase },
            new object[] { StringComparison.Ordinal, StringComparer.Ordinal },
            new object[] { StringComparison.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase },
        };

        [Theory]
        [MemberData(nameof(FromComparison_TestData))]
        public static void FromComparisonTest(StringComparison comparison, StringComparer comparer)
        {
            Assert.Equal(comparer, StringComparer.FromComparison(comparison));
        }

        [Fact]
        public static void FromComparisonInvalidTest()
        {
            StringComparison minInvalid = Enum.GetValues(typeof(StringComparison)).Cast<StringComparison>().Min() - 1;
            StringComparison maxInvalid = Enum.GetValues(typeof(StringComparison)).Cast<StringComparison>().Max() + 1;

            AssertExtensions.Throws<ArgumentException>("comparisonType", () => StringComparer.FromComparison(minInvalid));
            AssertExtensions.Throws<ArgumentException>("comparisonType", () => StringComparer.FromComparison(maxInvalid));
        }

        public static TheoryData<string, string, string, CompareOptions, bool> CreateFromCultureAndOptionsData => new TheoryData<string, string, string, CompareOptions, bool>
        {
            { "abcd", "ABCD", "en-US", CompareOptions.None, false},
            { "latin i", "LATIN I", "en-US", CompareOptions.None, false},
            { "turky \u0131", "TURKY I", "tr-TR", CompareOptions.None, false},
            { "turky i", "TURKY \u0130", "tr-TR", CompareOptions.None, false},
            { "abcd", "ABCD", "en-US", CompareOptions.IgnoreCase, true},
            { "latin i", "LATIN I", "en-US", CompareOptions.IgnoreCase, true},
            { "turky \u0131", "TURKY I", "tr-TR", CompareOptions.IgnoreCase, true},
            { "turky i", "TURKY \u0130", "tr-TR", CompareOptions.IgnoreCase, true},
            { "abcd", "ab cd", "en-US", CompareOptions.IgnoreSymbols, true },
            { "abcd", "ab+cd", "en-US", CompareOptions.IgnoreSymbols, true },
            { "abcd", "ab%cd", "en-US", CompareOptions.IgnoreSymbols, true },
            { "abcd", "ab&cd", "en-US", CompareOptions.IgnoreSymbols, true },
            { "abcd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true },
            { "abcd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true },
            { "a-bcd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true },
            { "abcd*", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true },
            { "ab$dd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, false },
            { "abcd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true },
        };

        public static TheoryData<string, string, string, CompareOptions, bool> CreateFromCultureAndOptionsStringSortData => new TheoryData<string, string, string, CompareOptions, bool>
        {
            { "abcd", "abcd", "en-US", CompareOptions.StringSort, true },
            { "abcd", "ABcd", "en-US", CompareOptions.StringSort, false },
        };

        [Theory]
        [MemberData(nameof(CreateFromCultureAndOptionsData))]
        [MemberData(nameof(CreateFromCultureAndOptionsStringSortData))]
        public static void CreateFromCultureAndOptions(string actualString, string expectedString, string cultureName, CompareOptions options, bool result)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            StringComparer sc = StringComparer.Create(ci, options);

            Assert.Equal(result, sc.Equals(actualString, expectedString));
            Assert.Equal(result, sc.Equals((object)actualString, (object)expectedString));
        }

        [Theory]
        [MemberData(nameof(CreateFromCultureAndOptionsData))]
        public static void CreateFromCultureAndOptionsStringSort(string actualString, string expectedString, string cultureName, CompareOptions options, bool result)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            StringComparer sc = StringComparer.Create(ci, options);

            if (result)
            {
                Assert.Equal(sc.GetHashCode(actualString), sc.GetHashCode(expectedString));
                Assert.Equal(sc.GetHashCode((object)actualString), sc.GetHashCode((object)actualString));
            }
            else
            {
                Assert.NotEqual(sc.GetHashCode(actualString), sc.GetHashCode(expectedString));
                Assert.NotEqual(sc.GetHashCode((object)actualString), sc.GetHashCode((object)expectedString));
            }
        }

        [Fact]
        public static void CreateFromCultureAndOptionsOrdinal()
        {
            CultureInfo ci = CultureInfo.GetCultureInfo("en-US");
            Assert.Throws<ArgumentException>(() => StringComparer.Create(ci, CompareOptions.Ordinal));
            Assert.Throws<ArgumentException>(() => StringComparer.Create(ci, CompareOptions.OrdinalIgnoreCase));
        }
    }
}
