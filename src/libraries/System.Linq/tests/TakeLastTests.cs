// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;
using static System.Linq.Tests.SkipTakeData;

namespace System.Linq.Tests
{
    public class TakeLastTests : EnumerableTests
    {
        [Fact]
        public void SkipLastThrowsOnNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).TakeLast(10));
        }

        [Theory]
        [MemberData(nameof(EnumerableData), MemberType = typeof(SkipTakeData))]
        public void TakeLast(IEnumerable<int> source, int count)
        {
            Assert.All(IdentityTransforms<int>(), transform =>
            {
                IEnumerable<int> equivalent = transform(source);

                IEnumerable<int> expected = equivalent.Reverse().Take(count).Reverse();
                IEnumerable<int> actual = equivalent.TakeLast(count);

                Assert.Equal(expected, actual);
                Assert.Equal(expected.Count(), actual.Count());
                Assert.Equal(expected, actual.ToArray());
                Assert.Equal(expected, actual.ToList());

                Assert.Equal(expected.FirstOrDefault(), actual.FirstOrDefault());
                Assert.Equal(expected.LastOrDefault(), actual.LastOrDefault());

                Assert.All(Enumerable.Range(0, expected.Count()), index =>
                {
                    Assert.Equal(expected.ElementAt(index), actual.ElementAt(index));
                });

                Assert.Equal(0, actual.ElementAtOrDefault(-1));
                Assert.Equal(0, actual.ElementAtOrDefault(actual.Count()));
            });
        }

        [Theory]
        [MemberData(nameof(EvaluationBehaviorData), MemberType = typeof(SkipTakeData))]
        public void EvaluationBehavior(int count)
        {
            int index = 0;
            int limit = count * 2;

            var source = new DelegateIterator<int>(
                moveNext: () => index++ != limit, // Stop once we go past the limit.
                current: () => index, // Yield from 1 up to the limit, inclusive.
                dispose: () => index ^= int.MinValue);

            IEnumerator<int> iterator = source.TakeLast(count).GetEnumerator();
            Assert.Equal(0, index); // Nothing should be done before MoveNext is called.

            for (int i = 1; i <= count; i++)
            {
                Assert.True(iterator.MoveNext());
                Assert.Equal(count + i, iterator.Current);

                // After the first MoveNext call to the enumerator, everything should be evaluated and the enumerator
                // should be disposed.
                Assert.Equal(int.MinValue, index & int.MinValue);
                Assert.Equal(limit + 1, index & int.MaxValue);
            }

            Assert.False(iterator.MoveNext());

            // Unlike SkipLast, TakeLast can tell straightaway that it can return a sequence with no elements if count <= 0.
            // The enumerable it returns is a specialized empty iterator that has no connections to the source. Hence,
            // after MoveNext returns false under those circumstances, it won't invoke Dispose on our enumerator.
            int expected = count <= 0 ? 0 : int.MinValue;
            Assert.Equal(expected, index & int.MinValue);
        }

        [Theory]
        [MemberData(nameof(EnumerableData), MemberType = typeof(SkipTakeData))]
        public void RunOnce(IEnumerable<int> source, int count)
        {
            IEnumerable<int> expected = source.TakeLast(count);
            Assert.Equal(expected, source.TakeLast(count).RunOnce());
        }

        [Fact]
        public void List_ChangesAfterTakeLast_ChangesReflectedInResults()
        {
            var list = new List<int>() { 1, 2, 3, 4, 5 };

            IEnumerable<int> e = list.TakeLast(3);

            list.RemoveAt(0);
            list.RemoveAt(0);

            Assert.Equal(new[] { 3, 4, 5 }, e.ToArray());
        }

        [Fact]
        public void List_Skip_ChangesAfterTakeLast_ChangesReflectedInResults()
        {
            var list = new List<int>() { 1, 2, 3, 4, 5 };

            IEnumerable<int> e = list.Skip(1).TakeLast(3);

            list.RemoveAt(0);

            Assert.Equal(new[] { 3, 4, 5 }, e.ToArray());
        }
    }
}
