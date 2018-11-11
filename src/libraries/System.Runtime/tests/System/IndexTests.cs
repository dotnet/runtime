// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class IndexTests
    {
        [Fact]
        public static void CreationTest()
        {
            Index index = new Index(1, fromEnd: false);
            Assert.Equal(1, index.Value);
            Assert.False(index.FromEnd);

            index = new Index(11, fromEnd: true);
            Assert.Equal(11, index.Value);
            Assert.True(index.FromEnd);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => new Index(-1, fromEnd: false));
        }

        [Fact]
        public static void ImplicitCastTest()
        {
            Index index = 10;
            Assert.Equal(10, index.Value);
            Assert.False(index.FromEnd);
        }

        [Fact]
        public static void EqualityTest()
        {
            Index index1 = 10;
            Index index2 = 10;
            Assert.True(index1.Equals(index2));
            Assert.True(index1.Equals((object)index2));

            index2 = new Index(10, fromEnd: true);
            Assert.False(index1.Equals(index2));
            Assert.False(index1.Equals((object)index2));

            index2 = new Index(9, fromEnd: false);
            Assert.False(index1.Equals(index2));
            Assert.False(index1.Equals((object)index2));
        }

        [Fact]
        public static void HashCodeTest()
        {
            Index index1 = 10;
            Index index2 = 10;
            Assert.Equal(index1.GetHashCode(), index2.GetHashCode());

            index2 = new Index(10, fromEnd: true);
            Assert.NotEqual(index1.GetHashCode(), index2.GetHashCode());

            index2 = new Index(99999, fromEnd: false);
            Assert.NotEqual(index1.GetHashCode(), index2.GetHashCode());
        }

        [Fact]
        public static void ToStringTest()
        {
            Index index1 = 100;
            Assert.Equal(100.ToString(), index1.ToString());

            index1 = new Index(50, fromEnd: true);
            Assert.Equal("^" + 50.ToString(), index1.ToString());
        }
    }
}
