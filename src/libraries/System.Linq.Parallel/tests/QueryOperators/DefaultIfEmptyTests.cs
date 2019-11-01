// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class DefaultIfEmptyTests
    {
        public static IEnumerable<object[]> EmptyData()
        {
            foreach (object[] query in UnorderedSources.Ranges(new[] { 0 }))
            {
                yield return new object[] { query[0], 1 };
            }
            foreach (object[] query in Sources.Ranges(new[] { 0 }))
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
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void DefaultIfEmpty_Unordered_NotEmpty(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in UnorderedSources.Default(count).DefaultIfEmpty())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void DefaultIfEmpty_Unordered_NotEmpty_Longrunning()
        {
            DefaultIfEmpty_Unordered_NotEmpty(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(Sources))]
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
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void DefaultIfEmpty_NotEmpty_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            DefaultIfEmpty_NotEmpty(labeled, count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void DefaultIfEmpty_Unordered_NotEmpty_NotPipelined(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).DefaultIfEmpty().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void DefaultIfEmpty_Unordered_NotEmpty_NotPipelined_Longrunning()
        {
            DefaultIfEmpty_Unordered_NotEmpty_NotPipelined(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(Sources))]
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
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void DefaultIfEmpty_NotEmpty_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            DefaultIfEmpty_NotEmpty_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(EmptyData))]
        [ActiveIssue("https://github.com/xunit/xunit/issues/1771")]
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
        [MemberData(nameof(EmptyData))]
        [ActiveIssue("https://github.com/xunit/xunit/issues/1771")]
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
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).DefaultIfEmpty());
        }
    }
}
