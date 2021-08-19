// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static class StringComparerTests
    {
        [Fact]
        public static void TestCurrent()
        {
            VerifyComparer(StringComparer.CurrentCulture, false);
            VerifyComparer(StringComparer.CurrentCultureIgnoreCase, true);
        }

        [Fact]
        public static void TestOrdinal()
        {
            VerifyComparer(StringComparer.Ordinal, false);
            VerifyComparer(StringComparer.OrdinalIgnoreCase, true);
        }

        [Fact]
        public static void TestOrdinal_EmbeddedNull_ReturnsDifferentHashCodes()
        {
            StringComparer sc = StringComparer.Ordinal;
            Assert.NotEqual(sc.GetHashCode("\0AAAAAAAAA"), sc.GetHashCode("\0BBBBBBBBBBBB"));
            sc = StringComparer.OrdinalIgnoreCase;
            Assert.NotEqual(sc.GetHashCode("\0AAAAAAAAA"), sc.GetHashCode("\0BBBBBBBBBBBB"));
        }

        private static void VerifyComparer(StringComparer sc, bool ignoreCase)
        {
            string s1 = "Hello";
            string s1a = "Hello";
            string s1b = "HELLO";
            string s2 = "There";
            string aa = "\0AAAAAAAAA";
            string bb = "\0BBBBBBBBBBBB";

            Assert.True(sc.Equals(s1, s1a));
            Assert.True(sc.Equals(s1, s1a));

            Assert.Equal(0, sc.Compare(s1, s1a));
            Assert.Equal(0, ((IComparer)sc).Compare(s1, s1a));

            Assert.True(sc.Equals(s1, s1));
            Assert.True(((IEqualityComparer)sc).Equals(s1, s1));
            Assert.Equal(0, sc.Compare(s1, s1));
            Assert.Equal(0, ((IComparer)sc).Compare(s1, s1));

            Assert.False(sc.Equals(s1, s2));
            Assert.False(((IEqualityComparer)sc).Equals(s1, s2));
            Assert.True(sc.Compare(s1, s2) < 0);
            Assert.True(((IComparer)sc).Compare(s1, s2) < 0);

            Assert.Equal(ignoreCase, sc.Equals(s1, s1b));
            Assert.Equal(ignoreCase, ((IEqualityComparer)sc).Equals(s1, s1b));

            Assert.NotEqual(0, ((IComparer)sc).Compare(aa, bb));
            Assert.False(sc.Equals(aa, bb));
            Assert.False(((IEqualityComparer)sc).Equals(aa, bb));
            Assert.True(sc.Compare(aa, bb) < 0);
            Assert.True(((IComparer)sc).Compare(aa, bb) < 0);

            int result = sc.Compare(s1, s1b);
            if (ignoreCase)
                Assert.Equal(0, result);
            else
                Assert.NotEqual(0, result);

            result = ((IComparer)sc).Compare(s1, s1b);
            if (ignoreCase)
                Assert.Equal(0, result);
            else
                Assert.NotEqual(0, result);
        }

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //                          lower                upper          Culture
            yield return new object[] { "abcd",             "ABCD",         "en-US" };
            yield return new object[] { "latin i",          "LATIN I",      "en-US" };

            if (PlatformDetection.IsNotInvariantGlobalization && !PlatformDetection.IsAndroid)
            {
                yield return new object[] { "turky \u0131",     "TURKY I",      "tr-TR" };
                yield return new object[] { "turky i",          "TURKY \u0130", "tr-TR" };
            }
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        public static void CreateWithCulturesTest(string lowerForm, string upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            StringComparer sc = StringComparer.Create(ci, false);
            Assert.False(sc.Equals(lowerForm, upperForm), "Not expected to have the lowercase equals the uppercase with ignore case is false");
            Assert.False(sc.Equals((object) lowerForm, (object) upperForm), "Not expected to have the lowercase object equals the uppercase with ignore case is false");
            Assert.NotEqual(sc.GetHashCode(lowerForm), sc.GetHashCode(upperForm));
            Assert.NotEqual(sc.GetHashCode((object) lowerForm), sc.GetHashCode((object) upperForm));

            sc = StringComparer.Create(ci, true);
            Assert.True(sc.Equals(lowerForm, upperForm), "It is expected to have the lowercase equals the uppercase with ignore case is true");
            Assert.True(sc.Equals((object) lowerForm, (object) upperForm), "It is expected to have the lowercase object equals the uppercase with ignore case is true");
            Assert.Equal(sc.GetHashCode(lowerForm), sc.GetHashCode(upperForm));
            Assert.Equal(sc.GetHashCode((object) lowerForm), sc.GetHashCode((object) upperForm));
        }

        [Fact]
        public static void InvariantTest()
        {
            Assert.True(StringComparer.InvariantCulture.Equals("test", "test"), "Same casing strings with StringComparer.InvariantCulture should be equal");
            Assert.True(StringComparer.InvariantCulture.Equals((object) "test", (object) "test"), "Same casing objects with StringComparer.InvariantCulture should be equal");
            Assert.Equal(StringComparer.InvariantCulture.GetHashCode("test"), StringComparer.InvariantCulture.GetHashCode("test"));
            Assert.Equal(0, StringComparer.InvariantCulture.Compare("test", "test"));

            Assert.False(StringComparer.InvariantCulture.Equals("test", "TEST"), "different casing strings with StringComparer.InvariantCulture should not be equal");
            Assert.False(StringComparer.InvariantCulture.Equals((object) "test", (object) "TEST"), "different casing objects with StringComparer.InvariantCulture should not be equal");
            Assert.NotEqual(StringComparer.InvariantCulture.GetHashCode("test"), StringComparer.InvariantCulture.GetHashCode("TEST"));
            Assert.NotEqual(0, StringComparer.InvariantCulture.Compare("test", "TEST"));

            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals("test", "test"), "Same casing strings with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals((object) "test", (object) "test"), "Same casing objects with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.Equal(0, StringComparer.InvariantCultureIgnoreCase.Compare("test", "test"));

            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals("test", "TEST"), "same strings with different casing with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.True(StringComparer.InvariantCultureIgnoreCase.Equals((object) "test", (object) "TEST"), "same objects with different casing with StringComparer.InvariantCultureIgnoreCase should be equal");
            Assert.Equal(StringComparer.InvariantCultureIgnoreCase.GetHashCode("test"), StringComparer.InvariantCultureIgnoreCase.GetHashCode("TEST"));
            Assert.Equal(0, StringComparer.InvariantCultureIgnoreCase.Compare("test", "TEST"));
        }

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

        public static IEnumerable<object[]> CreateFromCultureAndOptionsData()
        {
            yield return new object[] { "abcd", "ABCD", "en-US", CompareOptions.None, false};
            yield return new object[] { "latin i", "LATIN I", "en-US", CompareOptions.None, false};
            yield return new object[] { "turky \u0131", "TURKY I", "tr-TR", CompareOptions.None, false};
            yield return new object[] { "turky i", "TURKY \u0130", "tr-TR", CompareOptions.None, false};
            yield return new object[] { "abcd", "ABCD", "en-US", CompareOptions.IgnoreCase, true};
            yield return new object[] { "latin i", "LATIN I", "en-US", CompareOptions.IgnoreCase, true};

            // Android has its own ICU, which doesn't work well with tr
            if (!PlatformDetection.IsAndroid)
            {
                yield return new object[] { "turky \u0131", "TURKY I", "tr-TR", CompareOptions.IgnoreCase, true};
                yield return new object[] { "turky i", "TURKY \u0130", "tr-TR", CompareOptions.IgnoreCase, true};
            }

            yield return new object[] { "abcd", "ab cd", "en-US", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { "abcd", "ab+cd", "en-US", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { "abcd", "ab%cd", "en-US", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { "abcd", "ab&cd", "en-US", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { "abcd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { "abcd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { "a-bcd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { "abcd*", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true };
            yield return new object[] { "ab$dd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, false };
            yield return new object[] { "abcd", "ab$cd", "en-US", CompareOptions.IgnoreSymbols, true };
        }

        public static TheoryData<string, string, string, CompareOptions, bool> CreateFromCultureAndOptionsStringSortData => new TheoryData<string, string, string, CompareOptions, bool>
        {
            { "abcd", "abcd", "en-US", CompareOptions.StringSort, true },
            { "abcd", "ABcd", "en-US", CompareOptions.StringSort, false },
        };

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(CreateFromCultureAndOptionsData))]
        [MemberData(nameof(CreateFromCultureAndOptionsStringSortData))]
        public static void CreateFromCultureAndOptions(string actualString, string expectedString, string cultureName, CompareOptions options, bool result)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            StringComparer sc = StringComparer.Create(ci, options);

            Assert.Equal(result, sc.Equals(actualString, expectedString));
            Assert.Equal(result, sc.Equals((object)actualString, (object)expectedString));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
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
