// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Tests
{
    public class ElementAtTests : EnumerableBasedTests
    {
        [Fact]
        public void IndexNegative()
        {
            int?[] source = { 9, 8 };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.AsQueryable().ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.AsQueryable().ElementAt(^3));
        }

        [Fact]
        public void IndexEqualsCount()
        {
            int[] source = { 1, 2, 3, 4 };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.AsQueryable().ElementAt(source.Length));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.AsQueryable().ElementAt(new Index(source.Length)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.AsQueryable().ElementAt(^0));
        }

        [Fact]
        public void EmptyIndexZero()
        {
            int[] source = { };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.AsQueryable().ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.AsQueryable().ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.AsQueryable().ElementAt(^0));
        }

        [Fact]
        public void SingleElementIndexZero()
        {
            int[] source = { -4 };

            Assert.Equal(-4, source.AsQueryable().ElementAt(0));
            Assert.Equal(-4, source.AsQueryable().ElementAt(new Index(0)));
            Assert.Equal(-4, source.AsQueryable().ElementAt(^1));
        }

        [Fact]
        public void ManyElementsIndexTargetsLast()
        {
            int[] source = { 9, 8, 0, -5, 10 };

            Assert.Equal(10, source.AsQueryable().ElementAt(source.Length - 1));
            Assert.Equal(10, source.AsQueryable().ElementAt(source.Length - 1));
            Assert.Equal(10, source.AsQueryable().ElementAt(^1));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).ElementAt(2));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).ElementAt(new Index(2)));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).ElementAt(^2));
        }

        [Fact]
        public void ElementAt()
        {
            var val1 = new[] { 0, 2, 1 }.AsQueryable().ElementAt(1);
            Assert.Equal(2, val1);

            var val2 = new[] { 0, 2, 1 }.AsQueryable().ElementAt(new Index(1));
            Assert.Equal(2, val2);

            var val3 = new[] { 0, 2, 1 }.AsQueryable().ElementAt(^2);
            Assert.Equal(2, val3);
        }
    }
}
