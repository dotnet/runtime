// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;
using static System.Linq.Tests.SkipTakeData;

namespace System.Linq.Tests
{
    public class SkipLastTests : EnumerableTests
    {
        [Fact]
        public void SkipLastThrowsOnNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).SkipLast(10));
        }

        [Theory]
        [MemberData(nameof(EnumerableData), MemberType = typeof(SkipTakeData))]
        public void SkipLast(IEnumerable<int> source, int count)
        {
            Assert.All(IdentityTransforms<int>(), transform =>
            {
                IEnumerable<int> equivalent = transform(source);

                IEnumerable<int> expected = equivalent.Reverse().Skip(count).Reverse();
                IEnumerable<int> actual = equivalent.SkipLast(count);

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
            // We want to make sure no more than `count` items are ever evaluated ahead of the current position.
            // As an example, if Enumerable.Range(1, 6).SkipLast(2) is called, then we should read in the first 3 items,
            // yield 1, read in 4, yield 2, and so on.
            int index = 0;
            int limit = Math.Max(0, count * 2);

            var source = new DelegateIterator<int>(
                moveNext: () => index++ != limit, // Stop once we go past the limit.
                current: () => index, // Yield from 1 up to the limit, inclusive.
                dispose: () => index ^= int.MinValue);

            IEnumerator<int> iterator = source.SkipLast(count).GetEnumerator();
            Assert.Equal(0, index); // Nothing should be done before MoveNext is called.

            for (int i = 1; i <= count; i++)
            {
                Assert.True(iterator.MoveNext());
                Assert.Equal(i, iterator.Current);
                Assert.Equal(count + i, index);
            }

            Assert.False(iterator.MoveNext());
            Assert.Equal(int.MinValue, index & int.MinValue);
        }

        [Theory]
        [MemberData(nameof(EnumerableData), MemberType = typeof(SkipTakeData))]
        public void RunOnce(IEnumerable<int> source, int count)
        {
            IEnumerable<int> expected = source.SkipLast(count);
            Assert.Equal(expected, source.SkipLast(count).RunOnce());
        }

        [Fact]
        public void List_ChangesAfterSkipLast_ChangesReflectedInResults()
        {
            var list = new List<int>() { 1, 2, 3, 4, 5 };

            IEnumerable<int> e = list.SkipLast(2);

            list.RemoveAt(4);
            list.RemoveAt(3);

            Assert.Equal(new[] { 1 }, e.ToArray());
        }

        [Fact]
        public void List_Skip_ChangesAfterSkipLast_ChangesReflectedInResults()
        {
            var list = new List<int>() { 1, 2, 3, 4, 5 };

            IEnumerable<int> e = list.Skip(1).SkipLast(2);

            list.RemoveAt(4);

            Assert.Equal(new[] { 2 }, e.ToArray());
        }
    }
}
