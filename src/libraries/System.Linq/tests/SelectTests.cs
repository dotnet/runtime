// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Linq.Tests
{
    public class SelectTests
    {
        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q1 = from x1 in new string[] { "Alen", "Felix", null, null, "X", "Have Space", "Clinton", "" }
                     select x1; ;

            var q2 = from x2 in new int[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                     select x2;

            var q = from x3 in q1
                    from x4 in q2
                    select new { a1 = x3, a2 = x4 };

            Assert.Equal(q.Select(e => e.a1), q.Select(e => e.a1));
        }

        [Fact]
        public void SingleElement()
        {
            var source = new[]
            {
                new  { name = "Prakash", custID = 98088 }
            };
            string[] expected = { "Prakash" };

            Assert.Equal(expected, source.Select(e => e.name));
        }

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
            Assert.Equal(expected, source.Select(e => e.name));
        }

        [Fact]
        public void EmptyWithIndexedSelector()
        {
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Empty<string>().Select((s, i) => s.Length + i));
        }

        [Fact]
        public void SingleElementIndexedSelector()
        {
            var source = new[]
            {
                new  { name = "Prakash", custID = 98088 }
            };
            string[] expected = { "Prakash" };

            Assert.Equal(expected, source.Select((e, index) => e.name));
        }

        [Fact]
        public void SelectPropertyPassingIndex()
        {
            var source = new[]{
                new { name="Prakash", custID=98088 },
                new { name="Bob", custID=29099 },
                new { name="Chris", custID=39033 },
                new { name=(string)null, custID=30349 },
                new { name="Prakash", custID=39030 }
            };
            string[] expected = { "Prakash", "Bob", "Chris", null, "Prakash" };
            Assert.Equal(expected, source.Select((e, i) => e.name));
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
            Assert.Equal(expected, source.Select((e, i) => i == 0 ? e.name : null));
        }

        [Fact]
        public void SelectPropertyPassingIndexOnLast()
        {
            var source = new[]{
                new { name="Prakash", custID=98088},
                new { name="Bob", custID=29099 },
                new { name="Chris", custID=39033 },
                new { name="Robert", custID=39033 },
                new { name="Allen", custID=39033 },
                new { name="Chuck", custID=39033 }
            };
            string[] expected = { null, null, null, null, null, "Chuck" };
            Assert.Equal(expected, source.Select((e, i) => i == 5 ? e.name : null));
        }

        private sealed class FastInfiniteEnumerator : IEnumerable<int>, IEnumerator<int>
        {
            public IEnumerator<int> GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
            public bool MoveNext()
            {
                return true;
            }
            public void Reset()
            {
            }
            object IEnumerator.Current
            {
                get { return 0; }
            }
            public void Dispose()
            {
            }
            public int Current
            {
                get { return 0; }
            }
        }


        [Fact]
        [OuterLoop]
        public void Overflow()
        {
            var selected = new FastInfiniteEnumerator().Select((e, i) => e);
            using (var en = selected.GetEnumerator())
                Assert.Throws<OverflowException>(() =>
                {
                    while (en.MoveNext())
                    {

                    }
                });
        }

        [Fact]
        public void Select_SourceIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> source = null;
            Func<int, int> selector = i => i + 1;

            Assert.Throws<ArgumentNullException>("source", () => source.Select(selector));
        }

        [Fact]
        public void Select_SelectorIsNull_ArgumentNullExceptionThrown_Indexed()
        {
            IEnumerable<int> source = Enumerable.Range(1, 10);
            Func<int, int, int> selector = null;

            Assert.Throws<ArgumentNullException>("selector", () => source.Select(selector));
        }

        [Fact]
        public void Select_SourceIsNull_ArgumentNullExceptionThrown_Indexed()
        {
            IEnumerable<int> source = null;
            Func<int, int, int> selector = (e, i) => i + 1;

            Assert.Throws<ArgumentNullException>("source", () => source.Select(selector));
        }

        [Fact]
        public void Select_SelectorIsNull_ArgumentNullExceptionThrown()
        {
            IEnumerable<int> source = Enumerable.Range(1, 10);
            Func<int, int> selector = null;

            Assert.Throws<ArgumentNullException>("selector", () => source.Select(selector));
        }
        [Fact]
        public void Select_SourceIsAnArray_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            Func<int>[] source = new Func<int>[] { () => { funcCalled = true; return 1; } };

            IEnumerable<int> query = source.Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Select_SourceIsAList_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            List<Func<int>> source = new List<Func<int>>() { () => { funcCalled = true; return 1; } };

            IEnumerable<int> query = source.Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Select_SourceIsIReadOnlyCollection_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            IReadOnlyCollection<Func<int>> source = new ReadOnlyCollection<Func<int>>(new List<Func<int>>() { () => { funcCalled = true; return 1; } });

            IEnumerable<int> query = source.Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Select_SourceIsICollection_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            ICollection<Func<int>> source = new LinkedList<Func<int>>(new List<Func<int>>() { () => { funcCalled = true; return 1; } });

            IEnumerable<int> query = source.Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Select_SourceIsIEnumerable_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            IEnumerable<Func<int>> source = Enumerable.Repeat((Func<int>)(() => { funcCalled = true; return 1; }), 1);

            IEnumerable<int> query = source.Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void SelectSelect_SourceIsAnArray_ExecutionIsDefered()
        {
            bool funcCalled = false;
            Func<int>[] source = new Func<int>[] { () => { funcCalled = true; return 1; } };

            IEnumerable<int> query = source.Select(d => d).Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void SelectSelect_SourceIsAList_ExecutionIsDefered()
        {
            bool funcCalled = false;
            List<Func<int>> source = new List<Func<int>>() { () => { funcCalled = true; return 1; } };

            IEnumerable<int> query = source.Select(d => d).Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void SelectSelect_SourceIsIReadOnlyCollection_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            IReadOnlyCollection<Func<int>> source = new ReadOnlyCollection<Func<int>>(new List<Func<int>>() { () => { funcCalled = true; return 1; } });

            IEnumerable<int> query = source.Select(d => d).Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void SelectSelect_SourceIsICollection_ExecutionIsDeferred()
        {
            bool funcCalled = false;
            ICollection<Func<int>> source = new LinkedList<Func<int>>(new List<Func<int>>() { () => { funcCalled = true; return 1; } });

            IEnumerable<int> query = source.Select(d => d).Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void SelectSelect_SourceIsIEnumerable_ExecutionIsDefered()
        {
            bool funcCalled = false;
            IEnumerable<Func<int>> source = Enumerable.Repeat((Func<int>)(() => { funcCalled = true; return 1; }), 1);

            IEnumerable<int> query = source.Select(d => d).Select(d => d());
            Assert.False(funcCalled);
        }

        [Fact]
        public void Select_SourceIsAnArray_ReturnsExpectedValues()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                var expected = selector(source[index]);
                Assert.Equal(expected, item);
                index++;
            }

            Assert.Equal(source.Length, index);
        }

        [Fact]
        public void Select_SourceIsAList_ReturnsExpectedValues()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                var expected = selector(source[index]);
                Assert.Equal(expected, item);
                index++;
            }

            Assert.Equal(source.Count, index);
        }

        [Fact]
        public void Select_SourceIsIReadOnlyCollection_ReturnsExpectedValues()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                index++;
                var expected = selector(index);
                Assert.Equal(expected, item);
            }

            Assert.Equal(source.Count, index);
        }

        [Fact]
        public void Select_SourceIsICollection_ReturnsExpectedValues()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                index++;
                var expected = selector(index);
                Assert.Equal(expected, item);
            }

            Assert.Equal(source.Count, index);
        }

        [Fact]
        public void Select_SourceIsIEnumerable_ReturnsExpectedValues()
        {
            int nbOfItems = 5;
            IEnumerable<int> source = Enumerable.Range(1, nbOfItems);
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                index++;
                var expected = selector(index);
                Assert.Equal(expected, item);
            }

            Assert.Equal(nbOfItems, index);
        }

        [Fact]
        public void Select_SourceIsAnArray_CurrentIsDefaultOfTAfterEnumeration()
        {
            int[] source = new[] { 1 };
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            var enumerator = query.GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void Select_SourceIsAList_CurrentIsDefaultOfTAfterEnumeration()
        {
            List<int> source = new List<int>() { 1 };
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            var enumerator = query.GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void Select_SourceIsIReadOnlyCollection_CurrentIsDefaultOfTAfterEnumeration()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int>() { 1 });
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            var enumerator = query.GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void Select_SourceIsICollection_CurrentIsDefaultOfTAfterEnumeration()
        {
            ICollection<int> source = new LinkedList<int>(new List<int>() { 1 });
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            var enumerator = query.GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void Select_SourceIsIEnumerable_CurrentIsDefaultOfTAfterEnumeration()
        {
            IEnumerable<int> source = Enumerable.Repeat(1, 1);
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector);

            var enumerator = query.GetEnumerator();
            while (enumerator.MoveNext()) ;

            Assert.Equal(default(int), enumerator.Current);
        }

        [Fact]
        public void SelectSelect_SourceIsAnArray_ReturnsExpectedValues()
        {
            Func<int, int> selector = i => i + 1;
            int[] source = new[] { 1, 2, 3, 4, 5 };

            IEnumerable<int> query = source.Select(selector).Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                var expected = selector(selector(source[index]));
                Assert.Equal(expected, item);
                index++;
            }

            Assert.Equal(source.Length, index);
        }

        [Fact]
        public void SelectSelect_SourceIsAList_ReturnsExpectedValues()
        {
            List<int> source = new List<int> { 1, 2, 3, 4, 5 };
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector).Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                var expected = selector(selector(source[index]));
                Assert.Equal(expected, item);
                index++;
            }

            Assert.Equal(source.Count, index);
        }

        [Fact]
        public void SelectSelect_SourceIsIReadOnlyCollection_ReturnsExpectedValues()
        {
            IReadOnlyCollection<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector).Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                index++;
                var expected = selector(selector(index));
                Assert.Equal(expected, item);
            }

            Assert.Equal(source.Count, index);
        }

        [Fact]
        public void SelectSelect_SourceIsICollection_ReturnsExpectedValues()
        {
            ICollection<int> source = new LinkedList<int>(new List<int> { 1, 2, 3, 4, 5 });
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector).Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                index++;
                var expected = selector(selector(index));
                Assert.Equal(expected, item);
            }

            Assert.Equal(source.Count, index);
        }

        [Fact]
        public void SelectSelect_SourceIsIEnumerable_ReturnsExpectedValues()
        {
            int nbOfItems = 5;
            IEnumerable<int> source = Enumerable.Range(1, 5);
            Func<int, int> selector = i => i + 1;

            IEnumerable<int> query = source.Select(selector).Select(selector);

            int index = 0;
            foreach (var item in query)
            {
                index++;
                var expected = selector(selector(index));
                Assert.Equal(expected, item);
            }

            Assert.Equal(nbOfItems, index);
        }

        [Fact]
        public void Select_SourceIsEmptyEnumerable_ReturnedCollectionHasNoElements()
        {
            IEnumerable<int> source = Enumerable.Empty<int>();
            bool wasSelectorCalled = false;

            IEnumerable<int> result = source.Select(i => { wasSelectorCalled = true; return i + 1; });

            bool hadItems = false;
            foreach (var item in result)
            {
                hadItems = true;
            }

            Assert.False(hadItems);
            Assert.False(wasSelectorCalled);
        }

        [Fact]
        public void Select_ExceptionThrownFromSelector_ExceptionPropagatedToTheCaller()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, int> selector = i => { throw new InvalidOperationException(); };

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void Select_ExceptionThrownFromSelector_IteratorCanBeUsedAfterExceptionIsCaught()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, int> selector = i =>
            {
                if (i == 1)
                    throw new InvalidOperationException();
                return i + 1;
            };

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            enumerator.MoveNext();
            Assert.Equal(3 /* 2 + 1 */, enumerator.Current);
        }

        /// <summary>
        /// Test enumerator - returns int values from 1 to 5 included.
        /// </summary>
        private class TestEnumerator : IEnumerable<int>, IEnumerator<int>
        {
            private int _current = 0;

            public virtual int Current { get { return _current; } }

            object IEnumerator.Current { get { return Current; } }

            public void Dispose() { }

            public virtual IEnumerator<int> GetEnumerator()
            {
                return this;
            }

            public virtual bool MoveNext()
            {
                return _current++ < 5;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Fact]
        public void Select_ExceptionThrownFromCurrentOfSourceIterator_ExceptionPropagatedToTheCaller()
        {
            IEnumerable<int> source = new ThrowsOnCurrent();
            Func<int, int> selector = i => i + 1;

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void Select_ExceptionThrownFromCurrentOfSourceIterator_IteratorCanBeUsedAfterExceptionIsCaught()
        {
            IEnumerable<int> source = new ThrowsOnCurrent();
            Func<int, int> selector = i => i + 1;

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            enumerator.MoveNext();
            Assert.Equal(3 /* 2 + 1 */, enumerator.Current);
        }

        /// <summary>
        /// Test enumerator - throws InvalidOperationException from Current after MoveNext called once.
        /// </summary>
        private class ThrowsOnCurrent : TestEnumerator
        {
            public override int Current
            {
                get
                {
                    var current = base.Current;
                    if (current == 1)
                        throw new InvalidOperationException();
                    return current;
                }
            }
        }

        [Fact]
        public void Select_ExceptionThrownFromMoveNextOfSourceIterator_ExceptionPropagatedToTheCaller()
        {
            IEnumerable<int> source = new ThrowsOnMoveNext();
            Func<int, int> selector = i => i + 1;

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void Select_ExceptionThrownFromMoveNextOfSourceIterator_IteratorCanBeUsedAfterExceptionIsCaught()
        {
            IEnumerable<int> source = new ThrowsOnMoveNext();
            Func<int, int> selector = i => i + 1;

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            enumerator.MoveNext();
            Assert.Equal(3 /* 2 + 1 */, enumerator.Current);
        }

        /// <summary>
        /// Test enumerator - throws InvalidOperationException on first call to MoveNext.
        /// </summary>
        private class ThrowsOnMoveNext : TestEnumerator
        {
            public override bool MoveNext()
            {
                bool baseReturn = base.MoveNext();
                if (base.Current == 1)
                    throw new InvalidOperationException();

                return baseReturn;
            }
        }

        [Fact]
        public void Select_ExceptionThrownFromGetEnumeratorOnSource_ExceptionPropagatedToTheCaller()
        {
            IEnumerable<int> source = new ThrowsOnGetEnumerator();
            Func<int, int> selector = i => i + 1;

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void Select_ExceptionThrownFromGetEnumeratorOnSource_CurrentIsSetToDefaultOfItemTypeAndIteratorCanBeUsedAfterExceptionIsCaught()
        {
            IEnumerable<int> source = new ThrowsOnGetEnumerator();
            Func<int, string> selector = i => i.ToString();

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            string currentValue = enumerator.Current;
            Assert.Equal(default(string), currentValue);

            Assert.True(enumerator.MoveNext());
            Assert.Equal("1", enumerator.Current);
        }

        /// <summary>
        /// Test enumerator - throws InvalidOperationException from GetEnumerator when called for the first time.
        /// </summary>
        private class ThrowsOnGetEnumerator : TestEnumerator
        {
            private int getEnumeratorCallCount;
            public override IEnumerator<int> GetEnumerator()
            {
                if (getEnumeratorCallCount++ == 0)
                    throw new InvalidOperationException();

                return base.GetEnumerator();
            }
        }

        [Fact]
        public void Select_SourceListGetsModifiedDuringIteration_ExceptionIsPropagated()
        {
            List<int> source = new List<int>() { 1, 2, 3, 4, 5 };
            Func<int, int> selector = i => i + 1;

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(2 /* 1 + 1 */, enumerator.Current);

            source.Add(6);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void Select_GetEnumeratorCalledTwice_DifferentInstancesReturned()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            var query = source.Select(i => i + 1);

            var enumerator1 = query.GetEnumerator();
            var enumerator2 = query.GetEnumerator();

            Assert.Same(query, enumerator1);
            Assert.NotSame(enumerator1, enumerator2);

            enumerator1.Dispose();
            enumerator2.Dispose();
        }

        [Fact]
        public void Select_ResetCalledOnEnumerator_ExceptionThrown()
        {
            int[] source = new[] { 1, 2, 3, 4, 5 };
            Func<int, int> selector = i => i + 1;

            var result = source.Select(selector);
            var enumerator = result.GetEnumerator();

            Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }
    }
}
