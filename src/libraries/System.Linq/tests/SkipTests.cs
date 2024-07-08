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
        private static IEnumerable<T> GuaranteeNotIList<T>(IEnumerable<T> source)
        {
            foreach (T element in source)
                yield return element;
        }

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
            Assert.Equal(Enumerable.Empty<int>(), NumberRangeGuaranteedNotCollectionType(0, 20).Skip(42));
        }

        [Fact]
        public void SkipExcessiveIList()
        {
            Assert.Equal(Enumerable.Empty<int>(), NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(42));
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
            Assert.Equal(Enumerable.Empty<int>(), GuaranteeNotIList(Enumerable.Empty<int>()).Skip(0));
            Assert.Equal(Enumerable.Empty<string>(), GuaranteeNotIList(Enumerable.Empty<string>()).Skip(-1));
            Assert.Equal(Enumerable.Empty<double>(), GuaranteeNotIList(Enumerable.Empty<double>()).Skip(1));
        }

        [Fact]
        public void SkipOnEmptyIList()
        {
            // Enumerable.Empty does return an IList, but not guaranteed as such
            // by the spec.
            Assert.Equal(Enumerable.Empty<int>(), Enumerable.Empty<int>().ToList().Skip(0));
            Assert.Equal(Enumerable.Empty<string>(), Enumerable.Empty<string>().ToList().Skip(-1));
            Assert.Equal(Enumerable.Empty<double>(), Enumerable.Empty<double>().ToList().Skip(1));
        }

        [Fact]
        public void SkipNegative()
        {
            Assert.Equal(Enumerable.Range(0, 20), NumberRangeGuaranteedNotCollectionType(0, 20).Skip(-42));
        }

        [Fact]
        public void SkipNegativeIList()
        {
            Assert.Equal(Enumerable.Range(0, 20), NumberRangeGuaranteedNotCollectionType(0, 20).ToList().Skip(-42));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = GuaranteeNotIList(from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x);

            Assert.Equal(q.Skip(0), q.Skip(0));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQueryIList()
        {
            var q = (from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x).ToList();

            Assert.Equal(q.Skip(0), q.Skip(0));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = GuaranteeNotIList(from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x);

            Assert.Equal(q.Skip(0), q.Skip(0));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQueryIList()
        {
            var q = (from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x).ToList();

            Assert.Equal(q.Skip(0), q.Skip(0));
        }

        [Fact]
        public void SkipOne()
        {
            int?[] source = { 3, 100, 4, null, 10 };
            int?[] expected = { 100, 4, null, 10 };

            Assert.Equal(expected, source.Skip(1));
        }

        [Fact]
        public void SkipOneNotIList()
        {
            int?[] source = { 3, 100, 4, null, 10 };
            int?[] expected = { 100, 4, null, 10 };

            Assert.Equal(expected, GuaranteeNotIList(source).Skip(1));
        }

        [Fact]
        public void SkipAllButOne()
        {
            int?[] source = { 3, 100, null, 4, 10 };
            int?[] expected = { 10 };

            Assert.Equal(expected, source.Skip(source.Length - 1));
        }

        [Fact]
        public void SkipAllButOneNotIList()
        {
            int?[] source = { 3, 100, null, 4, 10 };
            int?[] expected = { 10 };

            Assert.Equal(expected, GuaranteeNotIList(source.Skip(source.Length - 1)));
        }

        [Fact]
        public void SkipOneMoreThanAll()
        {
            int[] source = { 3, 100, 4, 10 };
            Assert.Empty(source.Skip(source.Length + 1));
        }

        [Fact]
        public void SkipOneMoreThanAllNotIList()
        {
            int[] source = { 3, 100, 4, 10 };
            Assert.Empty(GuaranteeNotIList(source).Skip(source.Length + 1));
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Skip(2);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en is not null && en.MoveNext());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerateIList()
        {
            var iterator = (new[] { 0, 1, 2 }).Skip(2);
            // Don't insist on this behaviour, but check it's correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en is not null && en.MoveNext());
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
            var source = new[] { 5, 6, 7, 8 };
            var expected = new[] { 6, 7 };
            Assert.Equal(expected, source.Skip(1).Take(2));
        }

        [Fact]
        public void FollowWithTakeNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(5, 4);
            var expected = new[] { 6, 7 };
            Assert.Equal(expected, source.Skip(1).Take(2));
        }

        [Fact]
        public void FollowWithTakeThenMassiveTake()
        {
            var source = new[] { 5, 6, 7, 8 };
            var expected = new[] { 7 };
            Assert.Equal(expected, source.Skip(2).Take(1).Take(int.MaxValue));
        }
        [Fact]
        public void FollowWithTakeThenMassiveTakeNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(5, 4);
            var expected = new[] { 7 };
            Assert.Equal(expected, source.Skip(2).Take(1).Take(int.MaxValue));
        }

        [Fact]
        public void FollowWithSkip()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var expected = new[] { 4, 5, 6 };
            Assert.Equal(expected, source.Skip(1).Skip(2).Skip(-4));
        }

        [Fact]
        public void FollowWithSkipNotIList()
        {
            var source = NumberRangeGuaranteedNotCollectionType(1, 6);
            var expected = new[] { 4, 5, 6 };
            Assert.Equal(expected, source.Skip(1).Skip(2).Skip(-4));
        }

        [Fact]
        public void ElementAt()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var remaining = source.Skip(2);
            Assert.Equal(3, remaining.ElementAt(0));
            Assert.Equal(4, remaining.ElementAt(1));
            Assert.Equal(6, remaining.ElementAt(3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => remaining.ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => remaining.ElementAt(4));
        }

        [Fact]
        public void ElementAtNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5, 6 });
            var remaining = source.Skip(2);
            Assert.Equal(3, remaining.ElementAt(0));
            Assert.Equal(4, remaining.ElementAt(1));
            Assert.Equal(6, remaining.ElementAt(3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => remaining.ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => remaining.ElementAt(4));
        }

        [Fact]
        public void ElementAtOrDefault()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };
            var remaining = source.Skip(2);
            Assert.Equal(3, remaining.ElementAtOrDefault(0));
            Assert.Equal(4, remaining.ElementAtOrDefault(1));
            Assert.Equal(6, remaining.ElementAtOrDefault(3));
            Assert.Equal(0, remaining.ElementAtOrDefault(-1));
            Assert.Equal(0, remaining.ElementAtOrDefault(4));
        }

        [Fact]
        public void ElementAtOrDefaultNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5, 6 });
            var remaining = source.Skip(2);
            Assert.Equal(3, remaining.ElementAtOrDefault(0));
            Assert.Equal(4, remaining.ElementAtOrDefault(1));
            Assert.Equal(6, remaining.ElementAtOrDefault(3));
            Assert.Equal(0, remaining.ElementAtOrDefault(-1));
            Assert.Equal(0, remaining.ElementAtOrDefault(4));
        }

        [Fact]
        public void First()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(1, source.Skip(0).First());
            Assert.Equal(3, source.Skip(2).First());
            Assert.Equal(5, source.Skip(4).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).First());
        }

        [Fact]
        public void FirstNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Skip(0).First());
            Assert.Equal(3, source.Skip(2).First());
            Assert.Equal(5, source.Skip(4).First());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).First());
        }

        [Fact]
        public void FirstOrDefault()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(1, source.Skip(0).FirstOrDefault());
            Assert.Equal(3, source.Skip(2).FirstOrDefault());
            Assert.Equal(5, source.Skip(4).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).FirstOrDefault());
        }

        [Fact]
        public void FirstOrDefaultNotIList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(1, source.Skip(0).FirstOrDefault());
            Assert.Equal(3, source.Skip(2).FirstOrDefault());
            Assert.Equal(5, source.Skip(4).FirstOrDefault());
            Assert.Equal(0, source.Skip(5).FirstOrDefault());
        }

        [Fact]
        public void Last()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(5, source.Skip(0).Last());
            Assert.Equal(5, source.Skip(1).Last());
            Assert.Equal(5, source.Skip(4).Last());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Last());
        }

        [Fact]
        public void LastNotList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(5, source.Skip(0).Last());
            Assert.Equal(5, source.Skip(1).Last());
            Assert.Equal(5, source.Skip(4).Last());
            Assert.Throws<InvalidOperationException>(() => source.Skip(5).Last());
        }

        [Fact]
        public void LastOrDefault()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(5, source.Skip(0).LastOrDefault());
            Assert.Equal(5, source.Skip(1).LastOrDefault());
            Assert.Equal(5, source.Skip(4).LastOrDefault());
            Assert.Equal(0, source.Skip(5).LastOrDefault());
        }

        [Fact]
        public void LastOrDefaultNotList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(5, source.Skip(0).LastOrDefault());
            Assert.Equal(5, source.Skip(1).LastOrDefault());
            Assert.Equal(5, source.Skip(4).LastOrDefault());
            Assert.Equal(0, source.Skip(5).LastOrDefault());
        }

        [Fact]
        public void ToArray()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Skip(0).ToArray());
            Assert.Equal(new[] { 2, 3, 4, 5 }, source.Skip(1).ToArray());
            Assert.Equal(5, source.Skip(4).ToArray().Single());
            Assert.Empty(source.Skip(5).ToArray());
            Assert.Empty(source.Skip(40).ToArray());
        }

        [Fact]
        public void ToArrayNotList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Skip(0).ToArray());
            Assert.Equal(new[] { 2, 3, 4, 5 }, source.Skip(1).ToArray());
            Assert.Equal(5, source.Skip(4).ToArray().Single());
            Assert.Empty(source.Skip(5).ToArray());
            Assert.Empty(source.Skip(40).ToArray());
        }

        [Fact]
        public void ToList()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Skip(0).ToList());
            Assert.Equal(new[] { 2, 3, 4, 5 }, source.Skip(1).ToList());
            Assert.Equal(5, source.Skip(4).ToList().Single());
            Assert.Empty(source.Skip(5).ToList());
            Assert.Empty(source.Skip(40).ToList());
        }

        [Fact]
        public void ToListNotList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, source.Skip(0).ToList());
            Assert.Equal(new[] { 2, 3, 4, 5 }, source.Skip(1).ToList());
            Assert.Equal(5, source.Skip(4).ToList().Single());
            Assert.Empty(source.Skip(5).ToList());
            Assert.Empty(source.Skip(40).ToList());
        }

        [Fact]
        public void RepeatEnumerating()
        {
            var source = new[] { 1, 2, 3, 4, 5 };
            var remaining = source.Skip(1);
            Assert.Equal(remaining, remaining);
        }

        [Fact]
        public void RepeatEnumeratingNotList()
        {
            var source = GuaranteeNotIList(new[] { 1, 2, 3, 4, 5 });
            var remaining = source.Skip(1);
            Assert.Equal(remaining, remaining);
        }

        [Fact]
        public void LazySkipMoreThan32Bits()
        {
            var range = NumberRangeGuaranteedNotCollectionType(1, 100);
            var skipped = range.Skip(50).Skip(int.MaxValue); // Could cause an integer overflow.
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

            var iterator = new FastInfiniteEnumerator<int>().Skip(1).GetEnumerator();
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

            IEnumerator<int> iterator = source.Skip(count).GetEnumerator();
            int iteratorCount = Math.Max(0, sourceCount - Math.Max(0, count));
            Assert.All(Enumerable.Range(0, iteratorCount), _ => Assert.True(iterator.MoveNext()));

            Assert.False(iterator.MoveNext());
            Assert.Equal(-1, state);
        }
    }
}
