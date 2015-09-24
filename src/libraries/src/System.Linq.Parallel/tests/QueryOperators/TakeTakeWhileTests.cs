// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class TakeTakeWhileTests
    {
        //
        // Take
        //
        public static IEnumerable<object[]> TakeUnorderedData(int[] counts)
        {
            Func<int, IEnumerable<int>> take = x => new[] { -x, -1, 0, 1, x / 2, x, x * 2 }.Distinct();
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), take)) yield return results;
        }

        public static IEnumerable<object[]> TakeData(int[] counts)
        {
            Func<int, IEnumerable<int>> take = x => new[] { -x, -1, 0, 1, x / 2, x, x * 2 }.Distinct();
            foreach (object[] results in Sources.Ranges(counts.Cast<int>(), take)) yield return results;
        }

        [Theory]
        [MemberData("TakeUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Take_Unordered(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            foreach (int i in query.Take(take))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void Take_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            Take_Unordered(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Take(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.Take(take))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeData", (object)(new int[] { 1024 * 32 }))]
        public static void Take_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            Take(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Take_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            Assert.All(query.Take(take).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void Take_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            Take_Unordered_NotPipelined(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Take_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Take(take).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeData", (object)(new int[] { 1024 * 32 }))]
        public static void Take_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            Take_NotPipelined(labeled, count, take);
        }

        [Fact]
        public static void Take_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).Take(0));
        }

        //
        // TakeWhile
        //
        public static IEnumerable<object[]> TakeWhileData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.Cast<int>()))
            {
                yield return new[] { results[0], results[1], new[] { 0 } };
                yield return new[] { results[0], results[1], Enumerable.Range((int)results[1] / 2, ((int)results[1] - 1) / 2 + 1) };
                yield return new[] { results[0], results[1], new[] { (int)results[1] - 1 } };
            }
        }

        [Theory]
        [MemberData("TakeUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_Unordered(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            foreach (int i in query.TakeWhile(x => x < take))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_Unordered(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.TakeWhile(x => x < take))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            Assert.All(query.TakeWhile(x => x < take).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_Unordered_NotPipelined(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile(x => x < take).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_NotPipelined(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_Indexed_Unordered(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            foreach (int i in query.TakeWhile((x, index) => index < take))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_Indexed_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_Indexed_Unordered(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_Indexed(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.TakeWhile((x, index) => index < take))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_Indexed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_Indexed(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_Indexed_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            Assert.All(query.TakeWhile((x, index) => index < take).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_Indexed_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_Indexed_Unordered_NotPipelined(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_Indexed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile((x, index) => index < take).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_Indexed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_Indexed_NotPipelined(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeUnorderedData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_AllFalse(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Empty(query.TakeWhile(x => false));
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeUnorderedData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_AllFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_AllFalse(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void TakeWhile_AllTrue(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile(x => true), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_AllTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_AllTrue(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeWhileData", (object)(new int[] { 2, 16 }))]
        public static void TakeWhile_SomeTrue(Labeled<ParallelQuery<int>> labeled, int count, IEnumerable<int> take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile(x => take.Contains(x)), x => Assert.Equal(seen++, x));
            Assert.Equal(take.Min() > 0 ? 0 : take.Max() + 1, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeWhileData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_SomeTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, IEnumerable<int> take)
        {
            TakeWhile_SomeTrue(labeled, count, take);
        }

        [Theory]
        [MemberData("TakeWhileData", (object)(new int[] { 2, 16 }))]
        public static void TakeWhile_SomeFalse(Labeled<ParallelQuery<int>> labeled, int count, IEnumerable<int> take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile(x => !take.Contains(x)), x => Assert.Equal(seen++, x));
            Assert.Equal(take.Min(), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("TakeWhileData", (object)(new int[] { 1024 * 32 }))]
        public static void TakeWhile_SomeFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, IEnumerable<int> take)
        {
            TakeWhile_SomeFalse(labeled, count, take);
        }

        [Fact]
        public static void TakeWhile_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).TakeWhile(x => true));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().TakeWhile((Func<bool, bool>)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<bool>().TakeWhile((Func<bool, int, bool>)null));
        }
    }
}
