// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }
}
