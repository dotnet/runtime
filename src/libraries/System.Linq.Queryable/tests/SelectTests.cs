// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests
{
    public class SelectTests : EnumerableBasedTests
    {
        [Fact]
        public void SelectProperty()
        {
            var source = new []{
                new { name="Prakash", custID=98088 },
                new { name="Bob", custID=29099 },
                new { name="Chris", custID=39033 },
                new { name=(string)null, custID=30349 },
                new { name="Prakash", custID=39030 }
            };
            string[] expected = { "Prakash", "Bob", "Chris", null, "Prakash" };
            Assert.Equal(expected, source.AsQueryable().Select(e => e.name));
        }

        [Fact]
        public void SelectPropertyUsingIndex()
        {
            var source = new[]{
                new { name="Prakash", custID=98088 },
                new { name="Bob", custID=29099 },
                new { name="Chris", custID=39033 }
            };
            string[] expected = { "Prakash", null, null };
            Assert.Equal(expected, source.AsQueryable().Select((e, i) => i == 0 ? e.name : null));
        }

        [Fact]
        public void Select_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Select(i => i + 1));
        }

        [Fact]
        public void Select_SelectorIsNull_ArgumentNullExceptionThrown_Indexed()
        {
            IQueryable<int> source = Enumerable.Range(1, 10).AsQueryable();
            Expression<Func<int, int, int>> selector = null;

            AssertExtensions.Throws<ArgumentNullException>("selector", () => source.Select(selector));
        }

        [Fact]
        public void Select_SourceIsNull_ArgumentNullExceptionThrown_Indexed()
        {
            IQueryable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Select((e, i) => i + 1));
        }

        [Fact]
        public void Select_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IQueryable<int> source = Enumerable.Range(1, 10).AsQueryable();
            Expression<Func<int, int>> selector = null;

            AssertExtensions.Throws<ArgumentNullException>("selector", () => source.Select(selector));
        }

        [Fact]
        public void Select1()
        {
            var count = (new object[] { 0, 1, 2 }).AsQueryable().Select(o => (int)o).Count();
            Assert.Equal(3, count);
        }

        [Fact]
        public void Select2()
        {
            var count = (new object[] { 0, 1, 2 }).AsQueryable().Select((o, i) => (int)o + i).Count();
            Assert.Equal(3, count);
        }
    }
}
