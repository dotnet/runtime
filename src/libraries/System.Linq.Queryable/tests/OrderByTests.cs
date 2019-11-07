// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class OrderByTests : EnumerableBasedTests
    {
        [Fact]
        public void KeySelectorCalled()
        {
            var source = new[]
            {
                new { Name = "Tim", Score = 90 },
                new { Name = "Robert", Score = 45 },
                new { Name = "Prakash", Score = 99 }
            };
            var expected = new[]
            {
                new { Name = "Prakash", Score = 99 },
                new { Name = "Robert", Score = 45 },
                new { Name = "Tim", Score = 90 }
            };

            Assert.Equal(expected, source.AsQueryable().OrderBy(e => e.Name, null));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            string[] source = { "Prakash", "Alpha", "dan", "DAN", "Prakash" };
            string[] expected = { "Alpha", "dan", "DAN", "Prakash", "Prakash" };

            Assert.Equal(expected, source.AsQueryable().OrderBy(e => e, StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesNullPassedAsComparer()
        {
            int[] source = { 5, 1, 3, 2, 5 };
            int[] expected = { 1, 2, 3, 5, 5 };

            Assert.Equal(expected, source.AsQueryable().OrderBy(e => e, null));
        }

        [Fact]
        public void NullSource()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.OrderBy(i => i));
        }

        [Fact]
        public void NullKeySelector()
        {
            Expression<Func<DateTime, int>> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<DateTime>().AsQueryable().OrderBy(keySelector));
        }

        [Fact]
        public void NullSourceComparer()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.OrderBy(i => i, Comparer<int>.Default));
        }

        [Fact]
        public void NullKeySelectorComparer()
        {
            Expression<Func<DateTime, int>> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<DateTime>().AsQueryable().OrderBy(keySelector, Comparer<int>.Default));
        }

        [Fact]
        public void OrderBy1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void OrderBy2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n, Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
