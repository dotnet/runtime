// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SelectManyTests : EnumerableBasedTests
    {
        [Fact]
        public void ResultsSelected()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
            Assert.Equal(expected, source.AsQueryable().SelectMany(e => e.total));
        }

        [Fact]
        public void ResultsSelectedIndexUsed()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            int?[] expected = { 1, 2, 3, 4, 5, 6, 8, 9, -10, 100 };
            Assert.Equal(expected, source.AsQueryable().SelectMany((e, index) => e.total));
        }

        [Fact]
        public void ResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            string[] expected = { "1", "2", "3", "4", "5", "6", "8", "9", "-10", "100" };

            Assert.Equal(expected, source.AsQueryable().SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullResultSelector()
        {
            Expression<Func<StringWithIntArray, int?, string>> resultSelector = null;
            Assert.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Empty<StringWithIntArray>().AsQueryable().SelectMany(e => e.total, resultSelector));
        }

        [Fact]
        public void NullResultSelectorIndexedSelector()
        {
            Expression<Func<StringWithIntArray, int?, string>> resultSelector = null;
            Assert.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Empty<StringWithIntArray>().AsQueryable().SelectMany((e, i) => e.total, resultSelector));
        }

        [Fact]
        public void NullSourceWithResultSelector()
        {
            IQueryable<StringWithIntArray> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullCollectionSelector()
        {
            Expression<Func<StringWithIntArray, IEnumerable<int?>>> collectionSelector = null;
            Assert.Throws<ArgumentNullException>("collectionSelector", () => Enumerable.Empty<StringWithIntArray>().AsQueryable().SelectMany(collectionSelector, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullIndexedCollectionSelector()
        {
            Expression<Func<StringWithIntArray, int, IEnumerable<int?>>> collectionSelector = null;
            Assert.Throws<ArgumentNullException>("collectionSelector", () => Enumerable.Empty<StringWithIntArray>().AsQueryable().SelectMany(collectionSelector, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullSource()
        {
            IQueryable<StringWithIntArray> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany(e => e.total));
        }

        [Fact]
        public void NullSourceIndexedSelector()
        {
            IQueryable<StringWithIntArray> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany((e, i) => e.total));
        }

        [Fact]
        public void NullSourceIndexedSelectorWithResultSelector()
        {
            IQueryable<StringWithIntArray> source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullSelector()
        {
            Expression<Func<StringWithIntArray, IEnumerable<int>>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => new StringWithIntArray[0].AsQueryable().SelectMany(selector));
        }

        [Fact]
        public void NullIndexedSelector()
        {
            Expression<Func<StringWithIntArray, int, IEnumerable<int>>> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => new StringWithIntArray[0].AsQueryable().SelectMany(selector));
        }

        [Fact]
        public void IndexCausingLastToBeSelectedWithResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Robert", total=new int?[]{-10, 100} }
            };

            string[] expected = { "-10", "100" };
            Assert.Equal(expected, source.AsQueryable().SelectMany((e, i) => i == 4 ? e.total : Enumerable.Empty<int?>(), (e, f) => f.ToString()));
        }

        [Fact]
        public void SelectMany1()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SelectMany(n => new int[] { n + 4, 5 }).Count();
            Assert.Equal(6, count);
        }

        [Fact]
        public void SelectMany2()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SelectMany((n, i) => new int[] { 4 + i, 5 + n }).Count();
            Assert.Equal(6, count);
        }

        [Fact]
        public void SelectMany3()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SelectMany(n => new long[] { n + 4, 5 }, (n1, n2) => (n1 + n2).ToString()).Count();
            Assert.Equal(6, count);
        }

        [Fact]
        public void SelectMany4()
        {
            var count = (new int[] { 0, 1, 2 }).AsQueryable().SelectMany((n, i) => new long[] { 4 + i, 5 + n }, (n1, n2) => (n1 + n2).ToString()).Count();
            Assert.Equal(6, count);
        }
    }
}
