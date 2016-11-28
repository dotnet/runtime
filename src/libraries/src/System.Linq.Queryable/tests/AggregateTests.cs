// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class AggregateTests : EnumerableBasedTests
    {
        [Fact]
        public void MultipleElements()
        {
            int[] source = { 5, 6, 0, -4 };
            int expected = 7;

            Assert.Equal(expected, source.AsQueryable().Aggregate((x, y) => x + y));
        }

        [Fact]
        public void MultipleElementsAndSeed()
        {
            int[] source = { 5, 6, 2, -4 };
            long seed = 2;
            long expected = -480;

            Assert.Equal(expected, source.AsQueryable().Aggregate(seed, (x, y) => x * y));
        }

        [Fact]
        public void MultipleElementsSeedResultSelector()
        {
            int[] source = { 5, 6, 2, -4 };
            long seed = 2;
            long expected = -475;

            Assert.Equal(expected, source.AsQueryable().Aggregate(seed, (x, y) => x * y, x => x + 5.0));
        }

        [Fact]
        public void NullSource()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Aggregate((x, y) => x + y));
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Aggregate(0, (x, y) => x + y));
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).Aggregate(0, (x, y) => x + y, i => i));
        }

        [Fact]
        public void NullFunc()
        {
            Expression<Func<int, int, int>> func = null;
            Assert.Throws<ArgumentNullException>("func", () => Enumerable.Range(0, 3).AsQueryable().Aggregate(func));
            Assert.Throws<ArgumentNullException>("func", () => Enumerable.Range(0, 3).AsQueryable().Aggregate(0, func));
            Assert.Throws<ArgumentNullException>("func", () => Enumerable.Range(0, 3).AsQueryable().Aggregate(0, func, i => i));
        }

        [Fact]
        public void NullResultSelector()
        {
            Expression<Func<int, int>> resultSelector = null;
            Assert.Throws<ArgumentNullException>("selector", () => Enumerable.Range(0, 3).AsQueryable().Aggregate(0, (x, y) => x + y, resultSelector));
        }

        [Fact]
        public void Aggregate1()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Aggregate((n1, n2) => n1 + n2);
            Assert.Equal((int)3, val);
        }

        [Fact]
        public void Aggregate2()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Aggregate("", (n1, n2) => n1 + n2.ToString());
            Assert.Equal("021", val);
        }

        [Fact]
        public void Aggregate3()
        {
            var val = (new int[] { 0, 2, 1 }).AsQueryable().Aggregate(0L, (n1, n2) => n1 + n2, n => n.ToString());
            Assert.Equal("3", val);
        }
    }
}
