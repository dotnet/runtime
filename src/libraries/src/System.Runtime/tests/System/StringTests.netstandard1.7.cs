// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static class StringMiscTests
    {
        public static IEnumerable<object[]> Compare_TestData()
        {
            //                           str1               str2          culture  ignorecase   expected
            yield return new object[] { "abcd",             "ABcd",       "en-US",    false,       -1  };
            yield return new object[] { "ABcd",             "abcd",       "en-US",    false,        1  };
            yield return new object[] { "abcd",             "ABcd",       "en-US",    true,         0  };
            yield return new object[] { "latin i",         "Latin I",     "tr-TR",    false,        1  };
            yield return new object[] { "latin i",         "Latin I",     "tr-TR",    true,         1  };
            yield return new object[] { "turkish \u0130",   "Turkish i",  "tr-TR",    true,         0  };
            yield return new object[] { "turkish \u0131",   "Turkish I",  "tr-TR",    true,         0  };
            yield return new object[] { null,               null,         "en-us",    true,         0  };
            yield return new object[] { null,               "",           "en-us",    true,        -1  };
            yield return new object[] { "",                 null,         "en-us",    true,         1  };
        }

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //                          lower                upper          Culture
            yield return new object[] { "abcd",             "ABCD",         "en-US" };
            yield return new object[] { "latin i",          "LATIN I",      "en-US" };
            yield return new object[] { "turky \u0131",     "TURKY I",      "tr-TR" };
            yield return new object[] { "turky i",          "TURKY \u0130", "tr-TR" };
            yield return new object[] { "\ud801\udc29",     "\ud801\udc01", "en-US" };
        }

        public static IEnumerable<object[]> StartEndWith_TestData()
        {
            //                           str1                    Start      End   Culture  ignorecase   expected
            yield return new object[] { "abcd",                  "AB",      "CD", "en-US",    false,       false  };
            yield return new object[] { "ABcd",                  "ab",      "CD", "en-US",    false,       false  };
            yield return new object[] { "abcd",                  "AB",      "CD", "en-US",    true,        true   };
            yield return new object[] { "i latin i",             "I Latin", "I",  "tr-TR",    false,       false  };
            yield return new object[] { "i latin i",             "I Latin", "I",  "tr-TR",    true,        false  };
            yield return new object[] { "\u0130 turkish \u0130", "i",       "i",  "tr-TR",    true,        true   };
            yield return new object[] { "\u0131 turkish \u0131", "I",       "I",  "tr-TR",    true,        true   };
        }

        [Theory]
        [ActiveIssue(11617, Xunit.PlatformID.AnyUnix)]
        [MemberData(nameof(Compare_TestData))]
        public static void CompareTest(string s1, string s2, string cultureName, bool ignoreCase, int expected)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            CompareOptions ignoreCaseOption = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;

            Assert.Equal(expected, String.Compare(s1, s2, ignoreCase, ci));
            Assert.Equal(expected, String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ignoreCase, ci));
            Assert.Equal(expected, String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ci, ignoreCaseOption));

            Assert.Equal(expected, String.Compare(s1, s2, ci, ignoreCaseOption));
            Assert.Equal(String.Compare(s1, s2, StringComparison.Ordinal), String.Compare(s1, s2, ci, CompareOptions.Ordinal));
            Assert.Equal(String.Compare(s1, s2, StringComparison.OrdinalIgnoreCase), String.Compare(s1, s2, ci, CompareOptions.OrdinalIgnoreCase));
        }

        [Fact]
        [ActiveIssue(11617, Xunit.PlatformID.AnyUnix)]
        public static void CompareNegativeTest()
        {
            Assert.Throws<ArgumentNullException>("culture", () => String.Compare("a", "b", false, null));

            Assert.Throws<ArgumentException>("options", () => String.Compare("a", "b", CultureInfo.InvariantCulture, (CompareOptions) 7891));
            Assert.Throws<ArgumentNullException>("culture", () => String.Compare("a", "b", null, CompareOptions.None));

            Assert.Throws<ArgumentNullException>("culture", () => String.Compare("a", 0, "b", 0, 1, false, null));
            Assert.Throws<ArgumentOutOfRangeException>("length1", () => String.Compare("a", 10,"b", 0, 1, false, CultureInfo.InvariantCulture));
            Assert.Throws<ArgumentOutOfRangeException>("length2", () => String.Compare("a", 1, "b", 10,1, false, CultureInfo.InvariantCulture));
            Assert.Throws<ArgumentOutOfRangeException>("offset1", () => String.Compare("a",-1, "b", 1 ,1, false, CultureInfo.InvariantCulture));
            Assert.Throws<ArgumentOutOfRangeException>("offset2", () => String.Compare("a", 1, "b",-1 ,1, false, CultureInfo.InvariantCulture));
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        [ActiveIssue(11617, Xunit.PlatformID.AnyUnix)]
        public static void CasingTest(string lowerForm, string upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(lowerForm, upperForm.ToLower(ci));
            Assert.Equal(upperForm, lowerForm.ToUpper(ci));
        }

        [Fact]
        [ActiveIssue(11617, Xunit.PlatformID.AnyUnix)]
        public static void CasingNegativeTest()
        {
            Assert.Throws<ArgumentNullException>("culture", () => "".ToLower(null));
            Assert.Throws<ArgumentNullException>("culture", () => "".ToUpper(null));
        }

        [Theory]
        [MemberData(nameof(StartEndWith_TestData))]
        [ActiveIssue(11617, Xunit.PlatformID.AnyUnix)]
        public static void StartEndWithTest(string source, string start, string end, string cultureName, bool ignoreCase, bool expected)
        {
             CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
             Assert.Equal(expected, source.StartsWith(start, ignoreCase, ci));
             Assert.Equal(expected, source.EndsWith(end, ignoreCase, ci));
        }

        [Fact]
        [ActiveIssue(11617, Xunit.PlatformID.AnyUnix)]
        public static void StartEndNegativeTest()
        {
            Assert.Throws<ArgumentNullException>("value", () => "".StartsWith(null, true, null));
            Assert.Throws<ArgumentNullException>("value", () => "".EndsWith(null, true, null));
        }
  }
}