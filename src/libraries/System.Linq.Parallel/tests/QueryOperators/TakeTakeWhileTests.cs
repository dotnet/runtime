// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class TakeTakeWhileTests
    {
        private static readonly Func<int, IEnumerable<int>> TakePosition = x => new[] { -x, -1, 0, 1, x / 2, x, x * 2 }.Distinct();

        //
        // Take
        //

        public static IEnumerable<object[]> TakeUnorderedData(int[] counts)
        {
            foreach (int count in counts.DefaultIfEmpty(Sources.OuterLoopCount / 4))
            {
                foreach (int position in TakePosition(count))
                {
                    yield return new object[] { count, position };
                }
            }
        }

        public static IEnumerable<object[]> TakeData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount / 4), TakePosition)) yield return results;
        }

        [Theory]
        [MemberData(nameof(TakeUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Take_Unordered(int count, int take)
        {
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            foreach (int i in UnorderedSources.Default(count).Take(take))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Take_Unordered_Longrunning(int count, int take)
        {
            Take_Unordered(count, take);
        }

        [Theory]
        [MemberData(nameof(TakeData), new[] { 0, 1, 2, 16 })]
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
        [MemberData(nameof(TakeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Take_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            Take(labeled, count, take);
        }

        [Theory]
        [MemberData(nameof(TakeUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Take_Unordered_NotPipelined(int count, int take)
        {
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            Assert.All(UnorderedSources.Default(count).Take(take).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Take_Unordered_NotPipelined_Longrunning(int count, int take)
        {
            Take_Unordered_NotPipelined(count, take);
        }

        [Theory]
        [MemberData(nameof(TakeData), new[] { 0, 1, 2, 16 })]
        public static void Take_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Take(take).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Take_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            Take_NotPipelined(labeled, count, take);
        }

        [Fact]
        public static void Take_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Take(0));
        }

        //
        // TakeWhile
        //
        public static IEnumerable<object[]> TakeWhileData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount / 4)))
            {
                yield return new[] { results[0], results[1], new[] { 0 } };
                yield return new[] { results[0], results[1], Enumerable.Range((int)results[1] / 2, ((int)results[1] - 1) / 2 + 1).ToArray() };
                yield return new[] { results[0], results[1], new[] { (int)results[1] - 1 } };
            }
        }

        [Theory]
        [MemberData(nameof(TakeUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void TakeWhile_Unordered(int count, int take)
        {
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            foreach (int i in UnorderedSources.Default(count).TakeWhile(x => x < take))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_Unordered_Longrunning(int count, int take)
        {
            TakeWhile_Unordered(count, take);
        }

        [Theory]
        [MemberData(nameof(TakeData), new[] { 0, 1, 2, 16 })]
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
        [MemberData(nameof(TakeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile(labeled, count, take);
        }

        [Theory]
        [MemberData(nameof(TakeUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void TakeWhile_Unordered_NotPipelined(int count, int take)
        {
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            Assert.All(UnorderedSources.Default(count).TakeWhile(x => x < take).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_Unordered_NotPipelined_Longrunning(int count, int take)
        {
            TakeWhile_Unordered_NotPipelined(count, take);
        }

        [Theory]
        [MemberData(nameof(TakeData), new[] { 0, 1, 2, 16 })]
        public static void TakeWhile_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile(x => x < take).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_NotPipelined(labeled, count, take);
        }

        [Theory]
        [MemberData(nameof(TakeUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void TakeWhile_Indexed_Unordered(int count, int take)
        {
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            foreach (int i in UnorderedSources.Default(count).TakeWhile((x, index) => index < take))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_Indexed_Unordered_Longrunning(int count, int take)
        {
            TakeWhile_Indexed_Unordered(count, take);
        }

        [Theory]
        [MemberData(nameof(TakeData), new[] { 0, 1, 2, 16 })]
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
        [MemberData(nameof(TakeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_Indexed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_Indexed(labeled, count, take);
        }

        [Theory]
        [MemberData(nameof(TakeUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void TakeWhile_Indexed_Unordered_NotPipelined(int count, int take)
        {
            // For unordered collections, which elements (if any) are taken isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, Math.Min(count, Math.Max(0, take)));
            Assert.All(UnorderedSources.Default(count).TakeWhile((x, index) => index < take).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_Indexed_Unordered_NotPipelined_Longrunning(int count, int take)
        {
            TakeWhile_Indexed_Unordered_NotPipelined(count, take);
        }

        [Theory]
        [MemberData(nameof(TakeData), new[] { 0, 1, 2, 16 })]
        public static void TakeWhile_Indexed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile((x, index) => index < take).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Min(count, Math.Max(0, take)), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_Indexed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_Indexed_NotPipelined(labeled, count, take);
        }

        [Theory]
        [MemberData(nameof(TakeUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void TakeWhile_AllFalse(int count, int take)
        {
            Assert.Empty(UnorderedSources.Default(count).TakeWhile(x => false));
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_AllFalse_Longrunning(int count, int take)
        {
            TakeWhile_AllFalse(count, take);
        }

        [Theory]
        [MemberData(nameof(TakeData), new[] { 0, 1, 2, 16 })]
        public static void TakeWhile_AllTrue(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile(x => true), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_AllTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int take)
        {
            TakeWhile_AllTrue(labeled, count, take);
        }

        [Theory]
        [MemberData(nameof(TakeWhileData), new[] { 2, 16 })]
        public static void TakeWhile_SomeTrue(Labeled<ParallelQuery<int>> labeled, int count, int[] take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile(x => take.Contains(x)), x => Assert.Equal(seen++, x));
            Assert.Equal(take.Min() > 0 ? 0 : take.Max() + 1, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeWhileData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_SomeTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int[] take)
        {
            TakeWhile_SomeTrue(labeled, count, take);
        }

        [Theory]
        [MemberData(nameof(TakeWhileData), new[] { 2, 16 })]
        public static void TakeWhile_SomeFalse(Labeled<ParallelQuery<int>> labeled, int count, int[] take)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.TakeWhile(x => !take.Contains(x)), x => Assert.Equal(seen++, x));
            Assert.Equal(take.Min(), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(TakeWhileData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void TakeWhile_SomeFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int[] take)
        {
            TakeWhile_SomeFalse(labeled, count, take);
        }

        [Fact]
        public static void TakeWhile_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).TakeWhile(x => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<bool>().TakeWhile((Func<bool, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<bool>().TakeWhile((Func<bool, int, bool>)null));
        }
    }
}
