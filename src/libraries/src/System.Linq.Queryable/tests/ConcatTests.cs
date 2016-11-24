// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Tests
{
    public class ConcatTests : EnumerableBasedTests
    {
        [Fact]
        public void BothEmpty()
        {
            int[] first = { };
            int[] second = { };
            Assert.Empty(first.AsQueryable().Concat(second.AsQueryable()));
        }

        [Fact]
        public void NonEmptyAndNonEmpty()
        {
            int?[] first = { 2, null, 3, 5, 9 };
            int?[] second = { null, 8, 10 };
            int?[] expected = { 2, null, 3, 5, 9, null, 8, 10 };

            Assert.Equal(expected, first.AsQueryable().Concat(second.AsQueryable()));
        }

        [Fact]
        public void FirstNull()
        {
            Assert.Throws<ArgumentNullException>("source1", () => ((IQueryable<int>)null).Concat(Enumerable.Range(0, 0).AsQueryable()));
        }

        [Fact]
        public void SecondNull()
        {
            Assert.Throws<ArgumentNullException>("source2", () => Enumerable.Range(0, 0).AsQueryable().Concat(null));
        }

        [Fact]
        public void Concat()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Concat((new int[] { 10, 11, 12 }).AsQueryable()).Count();
            Assert.Equal(6, count);
        }
    }
}
