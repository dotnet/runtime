// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace System.Linq.Tests
{
    public class SkipTests : EnumerableTests
    {
        [Fact]
        public void SkipSome()
        {
            Assert.Equal(Enumerable.Range(10, 10), NumberRangeGuaranteedNotCollectionType(0, 20).Skip(10));
        }

        [Fact]
        public void SkipSomeIList()
        {
            Assert.Equal(Enumerable.Range(10, 10), NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(10));
        }

        [Fact]
        public void RunOnce()
        {
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).RunOnce().Skip(10));
            Assert.Equal(Enumerable.Range(10, 10), Enumerable.Range(0, 20).ToList().RunOnce().Skip(10));
        }

        [Fact]
        public void SkipNone()
        {
            Assert.Equal(Enumerable.Range(0, 20), NumberRangeGuaranteedNotCollectionType(0, 20).Skip(0));
        }

        [Fact]
        public void SkipNoneIList()
        {
            Assert.Equal(Enumerable.Range(0, 20), NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(0));
        }

        [Fact]
        public void SkipExcessive()
        {
            Assert.Equal([], NumberRangeGuaranteedNotCollectionType(0, 20).Skip(42));
        }

        [Fact]
        public void SkipExcessiveIList()
        {
            Assert.Equal([], NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(42));
        }

        [Fact]
        public void SkipAllExactly()
        {
            Assert.False(NumberRangeGuaranteedNotCollectionType(0, 20).Skip(20).Any());
        }

        [Fact]
        public void SkipAllExactlyIList()
        {
            Assert.False(NumberRangeGuaranteedNotCollectionType(0, 20).Skip(20).ToList().Any());
        }

        [Fact]
        public void SkipThrowsOnNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Skip(3));
        }

        [Fact]
        public void SkipThrowsOnNullIList()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((List<DateTime>)null).Skip(3));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IList<DateTime>)null).Skip(3));
        }

        [Fact]
        public void SkipOnEmpty()
        {
            foreach (IEnumerable<int> source in CreateSources<int>([]))
            {
                Assert.Equal([], source.Skip(0));
                Assert.Equal([], source.Skip(-1));
                Assert.Equal([], source.Skip(1));
            }

            foreach (IEnumerable<string> source in CreateSources<string>([]))
            {
                Assert.Equal([], source.Skip(0));
                Assert.Equal([], source.Skip(-1));
                Assert.Equal([], source.Skip(1));
            }
        }

        [Fact]
        public void SkipNegative()
        {
            foreach (IEnumerable<int> source in CreateSources(Enumerable.Range(0, 20)))
            {
                Assert.Equal(Enumerable.Range(0, 20), source.Skip(-42));
            }
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            foreach (IEnumerable<int> source in CreateSources([9999, 0, 888, -1, 66, -777, 1, 2, -12345]))
            {
                IEnumerable<int> q = from x in source
                                     where x > int.MinValue
                                     select x;

                Assert.Equal(q.Skip(0), q.Skip(0));
            }
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            foreach (IEnumerable<string> source in CreateSources(["!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty]))
            {
                IEnumerable<string> q = from x in source
                                        where !string.IsNullOrEmpty(x)
                                        select x;

                Assert.Equal(q.Skip(0), q.Skip(0));
            }
        }

        [Fact]
        public void SkipOne()
        {
            int?[] expected = [100, 4, null, 10];
            foreach (IEnumerable<int?> source in CreateSources<int?>([3, 100, 4, null, 10]))
            {
                Assert.Equal(expected, source.Skip(1));
            }
        }

        [Fact]
        public void SkipAllButOne()
        {
            int?[] expected = [10];
            foreach (IEnumerable<int?> source in CreateSources<int?>([3, 100, 4, null, 10]))
            {
                Assert.Equal(expected, source.Skip(4));
            }
        }

        [Fact]
        public void SkipOneMoreThanAll()
        {
            foreach (IEnumerable<int> source in CreateSources([3, 100, 4, 10]))
            {
                Assert.Empty(source.Skip(5));
            }
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            foreach (IEnumerable<int> source in CreateSources(Enumerable.Range(0, 3)))
            {
                // Don't insist on this behaviour, but check it's correct if it happens
                IEnumerable<int> iterator = source.Skip(2);
                var en = iterator as IEnumerator<int>;
                Assert.False(en is not null && en.MoveNext());
            }
        }

        [Fact]
        public void Count()
        {
            Assert.Equal(2, NumberRangeGuaranteedNotCollectionType(0, 3).Skip(1).Count());
            Assert.Equal(2, new[] { 1, 2, 3 }.Skip(1).Count());
        }

        [Fact]
        public void FollowWithTake()
        {
            int[] expected = [6, 7];
            foreach (IEnumerable<int> source in CreateSources(Enumerable.Range(5, 4)))
            {
                Assert.Equal(expected, source.Skip(1).Take(2));
            }
        }

        [Fact]
        public void FollowWithTakeThenMassiveTake()
        {
            int[] expected = [7];
            foreach (IEnumerable<int> source in CreateSources([5, 6, 7, 8]))
            {
                Assert.Equal(expected, source.Skip(2).Take(1).Take(int.MaxValue));
            }
        }

        [Fact]
        public void FollowWithSkip()
        {
            int[] expected = [4, 5, 6];
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5, 6]))
            {
                Assert.Equal(expected, source.Skip(1).Skip(2).Skip(-4));
            }
        }

        [Fact]
        public void ElementAt()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5, 6]))
            {
                IEnumerable<int> remaining = source.Skip(2);
                Assert.Equal(3, remaining.ElementAt(0));
                Assert.Equal(4, remaining.ElementAt(1));
                Assert.Equal(6, remaining.ElementAt(3));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => remaining.ElementAt(-1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => remaining.ElementAt(4));
            }
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5, 6]))
            {
                IEnumerable<int> remaining = source.Skip(2);
                Assert.Equal(3, remaining.ElementAtOrDefault(0));
                Assert.Equal(4, remaining.ElementAtOrDefault(1));
                Assert.Equal(6, remaining.ElementAtOrDefault(3));
                Assert.Equal(0, remaining.ElementAtOrDefault(-1));
                Assert.Equal(0, remaining.ElementAtOrDefault(4));
            }
        }

        [Fact]
        public void First()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5]))
            {
                Assert.Equal(1, source.Skip(0).First());
                Assert.Equal(3, source.Skip(2).First());
                Assert.Equal(5, source.Skip(4).First());
                Assert.Throws<InvalidOperationException>(() => source.Skip(5).First());
            }
        }

        [Fact]
        public void FirstOrDefault()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5]))
            {
                Assert.Equal(1, source.Skip(0).FirstOrDefault());
                Assert.Equal(3, source.Skip(2).FirstOrDefault());
                Assert.Equal(5, source.Skip(4).FirstOrDefault());
                Assert.Equal(0, source.Skip(5).FirstOrDefault());
            }
        }

        [Fact]
        public void Last()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5]))
            {
                Assert.Equal(5, source.Skip(0).Last());
                Assert.Equal(5, source.Skip(1).Last());
                Assert.Equal(5, source.Skip(4).Last());
                Assert.Throws<InvalidOperationException>(() => source.Skip(5).Last());
            }
        }

        [Fact]
        public void LastOrDefault()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5]))
            {
                Assert.Equal(5, source.Skip(0).LastOrDefault());
                Assert.Equal(5, source.Skip(1).LastOrDefault());
                Assert.Equal(5, source.Skip(4).LastOrDefault());
                Assert.Equal(0, source.Skip(5).LastOrDefault());
            }
        }

        [Fact]
        public void ToArray()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5]))
            {
                Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Skip(0).ToArray());
                Assert.Equal(new[] { 2, 3, 4, 5 }, source.Skip(1).ToArray());
                Assert.Equal(5, source.Skip(4).ToArray().Single());
                Assert.Empty(source.Skip(5).ToArray());
                Assert.Empty(source.Skip(40).ToArray());
            }
        }

        [Fact]
        public void ToList()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5]))
            {
                Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Skip(0).ToList());
                Assert.Equal(new[] { 2, 3, 4, 5 }, source.Skip(1).ToList());
                Assert.Equal(5, source.Skip(4).ToList().Single());
                Assert.Empty(source.Skip(5).ToList());
                Assert.Empty(source.Skip(40).ToList());
            }
        }

        [Fact]
        public void RepeatEnumerating()
        {
            foreach (IEnumerable<int> source in CreateSources([1, 2, 3, 4, 5]))
            {
                IEnumerable<int> remaining = source.Skip(1);
                Assert.Equal(remaining, remaining);
            }
        }

        [Fact]
        public void LazySkipMoreThan32Bits()
        {
            IEnumerable<int> range = NumberRangeGuaranteedNotCollectionType(1, 100);
            IEnumerable<int> skipped = range.Skip(50).Skip(int.MaxValue); // Could cause an integer overflow.
            Assert.Empty(skipped);
            Assert.Equal(0, skipped.Count());
            Assert.Empty(skipped.ToArray());
            Assert.Empty(skipped.ToList());
        }

        [Fact]
        public void IteratorStateShouldNotChangeIfNumberOfElementsIsUnbounded()
        {
            // With https://github.com/dotnet/corefx/pull/13628, Skip and Take return
            // the same type of iterator. For Take, there is a limit, or upper bound,
            // on how many items can be returned from the iterator. An integer field,
            // _state, is incremented to keep track of this and to stop enumerating once
            // we pass that limit. However, for Skip, there is no such limit and the
            // iterator can contain an unlimited number of items (including past int.MaxValue).

            // This test makes sure that, in Skip, _state is not incorrectly incremented,
            // so that it does not overflow to a negative number and enumeration does not
            // stop prematurely.

            using IEnumerator<int> iterator = new FastInfiniteEnumerator<int>().Skip(1).GetEnumerator();
            iterator.MoveNext(); // Make sure the underlying enumerator has been initialized.

            FieldInfo state = iterator.GetType().GetTypeInfo()
                .GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);

            // On platforms that do not have this change, the optimization may not be present
            // and the iterator may not have a field named _state. In that case, nop.
            if (state is not null)
            {
                state.SetValue(iterator, int.MaxValue);

                for (int i = 0; i < 10; i++)
                {
                    Assert.True(iterator.MoveNext());
                }
            }
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        public void DisposeSource(int sourceCount, int count)
        {
            int state = 0;

            var source = new DelegateIterator<int>(
                moveNext: () => ++state <= sourceCount,
                current: () => 0,
                dispose: () => state = -1);

            using IEnumerator<int> iterator = source.Skip(count).GetEnumerator();
            int iteratorCount = Math.Max(0, sourceCount - Math.Max(0, count));
            Assert.All(Enumerable.Range(0, iteratorCount), _ => Assert.True(iterator.MoveNext()));

            Assert.False(iterator.MoveNext());
            Assert.Equal(-1, state);
        }
    }
}
