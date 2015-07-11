// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Test
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
                seen.Add(enumerator.Current);
            }
            seen.AssertComplete();
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
                Assert.Equal(seen++, enumerator.Current);
            }
            Assert.Equal(count, seen);
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
            AggregateException ae = Assert.Throws<AggregateException>(() => enumerator.MoveNext());
            Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));

            //moveNext after queryOpening failed
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }
    }
}
