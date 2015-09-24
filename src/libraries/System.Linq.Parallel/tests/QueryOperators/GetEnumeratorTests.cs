// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class GetEnumeratorTests
    {
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            if (count == 0 && labeled.ToString().Contains("Array"))
            {
                Assert.Same(enumerator, labeled.Item.GetEnumerator());
            }
            else
            {
                Assert.NotSame(enumerator, labeled.Item.GetEnumerator());
            }
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
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 128 }), MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GetEnumerator_Unordered(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void GetEnumerator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int seen = 0;
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            if (count == 0 && labeled.ToString().Contains("Array"))
            {
                Assert.Same(enumerator, labeled.Item.GetEnumerator());
            }
            else
            {
                Assert.NotSame(enumerator, labeled.Item.GetEnumerator());
            }
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
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 128 }), MemberType = typeof(Sources))]
        public static void GetEnumerator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GetEnumerator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
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
        [MemberData("Ranges", (object)(new int[] { 16 }), MemberType = typeof(UnorderedSources))]
        [MemberData("Ranges", (object)(new int[] { 16 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
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
