// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class RangeTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range(-1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range(-1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range(int.MaxValue - 1, 3));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Range(42, 0));
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable()
        {
            foreach (int start in new[] { int.MinValue, -1, 0, 1, 1_000_000 })
            {
                foreach (int count in new[] { 0, 1, 3, 10 })
                {
                    await AssertEqual(
                        Enumerable.Range(start, count),
                        AsyncEnumerable.Range(start, count));
                }
            }
        }
    }
}
