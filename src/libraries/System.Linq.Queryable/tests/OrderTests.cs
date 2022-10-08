// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public sealed class OrderTests : EnumerableBasedTests
    {
        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            string[] source = { "Prakash", "Alpha", "dan", "DAN", "Prakash" };
            string[] expected = { "Alpha", "dan", "DAN", "Prakash", "Prakash" };

            Assert.Equal(expected, source.AsQueryable().Order(StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesNullPassedAsComparer()
        {
            int[] source = { 5, 1, 3, 2, 5 };
            int[] expected = { 1, 2, 3, 5, 5 };

            Assert.Equal(expected, source.AsQueryable().Order(null));
        }

        [Fact]
        public void NullSource()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Order());
        }

        [Fact]
        public void NullSourceComparer()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Order(Comparer<int>.Default));
        }

        [Fact]
        public void Order1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Order().Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Order2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().Order(Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
