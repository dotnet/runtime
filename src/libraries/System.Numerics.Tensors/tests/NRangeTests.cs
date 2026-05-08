// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Buffers.Tests
{
    public static class NativeRangeTests
    {
        [Fact]
        public static void CreationTest()
        {
            NRange NativeRange = new NRange(new NIndex(10, fromEnd: false), new NIndex(2, fromEnd: true));
            Assert.Equal(10, NativeRange.Start.Value);
            Assert.False(NativeRange.Start.IsFromEnd);
            Assert.Equal(2, NativeRange.End.Value);
            Assert.True(NativeRange.End.IsFromEnd);

            NativeRange = NRange.StartAt(new NIndex(7, fromEnd: false));
            Assert.Equal(7, NativeRange.Start.Value);
            Assert.False(NativeRange.Start.IsFromEnd);
            Assert.Equal(0, NativeRange.End.Value);
            Assert.True(NativeRange.End.IsFromEnd);

            NativeRange = NRange.EndAt(new NIndex(3, fromEnd: true));
            Assert.Equal(0, NativeRange.Start.Value);
            Assert.False(NativeRange.Start.IsFromEnd);
            Assert.Equal(3, NativeRange.End.Value);
            Assert.True(NativeRange.End.IsFromEnd);

            NativeRange = NRange.All;
            Assert.Equal(0, NativeRange.Start.Value);
            Assert.False(NativeRange.Start.IsFromEnd);
            Assert.Equal(0, NativeRange.End.Value);
            Assert.True(NativeRange.End.IsFromEnd);

            // Make sure can implicitly convert from Range/Index
            NativeRange = new Range(new Index(10, fromEnd: false), new Index(2, fromEnd: true));
            Assert.Equal(10, NativeRange.Start.Value);
            Assert.False(NativeRange.Start.IsFromEnd);
            Assert.Equal(2, NativeRange.End.Value);
            Assert.True(NativeRange.End.IsFromEnd);

            NativeRange = new NRange(new Index(10, fromEnd: false), new Index(2, fromEnd: true));
            Assert.Equal(10, NativeRange.Start.Value);
            Assert.False(NativeRange.Start.IsFromEnd);
            Assert.Equal(2, NativeRange.End.Value);
            Assert.True(NativeRange.End.IsFromEnd);
        }

        [Theory]
        [InlineData(5, false, 0, true, 20, 5, 15)]
        [InlineData(5, false, 0, true, 5, 5, 0)]
        [InlineData(5, false, 0, true, -10, 5, -15)]
        [InlineData(0, false, 4, false, 20, 0, 4)]
        public static void GetOffsetAndLengthTest(nint startValue, bool startFromEnd, nint endValue, bool endFromEnd, nint length, nint expectedOffset, nint expectedLength)
        {
            NRange range = new NRange(new NIndex(startValue, startFromEnd), new NIndex(endValue, endFromEnd));
            (nint offset, nint actualLength) = range.GetOffsetAndLength(length);
            Assert.Equal(expectedOffset, offset);
            Assert.Equal(expectedLength, actualLength);
        }

        [Theory]
        [InlineData(5, false, 0, true, 4)]
        [InlineData(0, false, 4, false, 1)]
        public static void GetOffsetAndLengthThrowsTest(nint startValue, bool startFromEnd, nint endValue, bool endFromEnd, nint length)
        {
            NRange range = new NRange(new NIndex(startValue, startFromEnd), new NIndex(endValue, endFromEnd));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.GetOffsetAndLength(length));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(5, false, 0, true, (long)uint.MaxValue + 20, 5, (long)uint.MaxValue + 15)]
        [InlineData(0, false, (long)uint.MaxValue + 5, false, (long)uint.MaxValue + 20, 0, (long)uint.MaxValue + 5)]
        public static void GetOffsetAndLengthTest64(long startValue, bool startFromEnd, long endValue, bool endFromEnd, long length, long expectedOffset, long expectedLength)
        {
            NRange range = new NRange(new NIndex((nint)startValue, startFromEnd), new NIndex((nint)endValue, endFromEnd));
            (nint offset, nint actualLength) = range.GetOffsetAndLength((nint)length);
            Assert.Equal((nint)expectedOffset, offset);
            Assert.Equal((nint)expectedLength, actualLength);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData((long)uint.MaxValue + 1, false, 2, false, (long)uint.MaxValue + 3)]
        [InlineData((long)uint.MaxValue + 5, false, 0, true, (long)uint.MaxValue + 1)]
        public static void GetOffsetAndLengthThrowsTest64(long startValue, bool startFromEnd, long endValue, bool endFromEnd, long length)
        {
            NRange range = new NRange(new NIndex((nint)startValue, startFromEnd), new NIndex((nint)endValue, endFromEnd));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.GetOffsetAndLength((nint)length));
        }

        [Fact]
        public static void EqualityTest()
        {
            NRange NativeRange1 = new NRange(new NIndex(10, fromEnd: false), new NIndex(20, fromEnd: false));
            NRange NativeRange2 = new NRange(new NIndex(10, fromEnd: false), new NIndex(20, fromEnd: false));
            Assert.True(NativeRange1.Equals(NativeRange2));
            Assert.True(NativeRange1.Equals((object)NativeRange2));

            NativeRange2 = new NRange(new NIndex(10, fromEnd: false), new NIndex(20, fromEnd: true));
            Assert.False(NativeRange1.Equals(NativeRange2));
            Assert.False(NativeRange1.Equals((object)NativeRange2));

            NativeRange2 = new NRange(new NIndex(10, fromEnd: false), new NIndex(21, fromEnd: false));
            Assert.False(NativeRange1.Equals(NativeRange2));
            Assert.False(NativeRange1.Equals((object)NativeRange2));
        }

        [Fact]
        public static void HashCodeTest()
        {
            NRange NativeRange1 = new NRange(new NIndex(10, fromEnd: false), new NIndex(20, fromEnd: false));
            NRange NativeRange2 = new NRange(new NIndex(10, fromEnd: false), new NIndex(20, fromEnd: false));
            Assert.Equal(NativeRange1.GetHashCode(), NativeRange2.GetHashCode());

            NativeRange2 = new NRange(new NIndex(101, fromEnd: false), new NIndex(20, fromEnd: true));
            Assert.NotEqual(NativeRange1.GetHashCode(), NativeRange2.GetHashCode());

            NativeRange2 = new NRange(new NIndex(10, fromEnd: false), new NIndex(21, fromEnd: false));
            Assert.NotEqual(NativeRange1.GetHashCode(), NativeRange2.GetHashCode());
        }

        [Theory]
        [InlineData(10, false, 20, false, "10..20")]
        [InlineData(10, false, 20, true, "10..^20")]
        [InlineData(0, true, 0, true, "^0..^0")]
        [InlineData(5, true, 10, false, "^5..10")]
        [InlineData(int.MaxValue, false, int.MaxValue, true, "2147483647..^2147483647")]
        public static void ToStringTest(long startValue, bool startFromEnd, long endValue, bool endFromEnd, string expected)
        {
            NRange range = new NRange(new NIndex((nint)startValue, startFromEnd), new NIndex((nint)endValue, endFromEnd));
            Assert.Equal(expected, range.ToString());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(1L + uint.MaxValue, false, 1L + uint.MaxValue, true, "4294967296..^4294967296")]
        [InlineData(long.MaxValue, false, long.MaxValue, true, "9223372036854775807..^9223372036854775807")]
        [InlineData(1L + uint.MaxValue, true, long.MaxValue, false, "^4294967296..9223372036854775807")]
        public static void ToStringTest_64bit(long startValue, bool startFromEnd, long endValue, bool endFromEnd, string expected)
        {
            NRange range = new NRange(new NIndex((nint)startValue, startFromEnd), new NIndex((nint)endValue, endFromEnd));
            Assert.Equal(expected, range.ToString());
        }

        [Fact]
        public static void CustomTypeTest()
        {
            CustomNativeRangeTester crt = new CustomNativeRangeTester(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            for (int i = 0; i < crt.Length; i++)
            {
                Assert.Equal(crt[i], crt[(int)NIndex.FromStart(i).Value]);
                Assert.Equal(crt[crt.Length - i - 1], crt[^(i + 1)]);

                Assert.True(crt.Slice(i, crt.Length - i).Equals(crt[i..^0]), $"NIndex = {i} and {crt.Slice(i, crt.Length - i)} != {crt[i..^0]}");
            }
        }

        // CustomNativeRangeTester is a custom class which containing the members FlattenedLength, Slice and int NativeIndexer.
        // Having these members allow the C# compiler to support
        //      this[NIndex]
        //      this[NRange]
        private class CustomNativeRangeTester : IEquatable<CustomNativeRangeTester>
        {
            private int[] _data;

            public CustomNativeRangeTester(int[] data) => _data = data;
            public int Length => _data.Length;
            public int this[int NativeIndex] => _data[NativeIndex];
            public CustomNativeRangeTester Slice(int start, int length) => new CustomNativeRangeTester(_data.AsSpan(start, length).ToArray());

            public int[] Data => _data;

            public bool Equals(CustomNativeRangeTester other)
            {
                if (_data.Length == other.Data.Length)
                {
                    for (int i = 0; i < _data.Length; i++)
                    {
                        if (_data[i] != other.Data[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }

                return false;
            }

            public override string ToString()
            {
                if (Length == 0)
                {
                    return "[]";
                }

                string s = "[" + _data[0];

                for (int i = 1; i < Length; i++)
                {
                    s = s + ", " + _data[i];
                }

                return s + "]";
            }
        }
    }
}
