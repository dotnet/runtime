// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class OfTypeTests
    {
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_Unordered_AllValid(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in query.Select(x => (object)x).OfType<int>())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_Unordered_AllValid_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_Unordered_AllValid(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void OfType_AllValid(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.Select(x => (object)x).OfType<int>())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        public static void OfType_AllValid_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_AllValid(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_Unordered_AllValid_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.Select(x => (object)x).OfType<int>().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_Unordered_AllValid_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_Unordered_AllValid_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void OfType_AllValid_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Select(x => (object)x).OfType<int>().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        public static void OfType_AllValid_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_AllValid_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_NoneValid(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Empty(query.OfType<long>());
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_NoneValid_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_NoneValid(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_NoneValid_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Empty(query.OfType<long>().ToList());
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_NoneValid_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_NoneValid_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_Unordered_SomeValid(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(count / 2, (count + 1) / 2);
            foreach (int i in query.Select(x => x >= count / 2 ? (object)x : x.ToString()).OfType<int>())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_Unordered_SomeValid_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_Unordered_SomeValid(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void OfType_SomeValid(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = count / 2;
            foreach (int i in query.Select(x => x >= count / 2 ? (object)x : x.ToString()).OfType<int>())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        public static void OfType_SomeValid_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_SomeValid(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_Unordered_SomeValid_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(count / 2, (count + 1) / 2);
            Assert.All(query.Select(x => x >= count / 2 ? (object)x : x.ToString()).OfType<int>().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(UnorderedSources))]
        public static void OfType_Unordered_SomeValid_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_Unordered_SomeValid_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void OfType_SomeValid_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = count / 2;
            Assert.All(query.Select(x => x >= count / 2 ? (object)x : x.ToString()).OfType<int>().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        public static void OfType_SomeValid_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_SomeValid_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void OfType_SomeInvalidNull(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = count / 2;
            Assert.All(query.Select(x => x >= count / 2 ? (object)x : null).OfType<int>(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        public static void OfType_SomeInvalidNull_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_SomeInvalidNull(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void OfType_SomeValidNull(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int nullCount = 0;
            int seen = count / 2;
            Assert.All(query.Select(x => x >= count / 2 ? (object)(x.ToString()) : (object)(string)null).OfType<string>(),
                x =>
                {
                    if (string.IsNullOrEmpty(x)) nullCount++;
                    else Assert.Equal(seen++, int.Parse(x));
                });
            Assert.Equal(count, seen);
            Assert.Equal(0, nullCount);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        public static void OfType_SomeValidNull_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_SomeValidNull(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void OfType_SomeNull(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int nullCount = 0;
            int seen = count / 2;
            Assert.All(query.Select(x => x >= count / 2 ? (object)(int?)x : (int?)null).OfType<int?>(),
                x =>
                {
                    if (!x.HasValue) nullCount++;
                    else Assert.Equal(seen++, x.Value);
                });
            Assert.Equal(count, seen);
            Assert.Equal(0, nullCount);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 256 }), MemberType = typeof(Sources))]
        public static void OfType_SomeNull_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OfType_SomeNull(labeled, count);
        }

        [Fact]
        public static void OfType_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<object>)null).OfType<int>());
        }
    }
}
