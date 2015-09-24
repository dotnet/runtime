// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class DefaultIfEmptyTests
    {
        public static IEnumerable<object[]> EmptyData()
        {
            foreach (object[] query in UnorderedSources.Ranges(new[] { 0 }))
            {
                yield return new object[] { query[0], 1 };
            }
            yield return new object[] { Labeled.Label("Empty-Int", ParallelEnumerable.Empty<int>()), 1 };
            yield return new object[] { Labeled.Label("Empty-Decimal", ParallelEnumerable.Empty<decimal>()), 1.5M };
            yield return new object[] { Labeled.Label("Empty-String", ParallelEnumerable.Empty<string>()), "default" };
        }

        //
        // DefaultIfEmpty
        //
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void DefaultIfEmpty_Unordered_NotEmpty(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in query.DefaultIfEmpty())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(UnorderedSources))]
        public static void DefaultIfEmpty_Unordered_NotEmpty_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            DefaultIfEmpty_Unordered_NotEmpty(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void DefaultIfEmpty_NotEmpty(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.DefaultIfEmpty())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(seen, count);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(Sources))]
        public static void DefaultIfEmpty_NotEmpty_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            DefaultIfEmpty_NotEmpty(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void DefaultIfEmpty_Unordered_NotEmpty_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.DefaultIfEmpty().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(UnorderedSources))]
        public static void DefaultIfEmpty_Unordered_NotEmpty_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            DefaultIfEmpty_Unordered_NotEmpty_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void DefaultIfEmpty_NotEmpty_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.DefaultIfEmpty().ToList())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(seen, count);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(Sources))]
        public static void DefaultIfEmpty_NotEmpty_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            DefaultIfEmpty_NotEmpty_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("EmptyData")]
        public static void DefaultIfEmpty_Empty<T>(Labeled<ParallelQuery<T>> labeled, T def)
        {
            ParallelQuery<T> notEmpty = labeled.Item.DefaultIfEmpty();
            Assert.NotEmpty(notEmpty);
            Assert.Equal(1, notEmpty.Count());
            Assert.Single(notEmpty, default(T));

            ParallelQuery<T> specified = labeled.Item.DefaultIfEmpty(def);
            Assert.NotEmpty(specified);
            Assert.Equal(1, specified.Count());
            Assert.Single(specified, def);
        }

        [Theory]
        [MemberData("EmptyData")]
        public static void DefaultIfEmpty_Empty_NotPipelined<T>(Labeled<ParallelQuery<T>> labeled, T def)
        {
            IList<T> notEmpty = labeled.Item.DefaultIfEmpty().ToList();
            Assert.NotEmpty(notEmpty);
            Assert.Equal(1, notEmpty.Count());
            Assert.Single(notEmpty, default(T));

            IList<T> specified = labeled.Item.DefaultIfEmpty(def).ToList();
            Assert.NotEmpty(specified);
            Assert.Equal(1, specified.Count());
            Assert.Single(specified, def);
        }

        [Fact]
        public static void DefaultIfEmpty_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).DefaultIfEmpty());
        }
    }
}
