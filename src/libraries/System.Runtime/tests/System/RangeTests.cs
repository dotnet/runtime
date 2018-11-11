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
            Range range = Range.Create(new Index(10, fromEnd: false), new Index(2, fromEnd: true));
            Assert.Equal(10, range.Start.Value);
            Assert.False(range.Start.FromEnd);
            Assert.Equal(2, range.End.Value);
            Assert.True(range.End.FromEnd);

            range = Range.FromStart(new Index(7, fromEnd: false));
            Assert.Equal(7, range.Start.Value);
            Assert.False(range.Start.FromEnd);
            Assert.Equal(0, range.End.Value);
            Assert.True(range.End.FromEnd);

            range = Range.ToEnd(new Index(3, fromEnd: true));
            Assert.Equal(0, range.Start.Value);
            Assert.False(range.Start.FromEnd);
            Assert.Equal(3, range.End.Value);
            Assert.True(range.End.FromEnd);

            range = Range.All();
            Assert.Equal(0, range.Start.Value);
            Assert.False(range.Start.FromEnd);
            Assert.Equal(0, range.End.Value);
            Assert.True(range.End.FromEnd);
        }

        [Fact]
        public static void EqualityTest()
        {
            Range range1 = Range.Create(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Range range2 = Range.Create(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Assert.True(range1.Equals(range2));
            Assert.True(range1.Equals((object)range2));

            range2 = Range.Create(new Index(10, fromEnd: false), new Index(20, fromEnd: true));
            Assert.False(range1.Equals(range2));
            Assert.False(range1.Equals((object)range2));

            range2 = Range.Create(new Index(10, fromEnd: false), new Index(21, fromEnd: false));
            Assert.False(range1.Equals(range2));
            Assert.False(range1.Equals((object)range2));
        }

        [Fact]
        public static void HashCodeTest()
        {
            Range range1 = Range.Create(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Range range2 = Range.Create(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Assert.Equal(range1.GetHashCode(), range2.GetHashCode());

            range2 = Range.Create(new Index(10, fromEnd: false), new Index(20, fromEnd: true));
            Assert.NotEqual(range1.GetHashCode(), range2.GetHashCode());

            range2 = Range.Create(new Index(10, fromEnd: false), new Index(21, fromEnd: false));
            Assert.NotEqual(range1.GetHashCode(), range2.GetHashCode());
        }

        [Fact]
        public static void ToStringTest()
        {
            Range range1 = Range.Create(new Index(10, fromEnd: false), new Index(20, fromEnd: false));
            Assert.Equal(10.ToString() + ".." + 20.ToString(), range1.ToString());

            range1 = Range.Create(new Index(10, fromEnd: false), new Index(20, fromEnd: true));
            Assert.Equal(10.ToString() + "..^" + 20.ToString(), range1.ToString());
        }
    }
}
