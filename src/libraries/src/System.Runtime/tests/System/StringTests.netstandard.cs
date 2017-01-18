// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
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
            yield return new object[] { "\ud801\udc29",     PlatformDetection.IsWindows7 ? "\ud801\udc29" : "\ud801\udc01", "en-US" };
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

            CultureInfo currentCulture = CultureInfo.CurrentCulture; 
            try 
            {
                CultureInfo.CurrentCulture = ci;
                Assert.Equal(expected, String.Compare(s1, 0, s2, 0, s1 == null ? 0 : s1.Length, ignoreCase));
            }
            finally
            {
                CultureInfo.CurrentCulture = currentCulture;
            }
        }

        [Fact]
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
        public static void CasingTest(string lowerForm, string upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(lowerForm, upperForm.ToLower(ci));
            Assert.Equal(upperForm, lowerForm.ToUpper(ci));
        }

        [Fact]
        public static void CasingNegativeTest()
        {
            Assert.Throws<ArgumentNullException>("culture", () => "".ToLower(null));
            Assert.Throws<ArgumentNullException>("culture", () => "".ToUpper(null));
        }

        [Theory]
        [MemberData(nameof(StartEndWith_TestData))]
        public static void StartEndWithTest(string source, string start, string end, string cultureName, bool ignoreCase, bool expected)
        {
             CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);
             Assert.Equal(expected, source.StartsWith(start, ignoreCase, ci));
             Assert.Equal(expected, source.EndsWith(end, ignoreCase, ci));
        }

        [Fact]
        public static void StartEndNegativeTest()
        {
            Assert.Throws<ArgumentNullException>("value", () => "".StartsWith(null, true, null));
            Assert.Throws<ArgumentNullException>("value", () => "".EndsWith(null, true, null));
        }

        [Fact]
        public static unsafe void ConstructorsTest()
        {
            string s = "This is a string constructor test";
            byte [] encodedBytes = Encoding.Default.GetBytes(s);

            fixed (byte *pBytes = encodedBytes)
            {
                Assert.Equal(s, new String((sbyte*) pBytes));
                Assert.Equal(s, new String((sbyte*) pBytes, 0, encodedBytes.Length));
                Assert.Equal(s, new String((sbyte*) pBytes, 0, encodedBytes.Length, Encoding.Default));
            }

            s = "This is some string \u0393\u0627\u3400\u0440\u1100";
            encodedBytes = Encoding.UTF8.GetBytes(s);

            fixed (byte *pBytes = encodedBytes)
            {
                Assert.Equal(s, new String((sbyte*) pBytes, 0, encodedBytes.Length, Encoding.UTF8));
            }
        }

        [Fact]
        public static unsafe void CloneTest()
        {
            string s = "some string to clone";
            string cloned = (string) s.Clone();
            Assert.Equal(s, cloned);
            Assert.True(Object.ReferenceEquals(s, cloned), "cloned object should return same instance of the string");
        }

        [Fact]
        public static unsafe void CopyTest()
        {
            string s = "some string to copy";
            string copy = String.Copy(s);
            Assert.Equal(s, copy);
            Assert.False(Object.ReferenceEquals(s, copy), "copy should return new instance of the string");
        }

        [Fact]
        public static unsafe void InternTest()
        {
            String s1 = "MyTest";
            String s2 = new StringBuilder().Append("My").Append("Test").ToString(); 
            String s3 = String.Intern(s2);

            Assert.Equal(s1, s2);
            Assert.False(Object.ReferenceEquals(s1, s2), "Created string from StringBuilder should have different reference than the literal string");
            Assert.True(Object.ReferenceEquals(s1, s3), "Created intern string should have same reference as the literal string");

            Assert.True(String.IsInterned(s1).Equals(s1), "Expected to the literal string interned");
            Assert.True(String.IsInterned(s2).Equals(s1), "Expected to the interned string to be in the string pool now");
        }

        [Fact]
        public static unsafe void NormalizationTest()
        {
            // U+0063  LATIN SMALL LETTER C
            // U+0301  COMBINING ACUTE ACCENT
            // U+0327  COMBINING CEDILLA
            // U+00BE  VULGAR FRACTION THREE QUARTERS            
            string s = new String( new char[] {'\u0063', '\u0301', '\u0327', '\u00BE'});

            Assert.False(s.IsNormalized(), "String should be not normalized when checking with the default which same as FormC");
            Assert.False(s.IsNormalized(NormalizationForm.FormC), "String should be not normalized when checking with FormC");
            Assert.False(s.IsNormalized(NormalizationForm.FormD), "String should be not normalized when checking with FormD");
            Assert.False(s.IsNormalized(NormalizationForm.FormKC), "String should be not normalized when checking with FormKC");
            Assert.False(s.IsNormalized(NormalizationForm.FormKD), "String should be not normalized when checking with FormKD");

            string normalized = s.Normalize(); // FormC
            Assert.True(normalized.IsNormalized(), "Expected to have the normalized string with default form FormC");
            Assert.True(normalized.IsNormalized(NormalizationForm.FormC), "Expected to have the normalized string with FormC");
            
            normalized = s.Normalize(NormalizationForm.FormC);
            Assert.True(normalized.IsNormalized(), "Expected to have the normalized string with default form FormC when using NormalizationForm.FormC");
            Assert.True(normalized.IsNormalized(NormalizationForm.FormC), "Expected to have the normalized string with FormC when using NormalizationForm.FormC");

            normalized = s.Normalize(NormalizationForm.FormD);
            Assert.True(normalized.IsNormalized(NormalizationForm.FormD), "Expected to have the normalized string with FormD");

            normalized = s.Normalize(NormalizationForm.FormKC);
            Assert.True(normalized.IsNormalized(NormalizationForm.FormKC), "Expected to have the normalized string with FormKC");

            normalized = s.Normalize(NormalizationForm.FormKD);
            Assert.True(normalized.IsNormalized(NormalizationForm.FormKD), "Expected to have the normalized string with FormKD");
        }

        [Fact]
        public static unsafe void GetEnumeratorTest()
        {
            string s = "This is some string to enumerate its characters using String.GetEnumerator";
            CharEnumerator chEnum = s.GetEnumerator();

            int calculatedLength = 0;
            while (chEnum.MoveNext())
            {
                calculatedLength++;
            }

            Assert.Equal(s.Length, calculatedLength);
            chEnum.Reset();

            // enumerate twice in same time
            foreach (char c in s)
            {
                Assert.True(chEnum.MoveNext(), "expect to have characters to enumerate in the string");
                Assert.Equal(c, chEnum.Current); 
            }

            Assert.False(chEnum.MoveNext(), "expect to not having any characters to enumerate");
        }
    }
}