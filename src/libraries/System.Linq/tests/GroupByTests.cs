// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class GroupByTests
    {
        [Fact]
        public void Grouping_IList_IsReadOnly()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            foreach (IList<int> grouping in oddsEvens)
            {
                Assert.True(grouping.IsReadOnly);
            }
        }

        [Fact]
        public void Grouping_IList_NotSupported()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            foreach (IList<int> grouping in oddsEvens)
            {
                Assert.Throws<NotSupportedException>(() => grouping.Add(5));
                Assert.Throws<NotSupportedException>(() => grouping.Clear());
                Assert.Throws<NotSupportedException>(() => grouping.Insert(0, 1));
                Assert.Throws<NotSupportedException>(() => grouping.Remove(1));
                Assert.Throws<NotSupportedException>(() => grouping.RemoveAt(0));
                Assert.Throws<NotSupportedException>(() => grouping[0] = 1);
            }
        }

        [Fact]
        public void Grouping_IList_IndexerGetter()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            var e = oddsEvens.GetEnumerator();

            Assert.True(e.MoveNext());
            IList<int> odds = (IList<int>)e.Current;
            Assert.Equal(1, odds[0]);
            Assert.Equal(3, odds[1]);

            Assert.True(e.MoveNext());
            IList<int> evens = (IList<int>)e.Current;
            Assert.Equal(2, evens[0]);
            Assert.Equal(4, evens[1]);
        }

        [Fact]
        public void Grouping_ICollection_Contains()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            var e = oddsEvens.GetEnumerator();

            Assert.True(e.MoveNext());
            ICollection<int> odds = (IList<int>)e.Current;
            Assert.True(odds.Contains(1));
            Assert.True(odds.Contains(3));
            Assert.False(odds.Contains(2));
            Assert.False(odds.Contains(4));

            Assert.True(e.MoveNext());
            ICollection<int> evens = (IList<int>)e.Current;
            Assert.True(evens.Contains(2));
            Assert.True(evens.Contains(4));
            Assert.False(evens.Contains(1));
            Assert.False(evens.Contains(3));
        }

        [Fact]
        public void Grouping_IList_IndexOf()
        {
            IEnumerable<IGrouping<bool, int>> oddsEvens = new int[] { 1, 2, 3, 4 }.GroupBy(i => i % 2 == 0);
            var e = oddsEvens.GetEnumerator();

            Assert.True(e.MoveNext());
            IList<int> odds = (IList<int>)e.Current;
            Assert.Equal(0, odds.IndexOf(1));
            Assert.Equal(1, odds.IndexOf(3));
            Assert.Equal(-1, odds.IndexOf(2));
            Assert.Equal(-1, odds.IndexOf(4));

            Assert.True(e.MoveNext());
            IList<int> evens = (IList<int>)e.Current;
            Assert.Equal(0, evens.IndexOf(2));
            Assert.Equal(1, evens.IndexOf(4));
            Assert.Equal(-1, evens.IndexOf(1));
            Assert.Equal(-1, evens.IndexOf(3));
        }


    }
}
