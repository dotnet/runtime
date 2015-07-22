// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Tests.Helpers;
using Xunit;

namespace System.Linq.Tests
{
    public class SingleTests
    {
        [Fact]
        public void FailOnEmpty()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Single());
        }

        [Fact]
        public void DefaultOnEmpty()
        {
            Assert.Equal(0, Enumerable.Empty<int>().SingleOrDefault());
        }

        [Fact]
        public void FindSingle()
        {
            Assert.Equal(42, Enumerable.Repeat(42, 1).Single());
        }

        [Theory]
        [InlineData(1, 100)]
        [InlineData(42, 100)]
        public void FindSingleMatch(int target, int range)
        {
            Assert.Equal(target, Enumerable.Range(0, range).Single(i => i == target));
        }
    }
}