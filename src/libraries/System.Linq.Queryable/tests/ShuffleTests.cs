// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Tests
{
    public class ShuffleTests : EnumerableBasedTests
    {
        [Fact]
        public void InvalidArguments()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<string>)null).Shuffle());
        }

        [Fact]
        public void ProducesAllElements()
        {
            int[] shuffled = Enumerable.Range(0, 1000).AsQueryable().Shuffle().ToArray();
            Array.Sort(shuffled);
            Assert.Equal(Enumerable.Range(0, shuffled.Length), shuffled);
        }

        [Fact]
        public void ElementsAreRandomized()
        {
            // The chance that shuffling a thousand elements produces the same order twice is infinitesimal.
            const int Length = 1000;
            IQueryable<int> source = Enumerable.Range(0, Length).AsQueryable().Shuffle();
            int[] first = source.ToArray();
            int[] second = source.ToArray();
            Assert.Equal(Length, first.Length);
            Assert.Equal(Length, second.Length);
            Assert.NotEqual(first, second);
        }
    }
}
