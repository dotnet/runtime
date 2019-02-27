// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class RangeTests
    {
        [Fact]
        public static void CreationTest()
        {
            Range range = new Range(new Index(10, fromEnd: false), new Index(2, fromEnd: true));
            Assert.Equal(10, range.Start.Value);
            Assert.False(range.Start.IsFromEnd);
            Assert.Equal(2, range.End.Value);
            Assert.True(range.End.IsFromEnd);

            range = Range.StartAt(new Index(7, fromEnd: false));
            Assert.Equal(7, range.Start.Value);
            Assert.False(range.Start.IsFromEnd);
            Assert.Equal(0, range.End.Value);
            Assert.True(range.End.IsFromEnd);

            range = Range.EndAt(new Index(3, fromEnd: true));
            Assert.Equal(0, range.Start.Value);
            Assert.False(range.Start.IsFromEnd);
            Assert.Equal(3, range.End.Value);
            Assert.True(range.End.IsFromEnd);

            range = Range.All;
            Assert.Equal(0, range.Start.Value);
            Assert.False(range.Start.IsFromEnd);
            Assert.Equal(0, range.End.Value);
            Assert.True(range.End.IsFromEnd);
        }

        [Fact]
        public static void GetOffsetAndLengthTest()
        {
            Range range = Range.StartAt(new Index(5));
            (int offset, int length) = range.GetOffsetAndLength(20);
            Assert.Equal(5, offset);
            Assert.Equal(15, length);

            (offset, length) = range.GetOffsetAndLength(5);
            Assert.Equal(5, offset);
            Assert.Equal(0, length);

            // we don't validate the length in the GetOffsetAndLength so passing negative length will just return the regular calculation according to the length value.
            (offset, length) = range.GetOffsetAndLength(-10);
            Assert.Equal(5, offset);
            Assert.Equal(-15, length);

            Assert.Throws<ArgumentOutOfRangeException>(() => range.GetOffsetAndLength(4));

            range = Range.EndAt(new Index(4));
            (offset, length) = range.GetOffsetAndLength(20);
            Assert.Equal(0, offset);
            Assert.Equal(4, length);
            Assert.Throws<ArgumentOutOfRangeException>(() => range.GetOffsetAndLength(1));
        }

        [Fact]
        public static void EqualityTest()
        {
            Range range1 = new Range(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Range range2 = new Range(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Assert.True(range1.Equals(range2));
            Assert.True(range1.Equals((object)range2));

            range2 = new Range(new Index(10, fromEnd: false), new Index(20, fromEnd: true));
            Assert.False(range1.Equals(range2));
            Assert.False(range1.Equals((object)range2));

            range2 = new Range(new Index(10, fromEnd: false), new Index(21, fromEnd: false));
            Assert.False(range1.Equals(range2));
            Assert.False(range1.Equals((object)range2));
        }

        [Fact]
        public static void HashCodeTest()
        {
            Range range1 = new Range(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Range range2 = new Range(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Assert.Equal(range1.GetHashCode(), range2.GetHashCode());

            range2 = new Range(new Index(10, fromEnd: false), new Index(20, fromEnd: true));
            Assert.NotEqual(range1.GetHashCode(), range2.GetHashCode());

            range2 = new Range(new Index(10, fromEnd: false), new Index(21, fromEnd: false));
            Assert.NotEqual(range1.GetHashCode(), range2.GetHashCode());
        }

        [Fact]
        public static void ToStringTest()
        {
            Range range1 = new Range(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Assert.Equal(10.ToString() + ".." + 20.ToString(), range1.ToString());

            range1 = new Range(new Index(10, fromEnd: false), new Index(20, fromEnd: true));
            Assert.Equal(10.ToString() + "..^" + 20.ToString(), range1.ToString());
        }
    }
}
