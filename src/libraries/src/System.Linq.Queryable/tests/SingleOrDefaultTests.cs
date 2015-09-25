// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class SingleOrDefaultTests : EnumerableBasedTests
    {
        [Fact]
        public void ManyMatch()
        {
            int[] source = { 4, 4, 4, 4, 4 };

            Assert.Throws<InvalidOperationException>(() => source.AsQueryable().SingleOrDefault());
        }

        [Fact]
        public void Empty()
        {
            Assert.Null(Enumerable.Empty<int?>().AsQueryable().SingleOrDefault());
        }

        [Fact]
        public void EmptySourceWithPredicate()
        {
            Assert.Null(Enumerable.Empty<int?>().AsQueryable().SingleOrDefault(i => i % 2 == 0));
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(42, 100)]
        public void FindSingleMatch(int target, int range)
        {
            Assert.Equal(target, Enumerable.Range(0, range).AsQueryable().SingleOrDefault(i => i == target));
        }
        
        [Fact]
        public void ThrowsOnNullSource()
        {
            IQueryable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SingleOrDefault());
            Assert.Throws<ArgumentNullException>("source", () => source.SingleOrDefault(i => i % 2 == 0));
        }

        [Fact]
        public void ThrowsOnNullPredicate()
        {
            int[] source = { };
            Expression<Func<int, bool>> nullPredicate = null;
            Assert.Throws<ArgumentNullException>("predicate", () => source.AsQueryable().SingleOrDefault(nullPredicate));
        }

        [Fact]
        public void SingleOrDefault1()
        {
            var val = (new int[] { 2 }).AsQueryable().SingleOrDefault();
            Assert.Equal(2, val);
        }

        [Fact]
        public void SingleOrDefault2()
        {
            var val = (new int[] { 2 }).AsQueryable().SingleOrDefault(n => n > 1);
            Assert.Equal(2, val);
        }
    }
}
