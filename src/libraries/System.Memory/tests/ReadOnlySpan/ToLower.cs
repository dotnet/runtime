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

    }
}
