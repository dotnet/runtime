// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Linq.Tests
{
    public class SkipTests : EnumerableBasedTests
    {
        [Fact]
        public void SkipSome()
        {
            Assert.Equal(Enumerable.Range(10, 10).AsQueryable(), Enumerable.Range(0, 20).AsQueryable().Skip(10));
        }

        [Fact]
        public void SkipExcessive()
        {
            Assert.Empty(Enumerable.Range(0, 20).AsQueryable().Skip(42));
        }

        [Fact]
        public void SkipThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<DateTime>)null).Skip(3));
        }

        [Fact]
        public void Skip()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Skip(1).Count();
            Assert.Equal(2, count);
        }
    }
}
