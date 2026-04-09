// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class RepeatTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Repeat("a", -1));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Repeat("42", 0));
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable()
        {
            foreach (int count in new[] { 0, 1, 10 })
            {
                await AssertEqual(
                    Enumerable.Repeat(42, count),
                    AsyncEnumerable.Repeat(42, count));

                await AssertEqual(
                    Enumerable.Repeat("test", count),
                    AsyncEnumerable.Repeat("test", count));
            }
        }
    }
}
