// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Buffers.Tests
{
    public static class NativeIndexTests
    {
        [Fact]
        public static void CreationTest()
        {
            NIndex index = new NIndex(1, fromEnd: false);
            Assert.Equal(1, index.Value);
            Assert.False(index.IsFromEnd);

            index = new NIndex(11, fromEnd: true);
            Assert.Equal(11, index.Value);
            Assert.True(index.IsFromEnd);

            index = NIndex.Start;
            Assert.Equal(0, index.Value);
            Assert.False(index.IsFromEnd);

            index = NIndex.End;
            Assert.Equal(0, index.Value);
            Assert.True(index.IsFromEnd);

            index = NIndex.FromStart(3);
            Assert.Equal(3, index.Value);
            Assert.False(index.IsFromEnd);

            index = NIndex.FromEnd(10);
            Assert.Equal(10, index.Value);
            Assert.True(index.IsFromEnd);

            // Make sure can implicitly convert form Index
            index = Index.FromEnd(10);
            Assert.Equal(10, index.Value);
            Assert.True(index.IsFromEnd);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => new NIndex(-1, fromEnd: false));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => NIndex.FromStart(-3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => NIndex.FromEnd(-1));
        }

        [Fact]
        public static void GetOffsetTest()
        {
            NIndex index = NIndex.FromStart(3);
            Assert.Equal(3, index.GetOffset(3));
            Assert.Equal(3, index.GetOffset(10));
            Assert.Equal(3, index.GetOffset(20));

            // we don't validate the length in the GetOffset so passing short length will just return the regular calculation according to the length value.
            Assert.Equal(3, index.GetOffset(2));

            index = NIndex.FromEnd(3);
            Assert.Equal(0, index.GetOffset(3));
            Assert.Equal(7, index.GetOffset(10));
            Assert.Equal(17, index.GetOffset(20));

            // we don't validate the length in the GetOffset so passing short length will just return the regular calculation according to the length value.
            Assert.Equal(-1, index.GetOffset(2));
        }

        [Fact]
        public static void ImplicitCastTest()
        {
            NIndex index = 10;
            Assert.Equal(10, index.Value);
            Assert.False(index.IsFromEnd);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => index = -10);
        }

        [Fact]
        public static void EqualityTest()
        {
            NIndex index1 = 10;
            NIndex index2 = 10;
            Assert.True(index1.Equals(index2));
            Assert.True(index1.Equals((object)index2));

            index2 = new NIndex(10, fromEnd: true);
            Assert.False(index1.Equals(index2));
            Assert.False(index1.Equals((object)index2));

            index2 = new NIndex(9, fromEnd: false);
            Assert.False(index1.Equals(index2));
            Assert.False(index1.Equals((object)index2));
        }

        [Fact]
        public static void HashCodeTest()
        {
            NIndex index1 = 10;
            NIndex index2 = 10;
            Assert.Equal(index1.GetHashCode(), index2.GetHashCode());

            index2 = new NIndex(101, fromEnd: true);
            Assert.NotEqual(index1.GetHashCode(), index2.GetHashCode());

            index2 = new NIndex(99999, fromEnd: false);
            Assert.NotEqual(index1.GetHashCode(), index2.GetHashCode());
        }

        [Theory]
        [InlineData(0, false, "0")]
        [InlineData(100, false, "100")]
        [InlineData(0, true, "^0")]
        [InlineData(50, true, "^50")]
        [InlineData(int.MaxValue, false, "2147483647")]
        [InlineData(int.MaxValue, true, "^2147483647")]
        public static void ToStringTest(long value, bool fromEnd, string expected)
        {
            NIndex index = new NIndex((nint)value, fromEnd);
            Assert.Equal(expected, index.ToString());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(1L + uint.MaxValue, false, "4294967296")]
        [InlineData(1L + uint.MaxValue, true, "^4294967296")]
        [InlineData(long.MaxValue, false, "9223372036854775807")]
        [InlineData(long.MaxValue, true, "^9223372036854775807")]
        public static void ToStringTest_64bit(long value, bool fromEnd, string expected)
        {
            NIndex index = new NIndex((nint)value, fromEnd);
            Assert.Equal(expected, index.ToString());
        }

        [Fact]
        public static void CollectionTest()
        {
            int[] array = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            List<int> list = new List<int>(array);

            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(i, list[(int)NIndex.FromStart(i).Value]);
                Assert.Equal(list.Count - i - 1, list[^(i + 1)]);

                Assert.Equal(i, array[NIndex.FromStart(i).Value]);
                Assert.Equal(list.Count - i - 1, array[^(i + 1)]);

                Assert.Equal(array.AsSpan(i, array.Length - i).ToArray(), array[i..]);
            }
        }
    }
}
