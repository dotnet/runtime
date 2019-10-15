// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ThenByTests
    {
        [Fact]
        public void SecondaryKeysAreUnique()
        {
            var source = new[]
            {
                new { Name = "Jim", City = "Minneapolis", Country = "USA" },
                new { Name = "Tim", City = "Seattle", Country = "USA" },
                new { Name = "Philip", City = "Orlando", Country = "USA" },
                new { Name = "Chris", City = "London", Country = "UK" },
                new { Name = "Rob", City = "Kent", Country = "UK" }
            };
            var expected = new[]
            {
                new { Name = "Rob", City = "Kent", Country = "UK" },
                new { Name = "Chris", City = "London", Country = "UK" },
                new { Name = "Jim", City = "Minneapolis", Country = "USA" },
                new { Name = "Philip", City = "Orlando", Country = "USA" },
                new { Name = "Tim", City = "Seattle", Country = "USA" }
            };

            Assert.Equal(expected, source.AsQueryable().OrderBy(e => e.Country).ThenBy(e => e.City));
        }

        [Fact]
        public void NullSource()
        {
            IOrderedQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ThenBy(i => i));
        }

        [Fact]
        public void NullKeySelector()
        {
            Expression<Func<DateTime, int>> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<DateTime>().AsQueryable().OrderBy(e => e).ThenBy(keySelector));
        }

        [Fact]
        public void NullSourceComparer()
        {
            IOrderedQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.ThenBy(i => i, null));
        }

        [Fact]
        public void NullKeySelectorComparer()
        {
            Expression<Func<DateTime, int>> keySelector = null;
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<DateTime>().AsQueryable().OrderBy(e => e).ThenBy(keySelector, null));
        }

        [Fact]
        public void ThenBy1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n).ThenBy(n => n).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void ThenBy2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().OrderBy(n => n).ThenBy(n => n, Comparer<int>.Default).Count();
            Assert.Equal(3, count);
        }
    }
}
