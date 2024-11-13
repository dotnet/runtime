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

        [Fact]
        public static void GetOffsetAndLengthTest()
        {
            NRange NativeRange = NRange.StartAt(new NIndex(5));
            (nint offset, nint length) = NativeRange.GetOffsetAndLength(20);
            Assert.Equal(5, offset);
            Assert.Equal(15, length);

            (offset, length) = NativeRange.GetOffsetAndLength(5);
            Assert.Equal(5, offset);
            Assert.Equal(0, length);

            // we don't validate the length in the GetOffsetAndLength so passing negative length will just return the regular calculation according to the length value.
            (offset, length) = NativeRange.GetOffsetAndLength(-10);
            Assert.Equal(5, offset);
            Assert.Equal(-15, length);

            Assert.Throws<ArgumentOutOfRangeException>(() => NativeRange.GetOffsetAndLength(4));

            NativeRange = NRange.EndAt(new NIndex(4));
            (offset, length) = NativeRange.GetOffsetAndLength(20);
            Assert.Equal(0, offset);
            Assert.Equal(4, length);
            Assert.Throws<ArgumentOutOfRangeException>(() => NativeRange.GetOffsetAndLength(1));
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

        [Fact]
        public static void ToStringTest()
        {
            NRange NativeRange1 = new NRange(new NIndex(10, fromEnd: false), new NIndex(20, fromEnd: false));
            Assert.Equal(10.ToString() + ".." + 20.ToString(), NativeRange1.ToString());

            NativeRange1 = new NRange(new NIndex(10, fromEnd: false), new NIndex(20, fromEnd: true));
            Assert.Equal(10.ToString() + "..^" + 20.ToString(), NativeRange1.ToString());
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
