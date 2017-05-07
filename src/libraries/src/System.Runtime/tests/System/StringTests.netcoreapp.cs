// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public partial class StringTests
    {
        [Theory]
        [InlineData("Hello", 'o', true)]
        [InlineData("Hello", 'O', false)]
        [InlineData("o", 'o', true)]
        [InlineData("o", 'O', false)]
        [InlineData("Hello", 'e', false)]
        [InlineData("Hello", '\0', false)]
        [InlineData("", '\0', false)]
        [InlineData("\0", '\0', true)]
        [InlineData("", 'a', false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", 'z', true)]
        public static void EndsWith(string s, char value, bool expected)
        {
            Assert.Equal(expected, s.EndsWith(value));
        }

        [Theory]
        [InlineData("Hello", 'H', true)]
        [InlineData("Hello", 'h', false)]
        [InlineData("H", 'H', true)]
        [InlineData("H", 'h', false)]
        [InlineData("Hello", 'e', false)]
        [InlineData("Hello", '\0', false)]
        [InlineData("", '\0', false)]
        [InlineData("\0", '\0', true)]
        [InlineData("", 'a', false)]
        [InlineData("abcdefghijklmnopqrstuvwxyz", 'a', true)]
        public static void StartsWith(string s, char value, bool expected)
        {
            Assert.Equal(expected, s.StartsWith(value));
        }

        public static IEnumerable<object[]> Join_Char_StringArray_TestData()
        {
            yield return new object[] { '|', new string[0], 0, 0, "" };
            yield return new object[] { '|', new string[] { "a" }, 0, 1, "a" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 3, "a|b|c" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 2, "a|b" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 1, 1, "b" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 1, 2, "b|c" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 3, 0, "" };
            yield return new object[] { '|', new string[] { "a", "b", "c" }, 0, 0, "" };
            yield return new object[] { '|', new string[] { "", "", "" }, 0, 3, "||" };
            yield return new object[] { '|', new string[] { null, null, null }, 0, 3, "||" };
        }

        [Theory]
        [MemberData(nameof(Join_Char_StringArray_TestData))]
        public static void Join_Char_StringArray(char separator, string[] values, int startIndex, int count, string expected)
        {
            if (startIndex == 0 && count == values.Length)
            {
                Assert.Equal(expected, string.Join(separator, values));
                Assert.Equal(expected, string.Join(separator, (IEnumerable<string>)values));
                Assert.Equal(expected, string.Join(separator, (object[])values));
                Assert.Equal(expected, string.Join(separator, (IEnumerable<object>)values));
            }

            Assert.Equal(expected, string.Join(separator, values, startIndex, count));
            Assert.Equal(expected, string.Join(separator.ToString(), values, startIndex, count));
        }

        public static IEnumerable<object[]> Join_Char_ObjectArray_TestData()
        {
            yield return new object[] { '|', new object[0], "" };
            yield return new object[] { '|', new object[] { 1 }, "1" };
            yield return new object[] { '|', new object[] { 1, 2, 3 }, "1|2|3" };
            yield return new object[] { '|', new object[] { new ObjectWithNullToString(), 2, new ObjectWithNullToString() }, "|2|" };
            yield return new object[] { '|', new object[] { "1", null, "3" }, "1||3" };
            yield return new object[] { '|', new object[] { "", "", "" }, "||" };
            yield return new object[] { '|', new object[] { "", null, "" }, "||" };
            yield return new object[] { '|', new object[] { null, null, null }, "||" };
        }

        [Theory]
        [MemberData(nameof(Join_Char_ObjectArray_TestData))]
        public static void Join_Char_ObjectArray(char separator, object[] values, string expected)
        {
            Assert.Equal(expected, string.Join(separator, values));
            Assert.Equal(expected, string.Join(separator, (IEnumerable<object>)values));
        }

        [Fact]
        public static void Join_Char_NullValues_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => string.Join('|', (string[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => string.Join('|', (string[])null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join('|', (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => string.Join('|', (IEnumerable<object>)null));
        }

        [Fact]
        public static void Join_Char_NegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join('|', new string[] { "Foo" }, -1, 0));
        }

        [Fact]
        public static void Join_Char_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => string.Join('|', new string[] { "Foo" }, 0, -1));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(2, 0)]
        [InlineData(1, 2)]
        [InlineData(1, 1)]
        [InlineData(0, 2)]
        [InlineData(-1, 0)]
        public static void Join_Char_InvalidStartIndexCount_ThrowsArgumentOutOfRangeException(int startIndex, int count)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => string.Join('|', new string[] { "Foo" }, startIndex, count));
        }

        public static IEnumerable<object[]> Replace_StringComparison_TestData()
        {
            yield return new object[] { "abc", "abc", "def", StringComparison.CurrentCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.CurrentCulture, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.CurrentCulture, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.CurrentCulture, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.CurrentCulture, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.CurrentCulture, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.CurrentCulture, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.CurrentCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.CurrentCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.CurrentCultureIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.CurrentCultureIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.CurrentCultureIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.CurrentCultureIgnoreCase, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.CurrentCultureIgnoreCase, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.Ordinal, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.Ordinal, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.Ordinal, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.Ordinal, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.Ordinal, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.Ordinal, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.Ordinal, "abc" };

            yield return new object[] { "abc", "abc", "def", StringComparison.OrdinalIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.OrdinalIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.OrdinalIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.OrdinalIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.OrdinalIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.OrdinalIgnoreCase, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.OrdinalIgnoreCase, "abc" };

            yield return new object[] { "abc", "abc", "def", StringComparison.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.InvariantCulture, "abc" };
            yield return new object[] { "abc", "abc", "", StringComparison.InvariantCulture, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.InvariantCulture, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.InvariantCulture, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.InvariantCulture, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.InvariantCulture, "def" };

            yield return new object[] { "abc", "abc", "def", StringComparison.InvariantCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "ABC", "def", StringComparison.InvariantCultureIgnoreCase, "def" };
            yield return new object[] { "abc", "abc", "", StringComparison.InvariantCultureIgnoreCase, "" };
            yield return new object[] { "abc", "b", "LONG", StringComparison.InvariantCultureIgnoreCase, "aLONGc" };
            yield return new object[] { "abc", "b", "d", StringComparison.InvariantCultureIgnoreCase, "adc" };
            yield return new object[] { "abc", "b", null, StringComparison.InvariantCultureIgnoreCase, "ac" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", StringComparison.InvariantCultureIgnoreCase, "def" };

            string turkishSource = "\u0069\u0130";

            yield return new object[] { turkishSource, "\u0069", "a", StringComparison.Ordinal, "a\u0130" };
            yield return new object[] { turkishSource, "\u0069", "a", StringComparison.OrdinalIgnoreCase, "a\u0130" };
            yield return new object[] { turkishSource, "\u0130", "a", StringComparison.Ordinal, "\u0069a" };
            yield return new object[] { turkishSource, "\u0130", "a", StringComparison.OrdinalIgnoreCase, "\u0069a" };

            yield return new object[] { turkishSource, "\u0069", "a", StringComparison.InvariantCulture, "a\u0130" };
            yield return new object[] { turkishSource, "\u0069", "a", StringComparison.InvariantCultureIgnoreCase, "a\u0130" };
            yield return new object[] { turkishSource, "\u0130", "a", StringComparison.InvariantCulture, "\u0069a" };
            yield return new object[] { turkishSource, "\u0130", "a", StringComparison.InvariantCultureIgnoreCase, "\u0069a" };
        }

        [Theory]
        [MemberData(nameof(Replace_StringComparison_TestData))]
        public void Replace_StringComparison_ReturnsExpected(string original, string oldValue, string newValue, StringComparison comparisonType, string expected)
        {
            Assert.Equal(expected, original.Replace(oldValue, newValue, comparisonType));
        }

        [Fact]
        public void Replace_StringComparison_TurkishI()
        {
            string source = "\u0069\u0130";
            Helpers.PerformActionWithCulture(new CultureInfo("tr-TR"), () =>
            {
                Assert.True("\u0069".Equals("\u0130", StringComparison.CurrentCultureIgnoreCase));
            
                Assert.Equal("a\u0130", source.Replace("\u0069", "a", StringComparison.CurrentCulture));
                Assert.Equal("aa", source.Replace("\u0069", "a", StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal("\u0069a", source.Replace("\u0130", "a", StringComparison.CurrentCulture));
                Assert.Equal("aa", source.Replace("\u0130", "a", StringComparison.CurrentCultureIgnoreCase));
            });

            Helpers.PerformActionWithCulture(new CultureInfo("en-US"), () =>
            {
                Assert.False("\u0069".Equals("\u0130", StringComparison.CurrentCultureIgnoreCase));

                Assert.Equal("a\u0130", source.Replace("\u0069", "a", StringComparison.CurrentCulture));
                Assert.Equal("a\u0130", source.Replace("\u0069", "a", StringComparison.CurrentCultureIgnoreCase));
                Assert.Equal("\u0069a", source.Replace("\u0130", "a", StringComparison.CurrentCulture));
                Assert.Equal("\u0069a", source.Replace("\u0130", "a", StringComparison.CurrentCultureIgnoreCase));
            });
        }

        public static IEnumerable<object[]> Replace_StringComparisonCulture_TestData()
        {
            yield return new object[] { "abc", "abc", "def", false, null, "def" };
            yield return new object[] { "abc", "ABC", "def", false, null, "abc" };
            yield return new object[] { "abc", "abc", "def", false, CultureInfo.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", false, CultureInfo.InvariantCulture, "abc" };

            yield return new object[] { "abc", "abc", "def", true, null, "def" };
            yield return new object[] { "abc", "ABC", "def", true, null, "def" };
            yield return new object[] { "abc", "abc", "def", true, CultureInfo.InvariantCulture, "def" };
            yield return new object[] { "abc", "ABC", "def", true, CultureInfo.InvariantCulture, "def" };

            yield return new object[] { "abc", "abc" + SoftHyphen, "def", false, null, "def" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", true, null, "def" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", false, CultureInfo.InvariantCulture, "def" };
            yield return new object[] { "abc", "abc" + SoftHyphen, "def", true, CultureInfo.InvariantCulture, "def" };

            yield return new object[] { "\u0069\u0130", "\u0069", "a", false, new CultureInfo("tr-TR"), "a\u0130" };
            yield return new object[] { "\u0069\u0130", "\u0069", "a", true, new CultureInfo("tr-TR"), "aa" };
            yield return new object[] { "\u0069\u0130", "\u0069", "a", false, CultureInfo.InvariantCulture, "a\u0130" };
            yield return new object[] { "\u0069\u0130", "\u0069", "a", true, CultureInfo.InvariantCulture, "a\u0130" };
        }

        [Theory]
        [MemberData(nameof(Replace_StringComparisonCulture_TestData))]
        public void Replace_StringComparisonCulture_ReturnsExpected(string original, string oldValue, string newValue, bool ignoreCase, CultureInfo culture, string expected)
        {
            Assert.Equal(expected, original.Replace(oldValue, newValue, ignoreCase, culture));
            if (culture == null)
            {
                Assert.Equal(expected, original.Replace(oldValue, newValue, ignoreCase, CultureInfo.CurrentCulture));
            }
        }

        [Fact]
        public void Replace_StringComparison_NullOldValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>("oldValue", () => "abc".Replace(null, "def", StringComparison.CurrentCulture));
            Assert.Throws<ArgumentNullException>("oldValue", () => "abc".Replace(null, "def", true, CultureInfo.CurrentCulture));
        }

        [Fact]
        public void Replace_StringComparison_EmptyOldValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("oldValue", () => "abc".Replace("", "def", StringComparison.CurrentCulture));
            Assert.Throws<ArgumentException>("oldValue", () => "abc".Replace("", "def", true, CultureInfo.CurrentCulture));
        }

        [Theory]
        [InlineData(StringComparison.CurrentCulture - 1)]
        [InlineData(StringComparison.OrdinalIgnoreCase + 1)]
        public void Replace_NoSuchStringComparison_ThrowsArgumentException(StringComparison comparisonType)
        {
            Assert.Throws<ArgumentException>("comparisonType", () => "abc".Replace("abc", "def", comparisonType));
        }


        private static readonly StringComparison[] StringComparisons = (StringComparison[])Enum.GetValues(typeof(StringComparison));


        public static IEnumerable<object[]> GetHashCode_StringComparison_Data => StringComparisons.Select(value => new object[] { value });

        [Theory]
        [MemberData(nameof(GetHashCode_StringComparison_Data))]
        public static void GetHashCode_StringComparison(StringComparison comparisonType)
        {
            Assert.Equal(StringComparer.FromComparison(comparisonType).GetHashCode("abc"), "abc".GetHashCode(comparisonType));
        }


        public static IEnumerable<object[]> GetHashCode_NoSuchStringComparison_ThrowsArgumentException_Data => new[]
        {
            new object[] { StringComparisons.Min() - 1 },
            new object[] { StringComparisons.Max() + 1 },
        };

        [Theory]
        [MemberData(nameof(GetHashCode_NoSuchStringComparison_ThrowsArgumentException_Data))]
        public static void GetHashCode_NoSuchStringComparison_ThrowsArgumentException(StringComparison comparisonType)
        {
            Assert.Throws<ArgumentException>("comparisonType", () => "abc".GetHashCode(comparisonType));
        }
    }
}
