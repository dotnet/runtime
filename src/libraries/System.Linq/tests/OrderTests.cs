// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Tests;
using Xunit;

namespace System.Linq.Tests
{
    public sealed class OrderTests : EnumerableTests
    {
        private class BadComparer1 : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return 1;
            }
        }

        private class BadComparer2 : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return -1;
            }
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x1 in new int[] { 1, 6, 0, -1, 3 }
                    select x1;

            Assert.Equal(q.Order().ThenBy(f => f * 2), q.Order().ThenBy(f => f * 2));
        }

        [Fact]
        public void SourceEmpty()
        {
            int[] source = { };
            Assert.Empty(source.Order());
        }

        [Fact]
        public void OrderedCount()
        {
            var source = Enumerable.Range(0, 20).Shuffle();
            Assert.Equal(20, source.Order().Count());
        }

        //FIXME: This will hang with a larger source. Do we want to deal with that case?
        [Fact]
        public void SurviveBadComparerAlwaysReturnsNegative()
        {
            int[] source = { 1 };
            int[] expected = { 1 };

            Assert.Equal(expected, source.Order(new BadComparer2()));
        }

        [Fact]
        public void KeySelectorReturnsNull()
        {
            int?[] source = { null, null, null };
            int?[] expected = { null, null, null };

            Assert.Equal(expected, source.Order());
        }

        [Fact]
        public void ElementsAllSameKey()
        {
            int?[] source = { 9, 9, 9, 9, 9, 9 };
            int?[] expected = { 9, 9, 9, 9, 9, 9 };

            Assert.Equal(expected, source.Order());
        }

        [Fact]
        public void FirstAndLastAreDuplicatesCustomComparer()
        {
            string[] source = { "Prakash", "Alpha", "dan", "DAN", "Prakash" };
            string[] expected = { "Alpha", "dan", "DAN", "Prakash", "Prakash" };

            Assert.Equal(expected, source.Order(StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void RunOnce()
        {
            string[] source = { "Prakash", "Alpha", "dan", "DAN", "Prakash" };
            string[] expected = { "Alpha", "dan", "DAN", "Prakash", "Prakash" };

            Assert.Equal(expected, source.RunOnce().Order(StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void FirstAndLastAreDuplicatesNullPassedAsComparer()
        {
            int[] source = { 5, 1, 3, 2, 5 };
            int[] expected = { 1, 2, 3, 5, 5 };

            Assert.Equal(expected, source.Order(null));
        }

        [Fact]
        public void SourceReverseOfResultNullPassedAsComparer()
        {
            int?[] source = { 100, 30, 9, 5, 0, -50, -75, null };
            int?[] expected = { null, -75, -50, 0, 5, 9, 30, 100 };

            Assert.Equal(expected, source.Order(null));
        }

        [Fact]
        public void OrderedToArray()
        {
            var source = new[]
            {
                5, 9, 6, 7, 8, 5, 20
            };
            var expected = new[]
            {
                5, 5, 6, 7, 8, 9, 20
            };

            Assert.Equal(expected, source.Order().ToArray());
        }

        [Fact]
        public void EmptyOrderedToArray()
        {
            Assert.Empty(Enumerable.Empty<int>().Order().ToArray());
        }

        [Fact]
        public void OrderedToList()
        {
            var source = new[]
            {
                5, 9, 6, 7, 8, 5, 20
            };
            var expected = new[]
            {
                5, 5, 6, 7, 8, 9, 20
            };

            Assert.Equal(expected, source.Order().ToList());
        }

        [Fact]
        public void EmptyOrderedToList()
        {
            Assert.Empty(Enumerable.Empty<int>().Order().ToList());
        }

        //FIXME: This will hang with a larger source. Do we want to deal with that case?
        [Fact]
        public void SurviveBadComparerAlwaysReturnsPositive()
        {
            int[] source = { 1 };
            int[] expected = { 1 };

            Assert.Equal(expected, source.Order(new BadComparer1()));
        }

        private class ExtremeComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                if (x == y)
                    return 0;
                if (x < y)
                    return int.MinValue;
                return int.MaxValue;
            }
        }

        [Fact]
        public void OrderExtremeComparer()
        {
            var outOfOrder = new[] { 7, 1, 0, 9, 3, 5, 4, 2, 8, 6 };
            Assert.Equal(Enumerable.Range(0, 10), outOfOrder.Order(new ExtremeComparer()));
        }

        [Fact]
        public void NullSource()
        {
            IEnumerable<int> source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Order());
        }

        [Fact]
        public void FirstOnOrdered()
        {
            Assert.Equal(0, Enumerable.Range(0, 10).Shuffle().Order().First());
            Assert.Equal(9, Enumerable.Range(0, 10).Shuffle().OrderDescending().First());
        }

        [Fact]
        public void FirstOnEmptyOrderedThrows()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Order().First());
        }

        [Fact]
        public void FirstWithPredicateOnOrdered()
        {
            IEnumerable<int> ordered = Enumerable.Range(0, 10).Shuffle().Order();
            IEnumerable<int> orderedDescending = Enumerable.Range(0, 10).Shuffle().OrderDescending();
            int counter;

            counter = 0;
            Assert.Equal(0, ordered.First(i => { counter++; return true; }));
            Assert.Equal(1, counter);

            counter = 0;
            Assert.Equal(9, ordered.First(i => { counter++; return i == 9; }));
            Assert.Equal(10, counter);

            counter = 0;
            Assert.Throws<InvalidOperationException>(() => ordered.First(i => { counter++; return false; }));
            Assert.Equal(10, counter);

            counter = 0;
            Assert.Equal(9, orderedDescending.First(i => { counter++; return true; }));
            Assert.Equal(1, counter);

            counter = 0;
            Assert.Equal(0, orderedDescending.First(i => { counter++; return i == 0; }));
            Assert.Equal(10, counter);

            counter = 0;
            Assert.Throws<InvalidOperationException>(() => orderedDescending.First(i => { counter++; return false; }));
            Assert.Equal(10, counter);
        }

        [Fact]
        public void FirstOrDefaultOnOrdered()
        {
            Assert.Equal(0, Enumerable.Range(0, 10).Shuffle().Order().FirstOrDefault());
            Assert.Equal(9, Enumerable.Range(0, 10).Shuffle().OrderDescending().FirstOrDefault());
            Assert.Equal(0, Enumerable.Empty<int>().Order().FirstOrDefault());
        }

        [Fact]
        public void FirstOrDefaultWithPredicateOnOrdered()
        {
            IEnumerable<int> Order = Enumerable.Range(0, 10).Shuffle().Order();
            IEnumerable<int> OrderDescending = Enumerable.Range(0, 10).Shuffle().OrderDescending();
            int counter;

            counter = 0;
            Assert.Equal(0, Order.FirstOrDefault(i => { counter++; return true; }));
            Assert.Equal(1, counter);

            counter = 0;
            Assert.Equal(9, Order.FirstOrDefault(i => { counter++; return i == 9; }));
            Assert.Equal(10, counter);

            counter = 0;
            Assert.Equal(0, Order.FirstOrDefault(i => { counter++; return false; }));
            Assert.Equal(10, counter);

            counter = 0;
            Assert.Equal(9, OrderDescending.FirstOrDefault(i => { counter++; return true; }));
            Assert.Equal(1, counter);

            counter = 0;
            Assert.Equal(0, OrderDescending.FirstOrDefault(i => { counter++; return i == 0; }));
            Assert.Equal(10, counter);

            counter = 0;
            Assert.Equal(0, OrderDescending.FirstOrDefault(i => { counter++; return false; }));
            Assert.Equal(10, counter);
        }

        [Fact]
        public void LastOnOrdered()
        {
            Assert.Equal(9, Enumerable.Range(0, 10).Shuffle().Order().Last());
            Assert.Equal(0, Enumerable.Range(0, 10).Shuffle().OrderDescending().Last());
        }

        [Fact]
        public void LastOnOrderedMatchingCases()
        {
            object[] boxedInts = new object[] { 0, 1, 2, 9, 1, 2, 3, 9, 4, 5, 7, 8, 9, 0, 1 };
            Assert.Same(boxedInts[12], boxedInts.Order().Last());
            Assert.Same(boxedInts[12], boxedInts.Order().LastOrDefault());
            Assert.Same(boxedInts[12], boxedInts.Order().Last(o => (int)o % 2 == 1));
            Assert.Same(boxedInts[12], boxedInts.Order().LastOrDefault(o => (int)o % 2 == 1));
        }

        [Fact]
        public void LastOnEmptyOrderedThrows()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Order().Last());
        }

        [Fact]
        public void LastOrDefaultOnOrdered()
        {
            Assert.Equal(9, Enumerable.Range(0, 10).Shuffle().Order().LastOrDefault());
            Assert.Equal(0, Enumerable.Range(0, 10).Shuffle().OrderDescending().LastOrDefault());
            Assert.Equal(0, Enumerable.Empty<int>().Order().LastOrDefault());
        }

        [Fact]
        public void EnumeratorDoesntContinue()
        {
            var enumerator = NumberRangeGuaranteedNotCollectionType(0, 3).Shuffle().Order().GetEnumerator();
            while (enumerator.MoveNext()) { }
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void SortsLargeAscendingEnumerableCorrectly()
        {
            const int Items = 1_000_000;
            IEnumerable<int> expected = NumberRangeGuaranteedNotCollectionType(0, Items);

            IEnumerable<int> unordered = expected.Select(i => i);
            IOrderedEnumerable<int> ordered = unordered.Order();

            Assert.Equal(expected, ordered);
        }

        [Fact]
        public void SortsLargeDescendingEnumerableCorrectly()
        {
            const int Items = 1_000_000;
            IEnumerable<int> expected = NumberRangeGuaranteedNotCollectionType(0, Items);

            IEnumerable<int> unordered = expected.Select(i => Items - i - 1);
            IOrderedEnumerable<int> ordered = unordered.Order();

            Assert.Equal(expected, ordered);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(1024)]
        [InlineData(4096)]
        [InlineData(1_000_000)]
        public void SortsRandomizedEnumerableCorrectly(int items)
        {
            var r = new Random(42);

            int[] randomized = Enumerable.Range(0, items).Select(i => r.Next()).ToArray();
            int[] ordered = ForceNotCollection(randomized).Order().ToArray();

            Array.Sort(randomized);
            Assert.Equal(randomized, ordered);
        }

        [Theory]
        [InlineData(new[] { 1 })]
        [InlineData(new[] { 1, 2 })]
        [InlineData(new[] { 2, 1 })]
        [InlineData(new[] { 1, 2, 3, 4, 5 })]
        [InlineData(new[] { 5, 4, 3, 2, 1 })]
        [InlineData(new[] { 4, 3, 2, 1, 5, 9, 8, 7, 6 })]
        [InlineData(new[] { 2, 4, 6, 8, 10, 5, 3, 7, 1, 9 })]
        public void TakeOne(IEnumerable<int> source)
        {
            int count = 0;
            foreach (int x in source.Order().Take(1))
            {
                count++;
                Assert.Equal(source.Min(), x);
            }
            Assert.Equal(1, count);
        }

        [Fact]
        public void CultureOrder()
        {
            string[] source = new[] { "Apple0", "Æble0", "Apple1", "Æble1", "Apple2", "Æble2" };

            CultureInfo dk = new CultureInfo("da-DK");
            CultureInfo au = new CultureInfo("en-AU");

            StringComparer comparerDk = StringComparer.Create(dk, ignoreCase: false);
            StringComparer comparerAu = StringComparer.Create(au, ignoreCase: false);

            // we don't provide a defined sorted result set because the Windows culture sorting
            // provides a different result set to the Linux culture sorting. But as we're really just
            // concerned that Order default string ordering matches current culture then this
            // should be sufficient
            string[] resultDK = source.ToArray();
            Array.Sort(resultDK, comparerDk);
            string[] resultAU = source.ToArray();
            Array.Sort(resultAU, comparerAu);

            string[] check;

            using (new ThreadCultureChange(dk))
            {
                check = source.Order().ToArray();
                Assert.Equal(resultDK, check, StringComparer.Ordinal);
            }

            using (new ThreadCultureChange(au))
            {
                check = source.Order().ToArray();
                Assert.Equal(resultAU, check, StringComparer.Ordinal);
            }

            using (new ThreadCultureChange(dk)) // "dk" whilst GetEnumerator
            {
                IEnumerator<string> s = source.Order().GetEnumerator();
                using (new ThreadCultureChange(au)) // but "au" whilst accessing...
                {
                    int idx = 0;
                    while (s.MoveNext()) // sort is done on first MoveNext, so should have "au" sorting
                    {
                        Assert.Equal(resultAU[idx++], s.Current, StringComparer.Ordinal);
                    }
                }
            }

            using (new ThreadCultureChange(au))
            {
                // "au" whilst GetEnumerator
                IEnumerator<string> s = source.Order().GetEnumerator();

                using (new ThreadCultureChange(dk))
                {
                    // but "dk" on first MoveNext
                    bool moveNext = s.MoveNext();
                    Assert.True(moveNext);

                    // ensure changing culture after MoveNext doesn't affect sort
                    using (new ThreadCultureChange(au)) // "au" whilst GetEnumerator
                    {
                        int idx = 0;
                        while (moveNext) // sort is done on first MoveNext, so should have "dk" sorting
                        {
                            Assert.Equal(resultDK[idx++], s.Current, StringComparer.Ordinal);
                            moveNext = s.MoveNext();
                        }
                    }
                }
            }
        }

        [Fact]
        public void CultureOrderElementAt()
        {
            string[] source = new[] { "Apple0", "Æble0", "Apple1", "Æble1", "Apple2", "Æble2" };

            CultureInfo dk = new CultureInfo("da-DK");
            CultureInfo au = new CultureInfo("en-AU");

            StringComparer comparerDk = StringComparer.Create(dk, ignoreCase: false);
            StringComparer comparerAu = StringComparer.Create(au, ignoreCase: false);

            // we don't provide a defined sorted result set because the Windows culture sorting
            // provides a different result set to the Linux culture sorting. But as we're really just
            // concerned that Order default string ordering matches current culture then this
            // should be sufficient
            string[] resultDK = source.ToArray();
            Array.Sort(resultDK, comparerDk);
            string[] resultAU = source.ToArray();
            Array.Sort(resultAU, comparerAu);

            IEnumerable<string> delaySortedSource = source.Order();
            for (int i = 0; i < source.Length; ++i)
            {
                using (new ThreadCultureChange(dk))
                {
                    Assert.Equal(resultDK[i], delaySortedSource.ElementAt(i), StringComparer.Ordinal);
                }

                using (new ThreadCultureChange(au))
                {
                    Assert.Equal(resultAU[i], delaySortedSource.ElementAt(i), StringComparer.Ordinal);
                }
            }
        }
    }
}
