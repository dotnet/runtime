// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class GetEnumeratorTests
    {
        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            while (enumerator.MoveNext())
            {
                int current = enumerator.Current;
                seen.Add(current);
                Assert.Equal(current, enumerator.Current);
            }
            seen.AssertComplete();

            if (labeled.ToString().StartsWith("Enumerable.Range") || labeled.ToString().StartsWith("Partitioner"))
            {
                Assert.Throws<NotSupportedException>(() => enumerator.Reset());
            }
            else
            {
                enumerator.Reset();
                seen = new IntegerRangeSet(0, count);
                while (enumerator.MoveNext())
                {
                    Assert.True(seen.Add(enumerator.Current));
                }
                seen.AssertComplete();
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1024 * 4, 1024 * 128 }, MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GetEnumerator_Unordered(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void GetEnumerator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int seen = 0;
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            while (enumerator.MoveNext())
            {
                int current = enumerator.Current;
                Assert.Equal(seen++, current);
                Assert.Equal(current, enumerator.Current);
            }
            Assert.Equal(count, seen);

            if (labeled.ToString().StartsWith("Enumerable.Range") || labeled.ToString().StartsWith("Partitioner"))
            {
                Assert.Throws<NotSupportedException>(() => enumerator.Reset());
            }
            else
            {
                enumerator.Reset();
                seen = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(seen++, enumerator.Current);
                }
                Assert.Equal(count, seen);
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 4, 1024 * 128 }, MemberType = typeof(Sources))]
        public static void GetEnumerator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GetEnumerator(labeled, count);
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_MoveNextAfterQueryOpeningFailsIsIllegal(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item.Select<int, int>(x => { throw new DeliberateTestException(); }).OrderBy(x => x);

            IEnumerator<int> enumerator = query.GetEnumerator();

            //moveNext will cause queryOpening to fail (no element generated)
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => enumerator.MoveNext());

            //moveNext after queryOpening failed
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 16 }, MemberType = typeof(UnorderedSources))]
        [MemberData(nameof(Sources.Ranges), new[] { 16 }, MemberType = typeof(Sources))]
        public static void GetEnumerator_CurrentBeforeMoveNext(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            if (labeled.ToString().StartsWith("Partitioner")
                || labeled.ToString().StartsWith("Array"))
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            }
            else
            {
                Assert.InRange(enumerator.Current, 0, count);
            }
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void GetEnumerator_MoveNextAfterEnd(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            while (enumerator.MoveNext())
            {
                count--;
            }
            Assert.Equal(0, count);
            Assert.False(enumerator.MoveNext());
        }
    }
}
