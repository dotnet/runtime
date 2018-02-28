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
        public static void ZeroLengthToUpper()
        {
            char[] expectedSource = { 'a', 'B', 'c' };
            char[] a = { 'a', 'B', 'c' };
            var source = new ReadOnlySpan<char>(a, 2, 0);

            var expectedDestination = new char[1] { 'a' };
            Span<char> destination = new char[1] { 'a' };

            Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
            Assert.Equal(source.Length, source.ToUpperInvariant(destination));
            Assert.Equal(expectedDestination, destination.ToArray());
            Assert.Equal(expectedSource, a);

            source = ReadOnlySpan<char>.Empty;
            Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
            Assert.Equal(source.Length, source.ToUpperInvariant(destination));
            Assert.Equal(expectedDestination, destination.ToArray());
            Assert.Equal(expectedSource, a);
        }

        [Fact]
        public static void SameSpanToUpper()
        {
            var expected = new char[3] { 'A', 'B', 'C' };
            var a = new char[3] { 'a', 'B', 'c' };
            {
                ReadOnlySpan<char> source = a;
                Span<char> destination = a;
                Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expected, destination.ToArray());
                Assert.Equal(expected, source.ToArray());
            }
            {
                ReadOnlySpan<char> source = a;
                Span<char> destination = a;
                Assert.Equal(source.Length, source.ToUpperInvariant(destination));
                Assert.Equal(expected, destination.ToArray());
                Assert.Equal(expected, source.ToArray());
            }
        }

        [Fact]
        public static void ToUpperOverlapping()
        {
            var expectedSource = new char[3] { 'b', 'C', 'B' };
            var expectedDestination = new char[3] { 'B', 'C', 'B' };

            {
                char[] a = { 'a', 'b', 'C', 'b', 'C', 'b' };
                var source = new ReadOnlySpan<char>(a, 1, 3);
                var destination = new Span<char>(a, 3, 3);
                Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
            {
                char[] a = { 'a', 'b', 'C', 'b', 'C', 'b' };
                var source = new ReadOnlySpan<char>(a, 1, 3);
                var destination = new Span<char>(a, 3, 3);
                Assert.Equal(source.Length, source.ToUpperInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void LengthMismatchToUpper()
        {
            {
                var expectedSource = new char[3] { 'a', 'B', 'c' };
                ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };

                var expectedDestination = new char[1] { 'a' };
                Span<char> destination = new char[1] { 'a' };

                Assert.Equal(-1, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(-1, source.ToUpperInvariant(destination));

                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }

            {
                var expectedSource = new char[3] { 'a', 'B', 'c' };
                ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };

                var expectedDestination = new char[4] { 'A', 'B', 'C', 'd' };
                Span<char> destination = new char[4] { 'x', 'Y', 'z', 'd' };

                Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(source.Length, source.ToUpperInvariant(destination));

                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void ToUpper()
        {
            var expectedSource = new char[3] { 'a', 'B', 'c' };
            var expectedDestination = new char[3] { 'A', 'B', 'C' };

            {
                ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };
                Span<char> destination = new char[3] { 'x', 'Y', 'z' };

                Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }

            {
                ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };
                Span<char> destination = new char[3] { 'x', 'Y', 'z' };

                Assert.Equal(source.Length, source.ToUpperInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());
            }
        }

        [Fact]
        public static void MakeSureNoToUpperChecksGoOutOfRange()
        {
            for (int length = 0; length < 100; length++)
            {
                var first = new char[length + 2];
                var second = new char[length + 2];

                for (int i = 0; i < first.Length; i++)
                {
                    first[i] = 'a';
                    second[i] = 'b';
                }

                first[0] = 'z';
                first[length + 1] = 'z';

                second[0] = 'y';
                second[length + 1] = 'y';

                var expectedSource = new char[length];
                var expectedDestination = new char[length];
                for (int i = 0; i < length; i++)
                {
                    expectedSource[i] = 'a';
                    expectedDestination[i] = 'A';
                }

                var source = new ReadOnlySpan<char>(first, 1, length);
                var destination = new Span<char>(second, 1, length);
                Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
                Assert.Equal(source.Length, source.ToUpperInvariant(destination));
                Assert.Equal(expectedDestination, destination.ToArray());
                Assert.Equal(expectedSource, source.ToArray());

                Assert.Equal('z', first[0]);
                Assert.Equal('z', first[length + 1]);
                Assert.Equal('y', second[0]);
                Assert.Equal('y', second[length + 1]);
            }
        }

        [Fact]
        public static void ToUpperNullCulture()
        {
            ReadOnlySpan<char> source = new char[3] { 'a', 'B', 'c' };
            Span<char> destination = new char[3] { 'a', 'B', 'c' };

            try
            {
                source.ToUpper(destination, null);
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
        [InlineData("hello", "HELLO")]
        [InlineData("HELLO", "HELLO")]
        [InlineData("", "")]
        public static void ToUpper(string s, string expected)
        {
            ReadOnlySpan<char> source = s.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, CultureInfo.CurrentCulture));
            Assert.Equal(expected, destination.ToString());
        }

        private static IEnumerable<object[]> ToUpper_Culture_TestData()
        {
            yield return new object[] { "h\u0069 world", "H\u0130 WORLD", new CultureInfo("tr-TR") };
            yield return new object[] { "h\u0130 world", "H\u0130 WORLD", new CultureInfo("tr-TR") };
            yield return new object[] { "h\u0131 world", "H\u0049 WORLD", new CultureInfo("tr-TR") };

            yield return new object[] { "h\u0069 world", "H\u0049 WORLD", new CultureInfo("en-US") };
            yield return new object[] { "h\u0130 world", "H\u0130 WORLD", new CultureInfo("en-US") };
            yield return new object[] { "h\u0131 world", "H\u0049 WORLD", new CultureInfo("en-US") };

            yield return new object[] { "h\u0069 world", "H\u0049 WORLD", CultureInfo.InvariantCulture };
            yield return new object[] { "h\u0130 world", "H\u0130 WORLD", CultureInfo.InvariantCulture };
            yield return new object[] { "h\u0131 world", "H\u0131 WORLD", CultureInfo.InvariantCulture };
        }

        [Theory]
        [MemberData(nameof(ToUpper_Culture_TestData))]
        public static void Test_ToUpper_Culture(string actual, string expected, CultureInfo culture)
        {
            ReadOnlySpan<char> source = actual.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());
        }

        [Fact]
        public static void ToUpper_TurkishI_TurkishCulture()
        {
            CultureInfo culture = new CultureInfo("tr-TR");

            string s = "H\u0069 World";
            string expected = "H\u0130 WORLD";
            ReadOnlySpan<char> source = s.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());

            s = "H\u0130 World";
            expected = "H\u0130 WORLD";
            source = s.AsSpan();
            destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());

            s = "H\u0131 World";
            expected = "H\u0049 WORLD";
            source = s.AsSpan();
            destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());
        }

        [Fact]
        public static void ToUpper_TurkishI_EnglishUSCulture()
        {
            CultureInfo culture = new CultureInfo("en-US");

            string s = "H\u0069 World";
            string expected = "H\u0049 WORLD";
            ReadOnlySpan<char> source = s.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());

            s = "H\u0130 World";
            expected = "H\u0130 WORLD";
            source = s.AsSpan();
            destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());

            s = "H\u0131 World";
            expected = "H\u0049 WORLD";
            source = s.AsSpan();
            destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());
        }

        [Fact]
        public static void ToUpper_TurkishI_InvariantCulture()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;

            string s = "H\u0069 World";
            string expected = "H\u0049 WORLD";
            ReadOnlySpan<char> source = s.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());

            s = "H\u0130 World";
            expected = "H\u0130 WORLD";
            source = s.AsSpan();
            destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());

            s = "H\u0131 World";
            expected = "H\u0131 WORLD";
            source = s.AsSpan();
            destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpper(destination, culture));
            Assert.Equal(expected, destination.ToString());
        }

        [Theory]
        [InlineData("hello", "HELLO")]
        [InlineData("HELLO", "HELLO")]
        [InlineData("", "")]
        public static void ToUpperInvariant(string s, string expected)
        {
            ReadOnlySpan<char> source = s.AsSpan();
            Span<char> destination = new char[source.Length];
            Assert.Equal(source.Length, source.ToUpperInvariant(destination));
            Assert.Equal(expected, destination.ToString());
        }
    }
}
