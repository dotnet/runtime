// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ReverseTests
    {
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Reverse_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in query.Reverse())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(UnorderedSources))]
        public static void Reverse_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Reverse_Unordered(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Reverse(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = count;
            foreach (int i in query.Reverse())
            {
                Assert.Equal(--seen, i);
            }
            Assert.Equal(0, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(Sources))]
        public static void Reverse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Reverse(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Reverse_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.Reverse().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(UnorderedSources))]
        public static void Reverse_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Reverse_Unordered_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Reverse_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = count;
            Assert.All(query.Reverse().ToList(), x => Assert.Equal(--seen, x));
            Assert.Equal(0, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(Sources))]
        public static void Reverse_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Reverse_NotPipelined(labeled, count);
        }

        [Fact]
        public static void Reverse_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<object>)null).Reverse());
        }
    }
}
