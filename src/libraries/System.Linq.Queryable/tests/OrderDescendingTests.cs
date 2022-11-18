// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public sealed class OrderDescendingTests : EnumerableBasedTests
    {
        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            string[] source = { "Prakash", "Alpha", "DAN", "dan", "Prakash" };
            string[] expected = { "Prakash", "Prakash", "DAN", "dan", "Alpha" };

            Assert.Equal(expected, source.AsQueryable().OrderDescending(StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesNullPassedAsComparer()
        {
            int[] source = { 5, 1, 3, 2, 5 };
            int[] expected = { 5, 5, 3, 2, 1 };

            Assert.Equal(expected, source.AsQueryable().OrderDescending(null));
        }

        [Fact]
        public void NullSource()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.OrderDescending());
        }

        [Fact]
        public void NullSourceComparer()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.OrderDescending(Comparer<int>.Default));
        }

        [Fact]
        public void OrderDescending1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderDescending().Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void OrderDescending2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderDescending(Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
