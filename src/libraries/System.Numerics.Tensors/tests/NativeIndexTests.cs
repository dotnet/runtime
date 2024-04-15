// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static class NativeIndexTests
    {
        [Fact]
        public static void CreationTest()
        {
            NativeIndex index = new NativeIndex(1, fromEnd: false);
            Assert.Equal(1, index.Value);
            Assert.False(index.IsFromEnd);

            index = new NativeIndex(11, fromEnd: true);
            Assert.Equal(11, index.Value);
            Assert.True(index.IsFromEnd);

            index = NativeIndex.Start;
            Assert.Equal(0, index.Value);
            Assert.False(index.IsFromEnd);

            index = NativeIndex.End;
            Assert.Equal(0, index.Value);
            Assert.True(index.IsFromEnd);

            index = NativeIndex.FromStart(3);
            Assert.Equal(3, index.Value);
            Assert.False(index.IsFromEnd);

            index = NativeIndex.FromEnd(10);
            Assert.Equal(10, index.Value);
            Assert.True(index.IsFromEnd);

            // Make sure can implicitly convert form Index
            index = Index.FromEnd(10);
            Assert.Equal(10, index.Value);
            Assert.True(index.IsFromEnd);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => new NativeIndex(-1, fromEnd: false));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => NativeIndex.FromStart(-3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => NativeIndex.FromEnd(-1));
        }

        [Fact]
        public static void GetOffsetTest()
        {
            NativeIndex index = NativeIndex.FromStart(3);
            Assert.Equal(3, index.GetOffset(3));
            Assert.Equal(3, index.GetOffset(10));
            Assert.Equal(3, index.GetOffset(20));

            // we don't validate the length in the GetOffset so passing short length will just return the regular calculation according to the length value.
            Assert.Equal(3, index.GetOffset(2));

            index = NativeIndex.FromEnd(3);
            Assert.Equal(0, index.GetOffset(3));
            Assert.Equal(7, index.GetOffset(10));
            Assert.Equal(17, index.GetOffset(20));

            // we don't validate the length in the GetOffset so passing short length will just return the regular calculation according to the length value.
            Assert.Equal(-1, index.GetOffset(2));
        }

        [Fact]
        public static void ImplicitCastTest()
        {
            NativeIndex index = 10;
            Assert.Equal(10, index.Value);
            Assert.False(index.IsFromEnd);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => index = -10);
        }

        [Fact]
        public static void EqualityTest()
        {
            NativeIndex index1 = 10;
            NativeIndex index2 = 10;
            Assert.True(index1.Equals(index2));
            Assert.True(index1.Equals((object)index2));

            index2 = new NativeIndex(10, fromEnd: true);
            Assert.False(index1.Equals(index2));
            Assert.False(index1.Equals((object)index2));

            index2 = new NativeIndex(9, fromEnd: false);
            Assert.False(index1.Equals(index2));
            Assert.False(index1.Equals((object)index2));
        }

        [Fact]
        public static void HashCodeTest()
        {
            NativeIndex index1 = 10;
            NativeIndex index2 = 10;
            Assert.Equal(index1.GetHashCode(), index2.GetHashCode());

            index2 = new NativeIndex(10, fromEnd: true);
            Assert.NotEqual(index1.GetHashCode(), index2.GetHashCode());

            index2 = new NativeIndex(99999, fromEnd: false);
            Assert.NotEqual(index1.GetHashCode(), index2.GetHashCode());
        }

        [Fact]
        public static void ToStringTest()
        {
            NativeIndex index1 = 100;
            Assert.Equal(100.ToString(), index1.ToString());

            index1 = new NativeIndex(50, fromEnd: true);
            Assert.Equal("^" + 50.ToString(), index1.ToString());
        }

        [Fact]
        public static void CollectionTest()
        {
            int[] array = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            List<int> list = new List<int>(array);

            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(i, list[(int)NativeIndex.FromStart(i).Value]);
                Assert.Equal(list.Count - i - 1, list[^(i + 1)]);

                Assert.Equal(i, array[NativeIndex.FromStart(i).Value]);
                Assert.Equal(list.Count - i - 1, array[^(i + 1)]);

                Assert.Equal(array.AsSpan(i, array.Length - i).ToArray(), array[i..]);
            }
        }
    }
}
