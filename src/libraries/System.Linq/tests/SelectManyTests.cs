// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SelectManyTests : EnumerableTests
    {
        [Fact]
        public void EmptySource()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany(e => e.total));
        }

        [Fact]
        public void EmptySourceIndexedSelector()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany((e, i) => e.total));
        }

        [Fact]
        public void EmptySourceResultSelector()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void EmptySourceResultSelectorIndexedSelector()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void SingleElement()
        {
            int?[] expected = { 90, 55, null, 43, 89 };
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name = "Prakash", total = expected }
            };
            Assert.Equal(expected, source.SelectMany(e => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmpty()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany(e => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexedSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany((e, i) => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyWithResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexedSelectorWithResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };

            Assert.Empty(source.SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

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
            Assert.Equal(expected, source.SelectMany(e => e.total));
        }

        [Fact]
        public void SourceEmptyIndexUsed()
        {
            Assert.Empty(Enumerable.Empty<StringWithIntArray>().SelectMany((e, index) => e.total));
        }

        [Fact]
        public void SingleElementIndexUsed()
        {
            int?[] expected = { 90, 55, null, 43, 89 };
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name = "Prakash", total = expected }
            };
            Assert.Equal(expected, source.SelectMany((e, index) => e.total));
        }

        [Fact]
        public void NonEmptySelectingEmptyIndexUsed()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total= new int?[0] },
                new StringWithIntArray { name="Bob", total=new int?[0] },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[0] },
                new StringWithIntArray { name="Prakash", total=new int?[0] }
            };
            Assert.Empty(source.SelectMany((e, index) => e.total));
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
            Assert.Equal(expected, source.SelectMany((e, index) => e.total));
        }

        [Fact]
        public void IndexCausingFirstToBeSelected()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };

            Assert.Equal(source.First().total, source.SelectMany((e, i) => i == 0 ? e.total : Enumerable.Empty<int?>()));
        }

        [Fact]
        public void IndexCausingLastToBeSelected()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Robert", total=new int?[]{-10, 100} }
            };

            Assert.Equal(source.Last().total, source.SelectMany((e, i) => i == 4 ? e.total : Enumerable.Empty<int?>()));
        }

        [Fact]
        [OuterLoop]
        public void IndexOverflow()
        {
            var selected = new FastInfiniteEnumerator<int>().SelectMany((e, i) => Enumerable.Empty<int>());
            using (var en = selected.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while(en.MoveNext())
                    {
                    }
                });
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

            Assert.Equal(expected, source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullResultSelector()
        {
            Func<StringWithIntArray, int?, string> resultSelector = null;
            Assert.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Empty<StringWithIntArray>().SelectMany(e => e.total, resultSelector));
        }

        [Fact]
        public void NullResultSelectorIndexedSelector()
        {
            Func<StringWithIntArray, int?, string> resultSelector = null;
            Assert.Throws<ArgumentNullException>("resultSelector", () => Enumerable.Empty<StringWithIntArray>().SelectMany((e, i) => e.total, resultSelector));
        }

        [Fact]
        public void NullSourceWithResultSelector()
        {
            StringWithIntArray[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany(e => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullCollectionSelector()
        {
            Func<StringWithIntArray, IEnumerable<int?>> collectionSelector = null;
            Assert.Throws<ArgumentNullException>("collectionSelector", () => Enumerable.Empty<StringWithIntArray>().SelectMany(collectionSelector, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullIndexedCollectionSelector()
        {
            Func<StringWithIntArray, int, IEnumerable<int?>> collectionSelector = null;
            Assert.Throws<ArgumentNullException>("collectionSelector", () => Enumerable.Empty<StringWithIntArray>().SelectMany(collectionSelector, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullSource()
        {
            StringWithIntArray[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany(e => e.total));
        }

        [Fact]
        public void NullSourceIndexedSelector()
        {
            StringWithIntArray[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany((e, i) => e.total));
        }

        [Fact]
        public void NullSourceIndexedSelectorWithResultSelector()
        {
            StringWithIntArray[] source = null;
            Assert.Throws<ArgumentNullException>("source", () => source.SelectMany((e, i) => e.total, (e, f) => f.ToString()));
        }

        [Fact]
        public void NullSelector()
        {
            Func<StringWithIntArray, int[]> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => new StringWithIntArray[0].SelectMany(selector));
        }

        [Fact]
        public void NullIndexedSelector()
        {
            Func<StringWithIntArray, int, int[]> selector = null;
            Assert.Throws<ArgumentNullException>("selector", () => new StringWithIntArray[0].SelectMany(selector));
        }

        [Fact]
        public void IndexCausingFirstToBeSelectedWithResultSelector()
        {
            StringWithIntArray[] source =
            {
                new StringWithIntArray { name="Prakash", total=new int?[]{1, 2, 3, 4} },
                new StringWithIntArray { name="Bob", total=new int?[]{5, 6} },
                new StringWithIntArray { name="Chris", total=new int?[0] },
                new StringWithIntArray { name=null, total=new int?[]{8, 9} },
                new StringWithIntArray { name="Prakash", total=new int?[]{-10, 100} }
            };
            string[] expected = { "1", "2", "3", "4" };
            Assert.Equal(expected, source.SelectMany((e, i) => i == 0 ? e.total : Enumerable.Empty<int?>(), (e, f) => f.ToString()));
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
            Assert.Equal(expected, source.SelectMany((e, i) => i == 4 ? e.total : Enumerable.Empty<int?>(), (e, f) => f.ToString()));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SelectMany(i => new int[0]);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIndexed()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SelectMany((e, i) => new int[0]);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateResultSel()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SelectMany(i => new int[0], (e, i) => e);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIndexedResultSel()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).SelectMany((e, i) => new int[0], (e, i) => e);
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
