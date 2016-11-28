// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            IQueryable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.Take(5));
        }

        [Fact]
        public void Take()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Take(2).Count();
            Assert.Equal(2, count);
        }
    }
}
