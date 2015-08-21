// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class WhereTests
    {
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Where_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, (count + 1) / 2);
            foreach (int i in query.Where(x => x % 2 == 0))
            {
                Assert.Equal(0, i % 2);
                seen.Add(i / 2);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(UnorderedSources))]
        public static void Where_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_Unordered(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Where(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = -2;
            foreach (int i in query.Where(x => x % 2 == 0))
            {
                Assert.Equal(seen += 2, i);
            }
            Assert.Equal(count - (count - 1) % 2 - 1, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(Sources))]
        public static void Where_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Where_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, (count + 1) / 2);
            Assert.All(query.Where(x => x % 2 == 0).ToList(), x => seen.Add(x / 2));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(UnorderedSources))]
        public static void Where_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_Unordered_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Where_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = -2;
            Assert.All(query.Where(x => x % 2 == 0).ToList(), x => Assert.Equal(seen += 2, x));
            Assert.Equal(count - (count - 1) % 2 - 1, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(Sources))]
        public static void Where_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_NotPipelined(labeled, count);
        }

        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Where_Indexed_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in query.Where((x, index) => x == index))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(UnorderedSources))]
        public static void Where_Indexed_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_Indexed_Unordered(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Where_Indexed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.Where((x, index) => x == index))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(Sources))]
        public static void Where_Indexed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_Indexed(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Where_Indexed_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.Where((x, index) => x == index).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(UnorderedSources))]
        public static void Where_Indexed_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_Indexed_Unordered_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Where_Indexed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Where((x, index) => x == index).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 512 }), MemberType = typeof(Sources))]
        public static void Where_Indexed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Where_Indexed_NotPipelined(labeled, count);
        }

        [Fact]
        public static void Where_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).Where(x => x));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).Where((x, index) => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().Where((Func<bool, bool>)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().Where((Func<bool, int, bool>)null));
        }
    }
}
