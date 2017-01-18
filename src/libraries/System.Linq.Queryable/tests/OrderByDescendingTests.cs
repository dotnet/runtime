// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class OrderByDescendingTests : EnumerableBasedTests
    {
        [Fact]
        public void KeySelectorCalled()
        {
            var source = new[]
            {

                new { Name = "Alpha", Score = 90 },
                new { Name = "Robert", Score = 45 },
                new { Name = "Prakash", Score = 99 },
                new { Name = "Bob", Score = 0 }
            };
            var expected = new[]
            {
                new { Name = "Robert", Score = 45 },
                new { Name = "Prakash", Score = 99 },
                new { Name = "Bob", Score = 0 },
                new { Name = "Alpha", Score = 90 }
            };

            Assert.Equal(expected, source.AsQueryable().OrderByDescending(e => e.Name, null));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            string[] source = { "Prakash", "Alpha", "DAN", "dan", "Prakash" };
            string[] expected = { "Prakash", "Prakash", "DAN", "dan", "Alpha" };

            Assert.Equal(expected, source.AsQueryable().OrderByDescending(e => e, StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesNullPassedAsComparer()
        {
            int[] source = { 5, 1, 3, 2, 5 };
            int[] expected = { 5, 5, 3, 2, 1 };

            Assert.Equal(expected, source.AsQueryable().OrderByDescending(e => e, null));
        }

        [Fact]
        public void NullSource()
        {
            IQueryable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.OrderByDescending(i => i));
        }

        [Fact]
        public void NullKeySelector()
        {
            Expression<Func<DateTime, int>> keySelector = null;
            Assert.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<DateTime>().AsQueryable().OrderByDescending(keySelector));
        }

        [Fact]
        public void NullSourceComparer()
        {
            IQueryable<int> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.OrderByDescending(i => i, Comparer<int>.Default));
        }

        [Fact]
        public void NullKeySelectorComparer()
        {
            Expression<Func<DateTime, int>> keySelector = null;
            Assert.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<DateTime>().AsQueryable().OrderByDescending(keySelector, Comparer<int>.Default));
        }

        [Fact]
        public void OrderByDescending1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderByDescending(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void OrderByDescending2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderByDescending(n => n, Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
