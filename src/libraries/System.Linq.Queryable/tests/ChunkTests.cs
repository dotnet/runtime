// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Tests
{
    public class ChunkTests : EnumerableBasedTests
    {

        [Fact]
        public void ThrowsOnNullSource()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Chunk(5));
        }

        [Fact]
        public void Chunk()
        {
            var count = new[] { 0, 1, 2 }.AsQueryable().Chunk(2).Count();
            Assert.Equal(2, count);
        }
    }
}
