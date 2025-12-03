// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Memory.Tests.Span
{
    public static class StringSearchValuesTests
    {
        public static bool CanTestInvariantCulture => RemoteExecutor.IsSupported;
        public static bool CanTestNls => RemoteExecutor.IsSupported && OperatingSystem.IsWindows();

        [Theory]
        [InlineData(StringComparison.Ordinal, "a")]
        [InlineData(StringComparison.Ordinal, "A")]
        [InlineData(StringComparison.Ordinal, "a", "ab", "abc", "bc")]
        [InlineData(StringComparison.Ordinal, "A", "ab", "aBc", "Bc")]
        [InlineData(StringComparison.OrdinalIgnoreCase, "a")]
        [InlineData(StringComparison.OrdinalIgnoreCase, "A")]
        [InlineData(StringComparison.OrdinalIgnoreCase, "A", "a")]
        [InlineData(StringComparison.OrdinalIgnoreCase, "Ab", "Abc")]
        [InlineData(StringComparison.OrdinalIgnoreCase, "a", "Ab", "abc", "bC")]
        public static void Values_ImplementsSearchValuesBase(StringComparison comparisonType, params string[] values)
        {
            const string ValueNotInSet = "Hello world";

            SearchValues<string> stringValues = SearchValues.Create(values, comparisonType);

            Assert.False(stringValues.Contains(ValueNotInSet));

            AssertIndexOfAnyAndFriends(Span<string>.Empty, -1, -1, -1, -1);
            AssertIndexOfAnyAndFriends(new[] { ValueNotInSet }, -1, 0, -1, 0);
            AssertIndexOfAnyAndFriends(new[] { ValueNotInSet, ValueNotInSet }, -1, 0, -1, 1);

            foreach (string value in values)
            {
                Assert.True(stringValues.Contains(value));

                string differentCase = value.ToLowerInvariant();
                if (value == differentCase)
                {
                    differentCase = value.ToUpperInvariant();
                }

                if (value != differentCase)
                {
                    Assert.Equal(comparisonType == StringComparison.OrdinalIgnoreCase, stringValues.Contains(differentCase));
                }

                AssertIndexOfAnyAndFriends(new[] { value }, 0, -1, 0, -1);
                AssertIndexOfAnyAndFriends(new[] { value, value }, 0, -1, 1, -1);
                AssertIndexOfAnyAndFriends(new[] { value, ValueNotInSet }, 0, 1, 0, 1);
                AssertIndexOfAnyAndFriends(new[] { value, ValueNotInSet, ValueNotInSet }, 0, 1, 0, 2);
                AssertIndexOfAnyAndFriends(new[] { ValueNotInSet, value }, 1, 0, 1, 0);
                AssertIndexOfAnyAndFriends(new[] { ValueNotInSet, ValueNotInSet, value }, 2, 0, 2, 1);
                AssertIndexOfAnyAndFriends(new[] { ValueNotInSet, value, ValueNotInSet }, 1, 0, 1, 2);
                AssertIndexOfAnyAndFriends(new[] { value, ValueNotInSet, value }, 0, 1, 2, 1);

                if (comparisonType == StringComparison.OrdinalIgnoreCase)
                {
                    AssertIndexOfAnyAndFriends(new[] { differentCase }, 0, -1, 0, -1);
                    AssertIndexOfAnyAndFriends(new[] { differentCase, differentCase }, 0, -1, 1, -1);
                    AssertIndexOfAnyAndFriends(new[] { differentCase, ValueNotInSet }, 0, 1, 0, 1);
                    AssertIndexOfAnyAndFriends(new[] { differentCase, ValueNotInSet, ValueNotInSet }, 0, 1, 0, 2);
                    AssertIndexOfAnyAndFriends(new[] { ValueNotInSet, differentCase }, 1, 0, 1, 0);
                    AssertIndexOfAnyAndFriends(new[] { ValueNotInSet, ValueNotInSet, differentCase }, 2, 0, 2, 1);
                    AssertIndexOfAnyAndFriends(new[] { ValueNotInSet, differentCase, ValueNotInSet }, 1, 0, 1, 2);
                    AssertIndexOfAnyAndFriends(new[] { differentCase, ValueNotInSet, differentCase }, 0, 1, 2, 1);
                }
                else if (value != differentCase)
                {
                    AssertIndexOfAnyAndFriends(new[] { differentCase }, -1, 0, -1, 0);
                    AssertIndexOfAnyAndFriends(new[] { differentCase, differentCase }, -1, 0, -1, 1);
                    AssertIndexOfAnyAndFriends(new[] { differentCase, ValueNotInSet }, -1, 0, -1, 1);
                    AssertIndexOfAnyAndFriends(new[] { ValueNotInSet, differentCase }, -1, 0, -1, 1);
                    AssertIndexOfAnyAndFriends(new[] { differentCase, ValueNotInSet, ValueNotInSet }, -1, 0, -1, 2);
                }
            }

            void AssertIndexOfAnyAndFriends(Span<string> values, int any, int anyExcept, int last, int lastExcept)
            {
                Assert.Equal(any >= 0, last >= 0);
                Assert.Equal(anyExcept >= 0, lastExcept >= 0);

                Assert.Equal(any, values.IndexOfAny(stringValues));
                Assert.Equal(any, ((ReadOnlySpan<string>)values).IndexOfAny(stringValues));
                Assert.Equal(anyExcept, values.IndexOfAnyExcept(stringValues));
                Assert.Equal(anyExcept, ((ReadOnlySpan<string>)values).IndexOfAnyExcept(stringValues));
                Assert.Equal(last, values.LastIndexOfAny(stringValues));
                Assert.Equal(last, ((ReadOnlySpan<string>)values).LastIndexOfAny(stringValues));
                Assert.Equal(lastExcept, values.LastIndexOfAnyExcept(stringValues));
                Assert.Equal(lastExcept, ((ReadOnlySpan<string>)values).LastIndexOfAnyExcept(stringValues));

                Assert.Equal(any >= 0, values.ContainsAny(stringValues));
                Assert.Equal(any >= 0, ((ReadOnlySpan<string>)values).ContainsAny(stringValues));
                Assert.Equal(anyExcept >= 0, values.ContainsAnyExcept(stringValues));
                Assert.Equal(anyExcept >= 0, ((ReadOnlySpan<string>)values).ContainsAnyExcept(stringValues));
            }
        }

        [Theory]
        // Sets with empty values
        [InlineData(StringComparison.Ordinal, 0, " ", "abc, ")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 0, " ", "abc, ")]
        [InlineData(StringComparison.Ordinal, 0, "", "")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 0, "", "abc, ")]
        // Empty sets
        [InlineData(StringComparison.Ordinal, -1, " ", null)]
        [InlineData(StringComparison.OrdinalIgnoreCase, -1, " ", null)]
        [InlineData(StringComparison.Ordinal, -1, "", null)]
        [InlineData(StringComparison.OrdinalIgnoreCase, -1, "", null)]
        // A few simple cases
        [InlineData(StringComparison.Ordinal, 1, "xbc", "abc, bc")]
        [InlineData(StringComparison.Ordinal, 0, "foobar", "foo, bar")]
        [InlineData(StringComparison.Ordinal, 0, "barfoo", "foo, bar")]
        [InlineData(StringComparison.Ordinal, 0, "foofoo", "foo, bar")]
        [InlineData(StringComparison.Ordinal, 0, "barbar", "foo, bar")]
        [InlineData(StringComparison.Ordinal, 4, "bafofoo", "foo, bar")]
        [InlineData(StringComparison.Ordinal, 4, "bafofoo", "bar, foo")]
        [InlineData(StringComparison.Ordinal, 4, "fobabar", "foo, bar")]
        [InlineData(StringComparison.Ordinal, 4, "fobabar", "bar, foo")]
        // Multiple potential matches - we want the first one
        [InlineData(StringComparison.Ordinal, 1, "abcd", "bc, cd")]
        // Simple case sensitivity
        [InlineData(StringComparison.Ordinal, -1, " ABC", "abc")]
        [InlineData(StringComparison.Ordinal, 1, " abc", "abc")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 1, " ABC", "abc")]
        // A few more complex cases that test the Aho-Corasick implementation
        [InlineData(StringComparison.Ordinal, 3, "RyrIGEdt2S9", "IGEdt2, G, rIGm6i")]
        [InlineData(StringComparison.Ordinal, 2, "Npww1HtmO", "NVOhQu, w, XeR")]
        [InlineData(StringComparison.Ordinal, 1, "08Qq6", "8, vx, BFA4s, aLP2, hm, lmT, y, CNTB, Q, vd")]
        [InlineData(StringComparison.Ordinal, 3, "A4sRYUhKZR1Vn8N", "F, scsx, nWBhrx, Q, 7Of, BX, huoJ, R")]
        [InlineData(StringComparison.Ordinal, 9, "40sufu3TdzcKQfK", "3MXvo26, zPd6t, zc, c5, ypUCK3A9, K, YlX")]
        [InlineData(StringComparison.Ordinal, 0, "111KtTGeWuV", "11, B51tJ, Z, j0DWudC, kuJRbcovn, 0T2vnT9")]
        [InlineData(StringComparison.Ordinal, 5, "Uykbt1zWw7wylEgC", "1zWw7, Bh, 7qDgAY, w, Z, dP, V, W, Hiols, T")]
        [InlineData(StringComparison.Ordinal, 6, "PI9yZx9AOWrUR", "4, A, MLbg, jACE, x9AZEYPbLr, 4bYTzw, W, 9AOW, O")]
        [InlineData(StringComparison.Ordinal, 7, "KV4cRyrIGEdt2S9kbXVK", "e64, 10Yw7k, IGEdt2, G, brL, rIGm6i, Z3, FHoVN, 7P2s")]
        // OrdinalIgnoreCase does not match ASCII chars with non-ASCII ones
        [InlineData(StringComparison.OrdinalIgnoreCase, 4, "AAAA\u212ABKBkBBCCCC", "\u212A")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 6, "AAAAKB\u212ABkBBCCCC", "\u212A")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 6, "AAAAkB\u212ABKBBCCCC", "\u212A")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 4, "AAAA\u017FBSBsBBCCCC", "\u017F")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 6, "AAAASB\u017FBsBBCCCC", "\u017F")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 6, "AAAAsB\u017FBSBBCCCC", "\u017F")]
        // A few misc non-ASCII examples
        [InlineData(StringComparison.OrdinalIgnoreCase, 2, "\0\u1226\u2C5F\0\n\0\u1226\u1242", "hh\u0012\uFE00\u26FF\0\u6C00\u2C00\0b, \u2C5F\0")]
        [InlineData(StringComparison.OrdinalIgnoreCase, -1, "barkbarK", "foo, bar\u212A")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 4, "bar\u212AbarK", "foo, bark")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 0, "bar\u03A3barK", "foo, bar\u03C3")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 1, "bar\u03A3barK", "foo, ar\u03C3")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 1, " foo\u0131", "foo\u0131")]
        [InlineData(StringComparison.OrdinalIgnoreCase, 1, " foo\u0131", "bar, foo\u0131")]
        [InlineData(StringComparison.OrdinalIgnoreCase, -1, "fooifooIfoo\u0130", "bar, foo\u0131")]
        [InlineData(StringComparison.OrdinalIgnoreCase, -1, "fooifooIfoo\u0131", "bar, foo\u0130")]
        public static void IndexOfAny(StringComparison comparisonType, int expected, string text, string? values)
        {
            Span<char> textSpan = text.ToArray(); // Test non-readonly Span<char> overloads

            string[] valuesArray = values is null ? Array.Empty<string>() : values.Split(", ");

            SearchValues<string> stringValues = SearchValues.Create(valuesArray, comparisonType);

            Assert.Equal(expected, IndexOfAnyReferenceImpl(text, valuesArray, comparisonType));

            Assert.Equal(expected, text.AsSpan().IndexOfAny(stringValues));
            Assert.Equal(expected, textSpan.IndexOfAny(stringValues));

            Assert.Equal(expected >= 0, text.AsSpan().ContainsAny(stringValues));
            Assert.Equal(expected >= 0, textSpan.ContainsAny(stringValues));

            if (values is null || stringValues.Contains(string.Empty))
            {
                // The tests below don't work if an empty string is in the set.
                return;
            }

            // The tests below assume none of the values contain these characters.
            Assert.Equal(-1, IndexOfAnyReferenceImpl(new string('\0', 100), valuesArray, comparisonType));
            Assert.Equal(-1, IndexOfAnyReferenceImpl(new string('\u00FC', 100), valuesArray, comparisonType));

            string[] valuesWithDifferentCases = valuesArray;

            if (comparisonType == StringComparison.OrdinalIgnoreCase)
            {
                valuesWithDifferentCases = valuesArray
                    .SelectMany(v => new[] { v, v.ToUpperInvariant(), v.ToLowerInvariant() })
                    .Distinct()
                    // Invariant conversions may produce values that don't match under ordinal rules. Filter them out.
                    .Where(v => valuesArray.Any(original => v.Equals(original, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
            }

            // Test cases where the implementation changes based on the haystack length (e.g. swapping from Teddy to Rabin-Karp).
            for (int haystackLength = 0; haystackLength < 50; haystackLength++)
            {
                TestWithPoisonPages(PoisonPagePlacement.Before, haystackLength);
                TestWithPoisonPages(PoisonPagePlacement.After, haystackLength);
            }

            void TestWithPoisonPages(PoisonPagePlacement poisonPlacement, int haystackLength)
            {
                using BoundedMemory<char> memory = BoundedMemory.Allocate<char>(haystackLength, poisonPlacement);
                Span<char> haystack = memory.Span;

                char asciiNumberNotInSet = Enumerable.Range('0', 10).Select(c => (char)c)
                    .First(c => !values.Contains(c));

                char asciiLetterLowerNotInSet;
                char asciiLetterUpperNotInSet;

                if (comparisonType == StringComparison.Ordinal)
                {
                    asciiLetterLowerNotInSet = Enumerable.Range('a', 26).Select(c => (char)c).First(c => !values.Contains(c));
                    asciiLetterUpperNotInSet = Enumerable.Range('A', 26).Select(c => (char)c).First(c => !values.Contains(c));
                }
                else
                {
                    asciiLetterLowerNotInSet = Enumerable.Range('a', 26).Select(c => (char)c)
                        .First(c => !values.AsSpan().ContainsAny(c, char.ToUpperInvariant(c)));

                    asciiLetterUpperNotInSet = Enumerable.Range(0, 26).Select(c => (char)('Z' - c))
                        .First(c => !values.AsSpan().ContainsAny(c, char.ToLowerInvariant(c)));
                }

                TestWithDifferentMarkerChars(haystack, '\0');
                TestWithDifferentMarkerChars(haystack, '\u00FC');
                TestWithDifferentMarkerChars(haystack, asciiNumberNotInSet);
                TestWithDifferentMarkerChars(haystack, asciiLetterLowerNotInSet);
                TestWithDifferentMarkerChars(haystack, asciiLetterUpperNotInSet);
            }

            void TestWithDifferentMarkerChars(Span<char> haystack, char marker)
            {
                haystack.Fill(marker);
                Assert.True(haystack.IndexOfAny(stringValues) == -1, marker.ToString());

                string shortestValue = valuesArray.MinBy(value => value.Length);

                // Test every value individually at every offset in the haystack.
                foreach (string value in valuesWithDifferentCases)
                {
                    for (int startOffset = 0; startOffset <= haystack.Length - value.Length; startOffset++)
                    {
                        haystack.Fill(marker);

                        // Place an unrelated matching value at the end of the haystack. It shouldn't affect the result.
                        shortestValue.CopyTo(haystack.Slice(haystack.Length - shortestValue.Length));

                        // Place a matching value at the offset position.
                        value.CopyTo(haystack.Slice(startOffset));

                        int actual = haystack.IndexOfAny(stringValues);
                        if (startOffset != actual)
                        {
                            StringSearchValuesTestHelper.AssertionFailed(haystack, valuesArray, stringValues, comparisonType, startOffset, actual);
                        }
                    }
                }

                if (text == valuesArray[0])
                {
                    // Already tested above.
                    return;
                }

                // Test the provided test case at various offsets in the haystack.
                for (int startOffset = 0; startOffset <= haystack.Length - text.Length; startOffset++)
                {
                    haystack.Fill(marker);

                    // Place the test case text at the end of the haystack. It shouldn't affect the result.
                    text.CopyTo(haystack.Slice(haystack.Length - text.Length));

                    // Place the test text at the offset position.
                    text.CopyTo(haystack.Slice(startOffset));

                    int expectedAtOffset = expected == -1 ? -1 : startOffset + expected;

                    int actual = haystack.IndexOfAny(stringValues);
                    if (expectedAtOffset != actual)
                    {
                        StringSearchValuesTestHelper.AssertionFailed(haystack, valuesArray, stringValues, comparisonType, expectedAtOffset, actual);
                    }
                }
            }
        }

        [Fact]
        public static void IndexOfAny_InvalidUtf16()
        {
            // Not using [InlineData] to prevent Xunit from modifying the invalid strings.
            // These strings have a high surrogate without the full pair.
            IndexOfAny(StringComparison.Ordinal, 1, " foo\uD800bar", "foo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.Ordinal, -1, " foo\uD801bar", "foo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.Ordinal, 2, " foo\uD800bar", "oo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.Ordinal, -1, " foo\uD801bar", "oo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.OrdinalIgnoreCase, 1, " foo\uD800bar", "foo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.OrdinalIgnoreCase, -1, " foo\uD801bar", "foo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.OrdinalIgnoreCase, 2, " foo\uD800bar", "oo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.OrdinalIgnoreCase, -1, " foo\uD801bar", "oo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.OrdinalIgnoreCase, 1, " fOo\uD800bar", "Foo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.OrdinalIgnoreCase, -1, " fOo\uD801bar", "Foo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.OrdinalIgnoreCase, 2, " foo\uD800bAr", "Oo\uD800bar, bar\uD800foo");
            IndexOfAny(StringComparison.OrdinalIgnoreCase, -1, " foO\uD801bar", "oo\uD800baR, bar\uD800foo");

            // Low surrogate without the high surrogate.
            IndexOfAny(StringComparison.OrdinalIgnoreCase, 1, "\uD801\uDCD8\uD8FB\uDCD8", "foo, \uDCD8");
        }

        [Theory]
        // Single value of various lengths
        [InlineData("a")]
        [InlineData("!")]
        [InlineData("\u00F6")]
        [InlineData("ab")]
        [InlineData("a!")]
        [InlineData("!a")]
        [InlineData("!%")]
        [InlineData("a\u00F6")]
        [InlineData("\u00F6\u00F6")]
        [InlineData("abc")]
        [InlineData("ab!")]
        [InlineData("a\u00F6b")]
        [InlineData("\u00F6a\u00F6")]
        [InlineData("abcd")]
        [InlineData("ab!cd")]
        [InlineData("abcde")]
        [InlineData("abcd!")]
        [InlineData("abcdefgh")]
        [InlineData("abcdefghi")]
        // Multiple values, but they all share the same prefix
        [InlineData("abc", "ab", "abcd")]
        // These should hit the Aho-Corasick implementation
        [InlineData("a", "b")]
        [InlineData("ab", "c")]
        // Simple Teddy cases
        [InlineData("abc", "cde")]
        [InlineData("abc", "cd")]
        // Teddy where all starting chars are letters, but not all other characters are
        [InlineData("ab", "de%", "ghi", "jkl!")]
        [InlineData("abc", "def%", "ghi", "jkl!")]
        // Teddy where starting chars aren't only letters
        [InlineData("ab", "d%e", "ghi", "jkl!")]
        [InlineData("abc", "def%", "ghi", "!jkl")]
        // Teddy where the starting chars aren't affected by case conversion
        [InlineData("12", "45b", "789")]
        [InlineData("123", "456", "789")]
        [InlineData("123", "456a", "789b")]
        // We'll expand these values to all case permutations
        [InlineData("ab", "bc")]
        [InlineData("ab", "c!")]
        [InlineData("ab", "c!", "!%")]
        // These won't be expanded as they would produce more than 8 permutations
        [InlineData("ab", "bc", "c!")]
        [InlineData("abc", "bc")]
        // Rabin-Karp where one of the values is longer than what the implementation can match (17)
        [InlineData("abc", "a012345678012345678")]
        // Rabin-Karp where all of the values are longer than what the implementation can match (17)
        [InlineData("a012345678012345678", "bc012345678012345678")]
        // Teddy with exactly 8 values (filling all 8 buckets)
        [InlineData("ab", "bc", "def", "ghi", "jkl", "mno", "pqr", "stu")]
        [InlineData("abc", "def", "ghi", "jkl", "mno", "pqr", "stu", "vwx")]
        // Teddy with more than 8 values
        [InlineData("ab", "bc", "def", "ghi", "jkl", "mno", "pqr", "stu", "vwx")]
        [InlineData("abc", "def", "ghi", "jkl", "mno", "pqr", "stu", "vwx", "yab")]
        public static void SimpleIndexOfAnyValues(params string[] valuesArray)
        {
            TestCore(valuesArray);

            // Test cases where the implementation differs for ASCII letters, different cases, non-letters.
            if (valuesArray.Any(v => v.Contains('a')))
            {
                TestCore(valuesArray.Select(v => v.Replace('a', 'A')).ToArray());
                TestCore(valuesArray.Select(v => v.Replace('a', '7')).ToArray());
            }

            int offset = valuesArray.Length / 2;
            string original = valuesArray[offset];

            // Test non-ASCII values
            valuesArray[offset] = $"{original}\u00F6";
            TestCore(valuesArray);

            valuesArray[offset] = $"\u00F6{original}";
            TestCore(valuesArray);

            valuesArray[offset] = $"{original[0]}\u00F6{original.AsSpan(1)}";
            TestCore(valuesArray);

            // Test null chars in values
            valuesArray[offset] = $"{original[0]}\0{original.AsSpan(1)}";
            TestCore(valuesArray);

            static void TestCore(string[] valuesArray)
            {
                Values_ImplementsSearchValuesBase(StringComparison.Ordinal, valuesArray);
                Values_ImplementsSearchValuesBase(StringComparison.OrdinalIgnoreCase, valuesArray);

                string values = string.Join(", ", valuesArray);

                IndexOfAny(StringComparison.Ordinal, 0, valuesArray[0], values);
                IndexOfAny(StringComparison.OrdinalIgnoreCase, 0, valuesArray[0], values);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Remote executor has problems with exit codes")]
        public static void IndexOfAny_CanProduceDifferentResultsUnderNls()
        {
            if (CanTestInvariantCulture)
            {
                RunUsingInvariantCulture(static () =>
                {
                    IndexOfAny(StringComparison.OrdinalIgnoreCase, 1, " \U00016E40", "\U00016E60");
                    IndexOfAny(StringComparison.OrdinalIgnoreCase, 1, " \U00016E40abc", "\U00016E60, abc");
                    IndexOfAny(StringComparison.OrdinalIgnoreCase, 1, " abc\U00016E40", "abc\U00016E60");
                });
            }

            if (CanTestNls)
            {
                RunUsingNLS(static () =>
                {
                    IndexOfAny(StringComparison.OrdinalIgnoreCase, -1, " \U00016E40", "\U00016E60");
                    IndexOfAny(StringComparison.OrdinalIgnoreCase, 3, " \U00016E40abc", "\U00016E60, abc");
                    IndexOfAny(StringComparison.OrdinalIgnoreCase, -1, " abc\U00016E40", "abc\U00016E60");
                });
            }
        }

        [Fact]
        public static void Create_OnlyOrdinalComparisonIsSupported()
        {
            foreach (StringComparison comparisonType in Enum.GetValues<StringComparison>())
            {
                if (comparisonType is StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase)
                {
                    _ = SearchValues.Create(new[] { "abc" }, comparisonType);
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => SearchValues.Create(new[] { "abc" }, comparisonType));
                }
            }
        }

        [Fact]
        public static void Create_ThrowsOnNullValues()
        {
            Assert.Throws<ArgumentNullException>("values", () => SearchValues.Create(new[] { "foo", null, "bar" }, StringComparison.Ordinal));
        }

        [Fact]
        public static void TestIndexOfAny_RandomInputs()
        {
            var helper = new StringSearchValuesTestHelper(
                expected: IndexOfAnyReferenceImpl,
                searchValues: (searchSpace, values) => searchSpace.IndexOfAny(values));

            helper.TestRandomInputs();
        }

        [ConditionalFact(nameof(CanTestInvariantCulture))]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Remote executor has problems with exit codes")]
        public static void TestIndexOfAny_RandomInputs_InvariantCulture()
        {
            RunUsingInvariantCulture(static () =>
            {
                Assert.Equal("Invariant Language (Invariant Country)", CultureInfo.CurrentCulture.NativeName);

                TestIndexOfAny_RandomInputs();
            });
        }

        [ConditionalFact(nameof(CanTestNls))]
        public static void TestIndexOfAny_RandomInputs_Nls()
        {
            RunUsingNLS(static () =>
            {
                Assert.NotEqual("Invariant Language (Invariant Country)", CultureInfo.CurrentCulture.NativeName);

                TestIndexOfAny_RandomInputs();
            });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Remote executor has problems with exit codes")]
        [ActiveIssue("Manual execution only. Worth running any time SearchValues<string> logic is modified.")]
        public static void TestIndexOfAny_RandomInputs_Stress()
        {
            RunStress();

            if (CanTestInvariantCulture)
            {
                RunUsingInvariantCulture(static () => RunStress());
            }

            if (CanTestNls)
            {
                RunUsingNLS(static () => RunStress());
            }

            static void RunStress()
            {
                foreach (int maxNeedleCount in new[] { 2, 8, 20, 100 })
                {
                    foreach (int maxNeedleValueLength in new[] { 8, 40 })
                    {
                        foreach (int haystackLength in new[] { 100, 1024 })
                        {
                            var helper = new StringSearchValuesTestHelper(
                                expected: IndexOfAnyReferenceImpl,
                                searchValues: (searchSpace, values) => searchSpace.IndexOfAny(values),
                                rngSeed: Random.Shared.Next())
                            {
                                MaxNeedleCount = maxNeedleCount,
                                MaxNeedleValueLength = maxNeedleValueLength,
                                MaxHaystackLength = haystackLength,
                                HaystackIterationsPerNeedle = 1_000,
                            };

                            helper.StressRandomInputs(TimeSpan.FromSeconds(5));
                        }
                    }
                }
            }
        }

        private static int IndexOfAnyReferenceImpl(ReadOnlySpan<char> searchSpace, ReadOnlySpan<string> values, StringComparison comparisonType)
        {
            int minIndex = int.MaxValue;

            foreach (string value in values)
            {
                int i = searchSpace.IndexOf(value, comparisonType);
                if ((uint)i < minIndex)
                {
                    minIndex = i;
                }
            }

            return minIndex == int.MaxValue ? -1 : minIndex;
        }

        private static void RunUsingInvariantCulture(Action action)
        {
            Assert.True(CanTestInvariantCulture);

            var psi = new ProcessStartInfo();
            psi.Environment.Clear();
            psi.Environment.Add("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "true");

            RemoteExecutor.Invoke(action, new RemoteInvokeOptions { StartInfo = psi, TimeOut = 10 * 60 * 1000 }).Dispose();
        }

        private static void RunUsingNLS(Action action)
        {
            Assert.True(CanTestNls);

            var psi = new ProcessStartInfo();
            psi.Environment.Clear();
            psi.Environment.Add("DOTNET_SYSTEM_GLOBALIZATION_USENLS", "true");

            RemoteExecutor.Invoke(action, new RemoteInvokeOptions { StartInfo = psi, TimeOut = 10 * 60 * 1000 }).Dispose();
        }

        private sealed class StringSearchValuesTestHelper
        {
            public delegate int IndexOfAnySearchDelegate(ReadOnlySpan<char> searchSpace, ReadOnlySpan<string> values, StringComparison comparisonType);

            public delegate int SearchValuesSearchDelegate(ReadOnlySpan<char> searchSpace, SearchValues<string> values);

            public int MaxNeedleCount = 20;
            public int MaxNeedleValueLength = 10;
            public int MaxHaystackLength = 100;
            public int HaystackIterationsPerNeedle = 50;
            public int MinValueLength = 1;

            private readonly IndexOfAnySearchDelegate _expectedDelegate;
            private readonly SearchValuesSearchDelegate _searchValuesDelegate;

            private readonly char[] _randomAsciiChars;
            private readonly char[] _randomSimpleAsciiChars;
            private readonly char[] _randomChars;

            public StringSearchValuesTestHelper(IndexOfAnySearchDelegate expected, SearchValuesSearchDelegate searchValues, int rngSeed = 42)
            {
                _expectedDelegate = expected;
                _searchValuesDelegate = searchValues;

                _randomAsciiChars = new char[100 * 1024];
                _randomSimpleAsciiChars = new char[100 * 1024];
                _randomChars = new char[1024 * 1024];

                var rng = new Random(rngSeed);

                for (int i = 0; i < _randomAsciiChars.Length; i++)
                {
                    _randomAsciiChars[i] = (char)rng.Next(0, 128);
                }

                for (int i = 0; i < _randomSimpleAsciiChars.Length; i++)
                {
                    int random = rng.Next(26 * 2 + 10);

                    _randomSimpleAsciiChars[i] = (char)(random + (random switch
                    {
                        < 10 => '0',
                        < 36 => 'a' - 10,
                        _ => 'A' - 36,
                    }));
                }

                rng.NextBytes(MemoryMarshal.Cast<char, byte>(_randomChars));
            }

            public void StressRandomInputs(TimeSpan duration)
            {
                ExceptionDispatchInfo? exception = null;
                Stopwatch s = Stopwatch.StartNew();

                Parallel.For(0, Environment.ProcessorCount - 1, _ =>
                {
                    while (s.Elapsed < duration && Volatile.Read(ref exception) is null)
                    {
                        try
                        {
                            TestRandomInputs(iterationCount: 1, rng: new Random());
                        }
                        catch (Exception ex)
                        {
                            exception = ExceptionDispatchInfo.Capture(ex);
                        }
                    }
                });

                exception?.Throw();
            }

            public void TestRandomInputs(int iterationCount = 1_000, Random? rng = null)
            {
                rng ??= new Random(42);

                for (int iterations = 0; iterations < iterationCount; iterations++)
                {
                    // There are more interesting corner cases with ASCII needles, test those more.
                    Test(rng, _randomSimpleAsciiChars, _randomSimpleAsciiChars);
                    Test(rng, _randomAsciiChars, _randomSimpleAsciiChars);
                    Test(rng, _randomSimpleAsciiChars, _randomAsciiChars);
                    Test(rng, _randomAsciiChars, _randomAsciiChars);
                    Test(rng, _randomChars, _randomSimpleAsciiChars);
                    Test(rng, _randomChars, _randomAsciiChars);

                    Test(rng, _randomChars, _randomChars);
                }
            }

            private void Test(Random rng, ReadOnlySpan<char> haystackRandom, ReadOnlySpan<char> needleRandom)
            {
                string[] values = new string[rng.Next(MaxNeedleCount) + 1];

                for (int i = 0; i < values.Length; i++)
                {
                    ReadOnlySpan<char> valueSpan;
                    do
                    {
                        valueSpan = GetRandomSlice(rng, needleRandom, MaxNeedleValueLength);
                    }
                    while (valueSpan.Length < MinValueLength);

                    values[i] = valueSpan.ToString();
                }

                SearchValues<string> valuesOrdinal = SearchValues.Create(values, StringComparison.Ordinal);
                SearchValues<string> valuesOrdinalIgnoreCase = SearchValues.Create(values, StringComparison.OrdinalIgnoreCase);

                for (int i = 0; i < HaystackIterationsPerNeedle; i++)
                {
                    Test(rng, StringComparison.Ordinal, haystackRandom, values, valuesOrdinal);
                    Test(rng, StringComparison.OrdinalIgnoreCase, haystackRandom, values, valuesOrdinalIgnoreCase);
                }
            }

            private void Test(Random rng, StringComparison comparisonType, ReadOnlySpan<char> haystackRandom,
                string[] needle, SearchValues<string> searchValuesInstance)
            {
                ReadOnlySpan<char> haystack = GetRandomSlice(rng, haystackRandom, MaxHaystackLength);

                int expectedIndex = _expectedDelegate(haystack, needle, comparisonType);
                int searchValuesIndex = _searchValuesDelegate(haystack, searchValuesInstance);

                if (expectedIndex != searchValuesIndex)
                {
                    AssertionFailed(haystack, needle, searchValuesInstance, comparisonType, expectedIndex, searchValuesIndex);
                }
            }

            private static ReadOnlySpan<T> GetRandomSlice<T>(Random rng, ReadOnlySpan<T> span, int maxLength)
            {
                ReadOnlySpan<T> slice = span.Slice(rng.Next(span.Length + 1));
                return slice.Slice(0, Math.Min(slice.Length, rng.Next(maxLength + 1)));
            }

            public static void AssertionFailed(ReadOnlySpan<char> haystack, string[] needle, SearchValues<string> searchValues, StringComparison comparisonType, int expected, int actual)
            {
                Type implType = searchValues.GetType();
                string impl = $"{implType.Name} [{string.Join(", ", implType.GenericTypeArguments.Select(t => t.Name))}]";

                string readableHaystack = ReadableAsciiOrSerialized(haystack.ToString());
                string readableNeedle = string.Join(", ", needle.Select(ReadableAsciiOrSerialized));

                Assert.Fail($"Expected {expected}, got {actual} for impl='{impl}' comparison={comparisonType} needle='{readableNeedle}', haystack='{readableHaystack}'");

                static string ReadableAsciiOrSerialized(string value)
                {
                    foreach (char c in value)
                    {
                        if (!char.IsAsciiLetterOrDigit(c))
                        {
                            return $"[ {string.Join(", ", value.Select(c => int.CreateChecked(c)))} ]";
                        }
                    }

                    return value;
                }
            }
        }
    }
}
