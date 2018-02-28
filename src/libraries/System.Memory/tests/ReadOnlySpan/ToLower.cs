// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthToLower()
        {
            char[] expectedSource = { 'a', 'B', 'c' };
            char[] a = { 'a', 'B', 'c' };
            var source = new ReadOnlySpan<char>(a, 2, 0);

            var expectedDestination = new char[1] { 'a' };
            Span<char> destination = new char[1] { 'a' };

            Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
            Assert.Equal(source.Length, source.ToLowerInvariant(destination));
            Assert.Equal(expectedDestination, destination.ToArray());
            Assert.Equal(expectedSource, a);

            source = ReadOnlySpan<char>.Empty;
            Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
            Assert.Equal(source.Length, source.ToLowerInvariant(destination));
            Assert.Equal(expectedDestination, destination.ToArray());
            Assert.Equal(expectedSource, a);
        }

        [Fact]
        public static void SameSpanToLower()
        {
            var expected = new char[3] { 'a', 'b', 'c' };
            var a = new char[3] { 'a', 'B', 'c' };
            {
                ReadOnlySpan<char> source = a;
                Span<char> destination = a;
                Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expected, destination.ToArray());
                Assert.Equal(expected, source.ToArray());
            }
            {
                ReadOnlySpan<char> source = a;
                Span<char> destination = a;
                Assert.Equal(source.Length, source.ToLowerInvariant(destination));
                Assert.Equal(expected, destination.ToArray());
                Assert.Equal(expected, source.ToArray());
            }
        }

        [Fact]
        public static void ToLowerOverlapping()
        {
            var expectedSource = new char[3] { 'B', 'c', 'b' };
            var expectedDestination = new char[3] { 'b', 'c', 'b' };

            {
                char[] a = { 'a', 'B', 'c', 'B', 'c', 'B' };
                var source = new ReadOnlySpan<char>(a, 1, 3);
                var destination = new Span<char>(a, 3, 3);
                Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
            {
                char[] a = { 'a', 'B', 'c', 'B', 'c', 'B' };
                var source = new ReadOnlySpan<char>(a, 1, 3);
                var destination = new Span<char>(a, 3, 3);
                Assert.Equal(source.Length, source.ToLowerInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void LengthMismatchToLower()
        {
            {
                var expectedSource = new char[3] { 'a', 'B', 'c' };
                ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };

                var expectedDestination = new char[1] { 'a' };
                Span<char> destination = new char[1] { 'a' };

                Assert.Equal(-1, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(-1, source.ToLowerInvariant(destination));

                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }

            {
                var expectedSource = new char[3] { 'a', 'B', 'c' };
                ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };

                var expectedDestination = new char[4] { 'a', 'b', 'c', 'D' };
                Span<char> destination = new char[4] { 'x', 'Y', 'z', 'D' };

                Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(source.Length, source.ToLowerInvariant(destination));

                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void ToLower()
        {
            var expectedSource = new char[3] { 'a', 'B', 'c' };
            var expectedDestination = new char[3] { 'a', 'b', 'c' };

            {
                ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };
                Span<char> destination = new char[3] { 'x', 'Y', 'z' };

                Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }

            {
                ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };
                Span<char> destination = new char[3] { 'x', 'Y', 'z' };

                Assert.Equal(source.Length, source.ToLowerInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void MakeSureNoToLowerChecksGoOutOfRange()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                var second = new char[length + 2];

                for (int i = 0; i < first.Length; i++)
                {
                    first[i] = 'A';
                    second[i] = 'B';
                }

                first[0] = 'Z';
                first[length + 1] = 'Z';

                second[0] = 'Y';
                second[length + 1] = 'Y';

                var expectedSource = new char[length];
                var expectedDestination = new char[length];
                for (int i = 0; i < length; i++)
                {
                    expectedSource[i] = 'A';
                    expectedDestination[i] = 'a';
                }

                var source = new ReadOnlySpan<char>(first, 1, length);
                var destination = new Span<char>(second, 1, length);
                Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
                Assert.Equal(source.Length, source.ToLowerInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());

                Assert.Equal('Z', first[0]);
                Assert.Equal('Z', first[length + 1]);
                Assert.Equal('Y', second[0]);
                Assert.Equal('Y', second[length + 1]);
            }
        }

        [Fact]
        public static void ToLowerNullCulture()
        {
            ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };
            Span<char> destination = new char[3] { 'a', 'B', 'c' };

            try
            {
                source.ToLower(destination, null);
                Assert.False(true, "Expected exception: " + typeof(ArgumentNullException).GetType());
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception wrongException)
            {
                Assert.False(true, "Wrong exception thrown: Expected " + typeof(ArgumentNullException).GetType() + ": Actual: " + wrongException.GetType());
            }
        }

        [Theory]
        [InlineData("HELLO", "hello")]
        [InlineData("hello", "hello")]
        [InlineData("", "")]
        public static void ToLower(string s, string expected)
        {
            ReadOnlySpan<char> source = s.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToLower(destination, CultureInfo.CurrentCulture));
            Assert.Equal(expected, destination.ToString());
        }

        private static IEnumerable<object[]> ToLower_Culture_TestData()
        {
            yield return new object[] { "H\u0049 World", "h\u0131 world", new CultureInfo("tr-TR") };
            yield return new object[] { "H\u0130 World", "h\u0069 world", new CultureInfo("tr-TR") };
            yield return new object[] { "H\u0131 World", "h\u0131 world", new CultureInfo("tr-TR") };

            yield return new object[] { "H\u0049 World", "h\u0069 world", new CultureInfo("en-US") };
            yield return new object[] { "H\u0130 World", "h\u0069 world", new CultureInfo("en-US") };
            yield return new object[] { "H\u0131 World", "h\u0131 world", new CultureInfo("en-US") };

            yield return new object[] { "H\u0049 World", "h\u0069 world", CultureInfo.InvariantCulture };
            yield return new object[] { "H\u0130 World", "h\u0130 world", CultureInfo.InvariantCulture };
            yield return new object[] { "H\u0131 World", "h\u0131 world", CultureInfo.InvariantCulture };
        }

        [Theory]
        [MemberData(nameof(ToLower_Culture_TestData))]
        public static void Test_ToLower_Culture(string actual, string expected, CultureInfo culture)
        {
            ReadOnlySpan<char> source = actual.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToLower(destination, culture));
            Assert.Equal(expected, destination.ToString());
        }

        [Theory]
        [InlineData("HELLO", "hello")]
        [InlineData("hello", "hello")]
        [InlineData("", "")]
        public static void ToLowerInvariant(string s, string expected)
        {
            ReadOnlySpan<char> source = s.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToLowerInvariant(destination));
            Assert.Equal(expected, destination.ToString());
        }

        [Fact]
        public static void ToLowerToUpperInvariant_ASCII()
        {
            var asciiChars = new char[128];
            var asciiCharsUpper = new char[128];
            var asciiCharsLower = new char[128];

            for (int i = 0; i < asciiChars.Length; i++)
            {
                char c = (char)i;
                asciiChars[i] = c;

                // Purposefully avoiding char.ToUpper/ToLower here so as not to use the same thing we're testing.
                asciiCharsLower[i] = (c >= 'A' && c <= 'Z') ? (char)(c - 'A' + 'a') : c;
                asciiCharsUpper[i] = (c >= 'a' && c <= 'z') ? (char)(c - 'a' + 'A') : c;
            }

            ReadOnlySpan<char> source = asciiChars;
            var ascii = new string(asciiChars);
            Span<char> destinationLower = new char[source.Length];
            Span<char> destinationUpper = new char[source.Length];

            Assert.Equal(source.Length, source.ToLowerInvariant(destinationLower));
            Assert.Equal(source.Length, source.ToUpperInvariant(destinationUpper));

            Assert.Equal(ascii.ToLowerInvariant(), destinationLower.ToString());
            Assert.Equal(ascii.ToUpperInvariant(), destinationUpper.ToString());

            Assert.Equal(ascii, source.ToString());
        }

        public static IEnumerable<object[]> UpperLowerCasing_TestData()
        {
            //lower, upper, Culture
            yield return new object[] { "abcd", "ABCD", "en-US" };
            yield return new object[] { "latin i", "LATIN I", "en-US" };
            yield return new object[] { "turky \u0131", "TURKY I", "tr-TR" };
            yield return new object[] { "turky i", "TURKY \u0130", "tr-TR" };
            yield return new object[] { "\ud801\udc29", PlatformDetection.IsWindows7 ? "\ud801\udc29" : "\ud801\udc01", "en-US" };
        }

        [Theory]
        [MemberData(nameof(UpperLowerCasing_TestData))]
        public static void CasingTest(string lowerForm, string upperForm, string cultureName)
        {
            CultureInfo ci = CultureInfo.GetCultureInfo(cultureName);

            ReadOnlySpan<char> sourceLower = lowerForm.AsSpan();
            ReadOnlySpan<char> sourceUpper = upperForm.AsSpan();
            Span<char> destinationLower = new char[sourceUpper.Length];
            Span<char> destinationUpper = new char[sourceLower.Length];

            Assert.Equal(sourceUpper.Length, sourceUpper.ToLower(destinationLower, ci));
            Assert.Equal(sourceLower.Length, sourceLower.ToUpper(destinationUpper, ci));

            Assert.Equal(upperForm.ToLower(ci), destinationLower.ToString());
            Assert.Equal(lowerForm.ToUpper(ci), destinationUpper.ToString());

            Assert.Equal(lowerForm, sourceLower.ToString());
            Assert.Equal(upperForm, sourceUpper.ToString());
        }
    }
}
