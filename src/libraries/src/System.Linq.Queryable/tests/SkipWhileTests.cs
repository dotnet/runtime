// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class SkipWhileTests : EnumerableBasedTests
    {
        [Fact]
        public void SkipWhileAllTrue()
        {
            Assert.Empty(Enumerable.Range(0, 20).AsQueryable().SkipWhile(i => i < 40));
            Assert.Empty(Enumerable.Range(0, 20).AsQueryable().SkipWhile((i, idx) => i == idx));
        }

        [Fact]
        public void SkipWhileAllFalse()
        {
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).AsQueryable().SkipWhile(i => i != 0));
            Assert.Equal(Enumerable.Range(0, 20), Enumerable.Range(0, 20).AsQueryable().SkipWhile((i, idx) => i != idx));
        }

        [Fact]
        public void SkipWhileThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).SkipWhile(i => i < 40));
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<int>)null).SkipWhile((i, idx) => i == idx));
            Assert.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 20).AsQueryable().SkipWhile((Expression<Func<int, int, bool>>)null));
            Assert.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 20).AsQueryable().SkipWhile((Expression<Func<int, bool>>)null));
        }

        [Fact]
        public void SkipWhile1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SkipWhile(n => n < 1).Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void SkipWhile2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SkipWhile((n, i) => n + i < 1).Count();
            Assert.Equal(2, count);
        }
    }
}
