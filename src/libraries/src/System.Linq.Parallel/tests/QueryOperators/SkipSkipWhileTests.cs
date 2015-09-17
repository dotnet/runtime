// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class SkipSkipWhileTests
    {
        //
        // Skip
        //

        public static IEnumerable<object[]> SkipUnorderedData(int[] counts)
        {
            Func<int, IEnumerable<int>> skip = x => new[] { -x, -1, 0, 1, x / 2, x, x * 2 }.Distinct();
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), skip)) yield return results;
        }

        public static IEnumerable<object[]> SkipData(int[] counts)
        {
            Func<int, IEnumerable<int>> skip = x => new[] { -x, -1, 0, 1, x / 2, x, x * 2 }.Distinct();
            foreach (object[] results in Sources.Ranges(counts.Cast<int>(), skip)) yield return results;
        }

        [Theory]
        [MemberData("SkipUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Skip_Unordered(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            foreach (int i in query.Skip(skip))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void Skip_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            Skip_Unordered(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Skip(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            foreach (int i in query.Skip(skip))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipData", (object)(new int[] { 1024 * 32 }))]
        public static void Skip_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            Skip(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Skip_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            Assert.All(query.Skip(skip).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void Skip_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            Skip_Unordered_NotPipelined(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Skip_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            Assert.All(query.Skip(skip).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipData", (object)(new int[] { 1024 * 32 }))]
        public static void Skip_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            Skip_NotPipelined(labeled, count, skip);
        }

        [Fact]
        public static void Skip_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).Skip(0));
        }

        //
        // SkipWhile
        //
        public static IEnumerable<object[]> SkipWhileData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.Cast<int>()))
            {
                yield return new[] { results[0], results[1], new[] { 0 } };
                yield return new[] { results[0], results[1], Enumerable.Range((int)results[1] / 2, ((int)results[1] - 1) / 2 + 1) };
                yield return new[] { results[0], results[1], new[] { (int)results[1] - 1 } };
            }
        }

        [Theory]
        [MemberData("SkipUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_Unordered(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            foreach (int i in query.SkipWhile(x => x < skip))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_Unordered(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            foreach (int i in query.SkipWhile(x => x < skip))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            Assert.All(query.SkipWhile(x => x < skip).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_Unordered_NotPipelined(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            Assert.All(query.SkipWhile(x => x < skip).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_NotPipelined(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_Indexed_Unordered(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            foreach (int i in query.SkipWhile((x, index) => index < skip))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_Indexed_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_Indexed_Unordered(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_Indexed(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            foreach (int i in query.SkipWhile((x, index) => index < skip))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_Indexed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_Indexed(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_Indexed_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            Assert.All(query.SkipWhile((x, index) => index < skip), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_Indexed_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_Indexed_Unordered_NotPipelined(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_Indexed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            Assert.All(query.SkipWhile((x, index) => index < skip), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_Indexed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_Indexed_NotPipelined(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_AllFalse(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.SkipWhile(x => false), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_AllFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_AllFalse(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void SkipWhile_AllTrue(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Empty(query.SkipWhile(x => seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_AllTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_AllTrue(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipWhileData", (object)(new int[] { 2, 16 }))]
        public static void SkipWhile_SomeTrue(Labeled<ParallelQuery<int>> labeled, int count, IEnumerable<int> skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = skip.Min() == 0 ? 1 : 0;
            Assert.All(query.SkipWhile(x => skip.Contains(x)), x => Assert.Equal(seen++, x));
            Assert.Equal(skip.Min() >= count ? 0 : count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipWhileData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_SomeTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, IEnumerable<int> skip)
        {
            SkipWhile_SomeTrue(labeled, count, skip);
        }

        [Theory]
        [MemberData("SkipWhileData", (object)(new int[] { 2, 16 }))]
        public static void SkipWhile_SomeFalse(Labeled<ParallelQuery<int>> labeled, int count, IEnumerable<int> skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = skip.Min();
            Assert.All(query.SkipWhile(x => !skip.Contains(x)), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("SkipWhileData", (object)(new int[] { 1024 * 32 }))]
        public static void SkipWhile_SomeFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, IEnumerable<int> skip)
        {
            SkipWhile_SomeFalse(labeled, count, skip);
        }

        [Fact]
        public static void SkipWhile_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).SkipWhile(x => true));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().SkipWhile((Func<bool, bool>)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().SkipWhile((Func<bool, int, bool>)null));
        }
    }
}
