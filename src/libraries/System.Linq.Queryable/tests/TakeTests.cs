// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Tests
{
    public class TakeTests : EnumerableBasedTests
    {
        [Fact]
        public void SourceNonEmptyTakeAllButOne()
        {
            int[] source = { 2, 5, 9, 1 };
            int[] expected = { 2, 5, 9 };

            Assert.Equal(expected, source.AsQueryable().Take(3));
            Assert.Equal(expected, source.AsQueryable().Take(0..3));
            Assert.Equal(expected, source.AsQueryable().Take(^4..3));
            Assert.Equal(expected, source.AsQueryable().Take(0..^1));
            Assert.Equal(expected, source.AsQueryable().Take(^4..^1));
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Take(5));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Take(0..5));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Take(^5..5));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Take(0..^0));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Take(^5..^0));
        }

        [Fact]
        public void Take()
        {
            var count1 = new[] { 0, 1, 2 }.AsQueryable().Take(2).Count();
            Assert.Equal(2, count1);

            var count2 = new[] { 0, 1, 2 }.AsQueryable().Take(0..2).Count();
            Assert.Equal(2, count2);

            var count3 = new[] { 0, 1, 2 }.AsQueryable().Take(^3..2).Count();
            Assert.Equal(2, count3);

            var count4 = new[] { 0, 1, 2 }.AsQueryable().Take(0..^1).Count();
            Assert.Equal(2, count4);

            var count5 = new[] { 0, 1, 2 }.AsQueryable().Take(^3..^1).Count();
            Assert.Equal(2, count5);

            var count6 = new[] { 0, 1, 2 }.AsQueryable().Take(1..3).Count();
            Assert.Equal(2, count6);

            var count7 = new[] { 0, 1, 2 }.AsQueryable().Take(^2..3).Count();
            Assert.Equal(2, count7);

            var count8 = new[] { 0, 1, 2 }.AsQueryable().Take(1..^0).Count();
            Assert.Equal(2, count8);

            var count9 = new[] { 0, 1, 2 }.AsQueryable().Take(^2..^0).Count();
            Assert.Equal(2, count9);
        }
    }
}
