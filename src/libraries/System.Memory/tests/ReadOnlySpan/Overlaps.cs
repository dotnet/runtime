// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        private static void DoubleEachElementForwards(ReadOnlySpan<int> source, Span<int> destination)
        {
            if (source.Length != destination.Length)
                throw new ArgumentException();

            // This loop below moves forwards, so if there is an overlap and destination starts
            // after source then the loop will overwrite unread data without making a copy first.

            if (source.Overlaps(destination, out int elementOffset) && elementOffset > 0)
                source = source.ToArray();

            for (int i = 0; i < source.Length; i++)
                destination[i] = 2 * source[i];
        }

        [Fact]
        public static void TestAlignedForwards()
        {
            for (int i = 0; i < 14; i++)
            {
                int[] a = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };

                ReadOnlySpan<int> source = a.AsReadOnlySpan().Slice(7, 5);

                Span<int> expected = new int[a.Length].AsSpan().Slice(i, 5);
                Span<int> actual = a.AsSpan().Slice(i, 5);

                DoubleEachElementForwards(source, expected);
                DoubleEachElementForwards(source, actual);

                Assert.Equal(expected.ToArray(), actual.ToArray());
            }
        }

        [Fact]
        public static void TestUnalignedForwards()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                int[] a = new int[] { 1, 2, 3, 4, 5, 6 };

                ReadOnlySpan<int> source = a.AsReadOnlySpan().AsBytes()
                    .Slice(2, 5 * sizeof(int))
                    .NonPortableCast<byte, int>();

                Span<int> actual = a.AsSpan().Slice(0, 5);

                DoubleEachElementForwards(source, actual);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                int[] a = new int[] { 1, 2, 3, 4, 5, 6 };

                ReadOnlySpan<int> source = a.AsReadOnlySpan().AsBytes()
                    .Slice(2, 5 * sizeof(int))
                    .NonPortableCast<byte, int>();

                Span<int> actual = a.AsSpan().Slice(1, 5);

                DoubleEachElementForwards(source, actual);
            });
        }

        private static void DoubleEachElementBackwards(ReadOnlySpan<int> source, Span<int> destination)
        {
            if (source.Length != destination.Length)
                throw new ArgumentException();

            // This loop below moves backwards, so if there is an overlap and destination starts
            // before source then the loop will overwrite unread data without making a copy first.

            if (source.Overlaps(destination, out int elementOffset) && elementOffset < 0)
                source = source.ToArray();

            for (int i = source.Length - 1; i >= 0; i--)
                destination[i] = 2 * source[i];
        }

        [Fact]
        public static void TestAlignedBackwards()
        {
            for (int i = 0; i < 14; i++)
            {
                int[] a = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };

                ReadOnlySpan<int> source = a.AsReadOnlySpan().Slice(7, 5);

                Span<int> expected = new int[a.Length].AsSpan().Slice(i, 5);
                Span<int> actual = a.AsSpan().Slice(i, 5);

                DoubleEachElementBackwards(source, expected);
                DoubleEachElementBackwards(source, actual);

                Assert.Equal(expected.ToArray(), actual.ToArray());
            }
        }

        [Fact]
        public static void TestUnalignedBackwards()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                int[] a = new int[] { 1, 2, 3, 4, 5, 6 };

                ReadOnlySpan<int> source = a.AsReadOnlySpan().AsBytes()
                    .Slice(2, 5 * sizeof(int))
                    .NonPortableCast<byte, int>();

                Span<int> actual = a.AsSpan().Slice(0, 5);

                DoubleEachElementBackwards(source, actual);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                int[] a = new int[] { 1, 2, 3, 4, 5, 6 };

                ReadOnlySpan<int> source = a.AsReadOnlySpan().AsBytes()
                    .Slice(2, 5 * sizeof(int))
                    .NonPortableCast<byte, int>();

                Span<int> actual = a.AsSpan().Slice(1, 5);

                DoubleEachElementBackwards(source, actual);
            });
        }

        [Fact]
        public static void SizeOf1Overlaps()
        {
            byte[] a = new byte[16];

            Assert.True(a.AsReadOnlySpan().Slice(0, 12).Overlaps(a.AsReadOnlySpan().Slice(8, 8), out int elementOffset));
            Assert.Equal(8, elementOffset);
        }

        [Fact]
        public static void SizeOf16Overlaps()
        {
            Guid[] a = new Guid[16];

            Assert.True(a.AsReadOnlySpan().Slice(0, 12).Overlaps(a.AsReadOnlySpan().Slice(8, 8), out int elementOffset));
            Assert.Equal(8, elementOffset);
        }

        //
        // The following tests were all generated with this (otherwise unused) method:
        //
        private static string GenerateOverlapsTests()
        {
            const int count = 4;
            string result = "";

            for (int x1 = 0; x1 < count; x1++)
            {
                for (int x2 = x1; x2 < count; x2++)
                {
                    for (int y1 = 0; y1 < count; y1++)
                    {
                        for (int y2 = y1; y2 < count; y2++)
                        {
                            bool expected = (x1 < x2) && (y1 < y2) && (x1 < y2) && (y1 < x2);

                            result += $"[InlineData({x1 * 100}, {x2 * 100}, {y1 * 100}, {y2 * 100}, {(expected ? "true" : "false")})]\r\n";
                        }
                    }
                }
            }

            return result;
        }

        //
        //               0
        //       first:  |
        //      second:  |
        //               0
        //
        [InlineData(0, 0, 0, 0, false)]

        //
        //               0
        //       first:  |
        //      second:  [---------)
        //               0        100
        //
        [InlineData(0, 0, 0, 100, false)]

        //
        //               0
        //       first:  |
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(0, 0, 0, 200, false)]

        //
        //               0
        //       first:  |
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(0, 0, 0, 300, false)]

        //
        //               0
        //       first:  |
        //      second:            |
        //                        100
        //
        [InlineData(0, 0, 100, 100, false)]

        //
        //               0
        //       first:  |
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(0, 0, 100, 200, false)]

        //
        //               0
        //       first:  |
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(0, 0, 100, 300, false)]

        //
        //               0
        //       first:  |
        //      second:                      |
        //                                  200
        //
        [InlineData(0, 0, 200, 200, false)]

        //
        //               0
        //       first:  |
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(0, 0, 200, 300, false)]

        //
        //               0
        //       first:  |
        //      second:                                |
        //                                            300
        //
        [InlineData(0, 0, 300, 300, false)]

        //
        //               0        100
        //       first:  [---------)
        //      second:  |
        //               0
        //
        [InlineData(0, 100, 0, 0, false)]

        //
        //               0        100
        //       first:  [---------)
        //      second:  [---------)
        //               0        100
        //
        [InlineData(0, 100, 0, 100, true)]

        //
        //               0        100
        //       first:  [---------)
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(0, 100, 0, 200, true)]

        //
        //               0        100
        //       first:  [---------)
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(0, 100, 0, 300, true)]

        //
        //               0        100
        //       first:  [---------)
        //      second:            |
        //                        100
        //
        [InlineData(0, 100, 100, 100, false)]

        //
        //               0        100
        //       first:  [---------)
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(0, 100, 100, 200, false)]

        //
        //               0        100
        //       first:  [---------)
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(0, 100, 100, 300, false)]

        //
        //               0        100
        //       first:  [---------)
        //      second:                      |
        //                                  200
        //
        [InlineData(0, 100, 200, 200, false)]

        //
        //               0        100
        //       first:  [---------)
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(0, 100, 200, 300, false)]

        //
        //               0        100
        //       first:  [---------)
        //      second:                                |
        //                                            300
        //
        [InlineData(0, 100, 300, 300, false)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:  |
        //               0
        //
        [InlineData(0, 200, 0, 0, false)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:  [---------)
        //               0        100
        //
        [InlineData(0, 200, 0, 100, true)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(0, 200, 0, 200, true)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(0, 200, 0, 300, true)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:            |
        //                        100
        //
        [InlineData(0, 200, 100, 100, false)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(0, 200, 100, 200, true)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(0, 200, 100, 300, true)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:                      |
        //                                  200
        //
        [InlineData(0, 200, 200, 200, false)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(0, 200, 200, 300, false)]

        //
        //               0                  200
        //       first:  [-------------------)
        //      second:                                |
        //                                            300
        //
        [InlineData(0, 200, 300, 300, false)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:  |
        //               0
        //
        [InlineData(0, 300, 0, 0, false)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:  [---------)
        //               0        100
        //
        [InlineData(0, 300, 0, 100, true)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(0, 300, 0, 200, true)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(0, 300, 0, 300, true)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:            |
        //                        100
        //
        [InlineData(0, 300, 100, 100, false)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(0, 300, 100, 200, true)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(0, 300, 100, 300, true)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:                      |
        //                                  200
        //
        [InlineData(0, 300, 200, 200, false)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(0, 300, 200, 300, true)]

        //
        //               0                            300
        //       first:  [-----------------------------)
        //      second:                                |
        //                                            300
        //
        [InlineData(0, 300, 300, 300, false)]

        //
        //                        100
        //       first:            |
        //      second:  |
        //               0
        //
        [InlineData(100, 100, 0, 0, false)]

        //
        //                        100
        //       first:            |
        //      second:  [---------)
        //               0        100
        //
        [InlineData(100, 100, 0, 100, false)]

        //
        //                        100
        //       first:            |
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(100, 100, 0, 200, false)]

        //
        //                        100
        //       first:            |
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(100, 100, 0, 300, false)]

        //
        //                        100
        //       first:            |
        //      second:            |
        //                        100
        //
        [InlineData(100, 100, 100, 100, false)]

        //
        //                        100
        //       first:            |
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(100, 100, 100, 200, false)]

        //
        //                        100
        //       first:            |
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(100, 100, 100, 300, false)]

        //
        //                        100
        //       first:            |
        //      second:                      |
        //                                  200
        //
        [InlineData(100, 100, 200, 200, false)]

        //
        //                        100
        //       first:            |
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(100, 100, 200, 300, false)]

        //
        //                        100
        //       first:            |
        //      second:                                |
        //                                            300
        //
        [InlineData(100, 100, 300, 300, false)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:  |
        //               0
        //
        [InlineData(100, 200, 0, 0, false)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:  [---------)
        //               0        100
        //
        [InlineData(100, 200, 0, 100, false)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(100, 200, 0, 200, true)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(100, 200, 0, 300, true)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:            |
        //                        100
        //
        [InlineData(100, 200, 100, 100, false)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(100, 200, 100, 200, true)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(100, 200, 100, 300, true)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:                      |
        //                                  200
        //
        [InlineData(100, 200, 200, 200, false)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(100, 200, 200, 300, false)]

        //
        //                        100       200
        //       first:            [---------)
        //      second:                                |
        //                                            300
        //
        [InlineData(100, 200, 300, 300, false)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:  |
        //               0
        //
        [InlineData(100, 300, 0, 0, false)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:  [---------)
        //               0        100
        //
        [InlineData(100, 300, 0, 100, false)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(100, 300, 0, 200, true)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(100, 300, 0, 300, true)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:            |
        //                        100
        //
        [InlineData(100, 300, 100, 100, false)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(100, 300, 100, 200, true)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(100, 300, 100, 300, true)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:                      |
        //                                  200
        //
        [InlineData(100, 300, 200, 200, false)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(100, 300, 200, 300, true)]

        //
        //                        100                 300
        //       first:            [-------------------)
        //      second:                                |
        //                                            300
        //
        [InlineData(100, 300, 300, 300, false)]

        //
        //                                  200
        //       first:                      |
        //      second:  |
        //               0
        //
        [InlineData(200, 200, 0, 0, false)]

        //
        //                                  200
        //       first:                      |
        //      second:  [---------)
        //               0        100
        //
        [InlineData(200, 200, 0, 100, false)]

        //
        //                                  200
        //       first:                      |
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(200, 200, 0, 200, false)]

        //
        //                                  200
        //       first:                      |
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(200, 200, 0, 300, false)]

        //
        //                                  200
        //       first:                      |
        //      second:            |
        //                        100
        //
        [InlineData(200, 200, 100, 100, false)]

        //
        //                                  200
        //       first:                      |
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(200, 200, 100, 200, false)]

        //
        //                                  200
        //       first:                      |
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(200, 200, 100, 300, false)]

        //
        //                                  200
        //       first:                      |
        //      second:                      |
        //                                  200
        //
        [InlineData(200, 200, 200, 200, false)]

        //
        //                                  200
        //       first:                      |
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(200, 200, 200, 300, false)]

        //
        //                                  200
        //       first:                      |
        //      second:                                |
        //                                            300
        //
        [InlineData(200, 200, 300, 300, false)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:  |
        //               0
        //
        [InlineData(200, 300, 0, 0, false)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:  [---------)
        //               0        100
        //
        [InlineData(200, 300, 0, 100, false)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(200, 300, 0, 200, false)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(200, 300, 0, 300, true)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:            |
        //                        100
        //
        [InlineData(200, 300, 100, 100, false)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(200, 300, 100, 200, false)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(200, 300, 100, 300, true)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:                      |
        //                                  200
        //
        [InlineData(200, 300, 200, 200, false)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(200, 300, 200, 300, true)]

        //
        //                                  200       300
        //       first:                      [---------)
        //      second:                                |
        //                                            300
        //
        [InlineData(200, 300, 300, 300, false)]

        //
        //                                            300
        //       first:                                |
        //      second:  |
        //               0
        //
        [InlineData(300, 300, 0, 0, false)]

        //
        //                                            300
        //       first:                                |
        //      second:  [---------)
        //               0        100
        //
        [InlineData(300, 300, 0, 100, false)]

        //
        //                                            300
        //       first:                                |
        //      second:  [-------------------)
        //               0                  200
        //
        [InlineData(300, 300, 0, 200, false)]

        //
        //                                            300
        //       first:                                |
        //      second:  [-----------------------------)
        //               0                            300
        //
        [InlineData(300, 300, 0, 300, false)]

        //
        //                                            300
        //       first:                                |
        //      second:            |
        //                        100
        //
        [InlineData(300, 300, 100, 100, false)]

        //
        //                                            300
        //       first:                                |
        //      second:            [---------)
        //                        100       200
        //
        [InlineData(300, 300, 100, 200, false)]

        //
        //                                            300
        //       first:                                |
        //      second:            [-------------------)
        //                        100                 300
        //
        [InlineData(300, 300, 100, 300, false)]

        //
        //                                            300
        //       first:                                |
        //      second:                      |
        //                                  200
        //
        [InlineData(300, 300, 200, 200, false)]

        //
        //                                            300
        //       first:                                |
        //      second:                      [---------)
        //                                  200       300
        //
        [InlineData(300, 300, 200, 300, false)]

        //
        //                                            300
        //       first:                                |
        //      second:                                |
        //                                            300
        //
        [InlineData(300, 300, 300, 300, false)]

        [Theory]
        public static void Overlap(int x1, int y1, int x2, int y2, bool expected)
        {
            ReadOnlySpan<int> a = new int[300];

            Assert.Equal(expected, a.Slice(x1, y1 - x1).Overlaps(a.Slice(x2, y2 - x2)));
            Assert.Equal(expected, a.Slice(x1, y1 - x1).Overlaps(a.Slice(x2, y2 - x2), out int elementOffset));
            Assert.Equal(expected ? x2 - x1 : 0, elementOffset);
        }
    }
}
