// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class SkipSkipWhileTests
    {
        private static readonly Func<int, IEnumerable<int>> SkipPosition = x => new[] { -x, -1, 0, 1, x / 2, x, x * 2 }.Distinct();
        //
        // Skip
        //

        public static IEnumerable<object[]> SkipUnorderedData(int[] counts)
        {
            foreach (int count in counts.DefaultIfEmpty(Sources.OuterLoopCount / 4))
            {
                foreach (int position in SkipPosition(count))
                {
                    yield return new object[] { count, position };
                }
            }
        }

        public static IEnumerable<object[]> SkipData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount / 4), SkipPosition)) yield return results;
        }

        [Theory]
        [MemberData(nameof(SkipUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Skip_Unordered(int count, int skip)
        {
            // For unordered collections, which elements are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            foreach (int i in UnorderedSources.Default(count).Skip(skip))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Skip_Unordered_Longrunning(int count, int skip)
        {
            Skip_Unordered(count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipData), new[] { 0, 1, 2, 16 })]
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
        [MemberData(nameof(SkipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Skip_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            Skip(labeled, count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void Skip_Unordered_NotPipelined(int count, int skip)
        {
            // For unordered collections, which elements are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            Assert.All(UnorderedSources.Default(count).Skip(skip).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Skip_Unordered_NotPipelined_Longrunning(int count, int skip)
        {
            Skip_Unordered_NotPipelined(count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipData), new[] { 0, 1, 2, 16 })]
        public static void Skip_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            Assert.All(query.Skip(skip).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Skip_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            Skip_NotPipelined(labeled, count, skip);
        }

        [Fact]
        public static void Skip_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Skip(0));
        }

        //
        // SkipWhile
        //
        public static IEnumerable<object[]> SkipWhileData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount / 4)))
            {
                yield return new[] { results[0], results[1], new[] { 0 } };
                yield return new[] { results[0], results[1], Enumerable.Range((int)results[1] / 2, ((int)results[1] - 1) / 2 + 1).ToArray() };
                yield return new[] { results[0], results[1], new[] { (int)results[1] - 1 } };
            }
        }

        [Theory]
        [MemberData(nameof(SkipUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SkipWhile_Unordered(int count, int skip)
        {
            // For unordered collections, which elements (if any) are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            foreach (int i in UnorderedSources.Default(count).SkipWhile(x => x < skip))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_Unordered_Longrunning(int count, int skip)
        {
            SkipWhile_Unordered(count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipData), new[] { 0, 1, 2, 16 })]
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
        [MemberData(nameof(SkipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile(labeled, count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SkipWhile_Unordered_NotPipelined(int count, int skip)
        {
            // For unordered collections, which elements (if any) are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            Assert.All(UnorderedSources.Default(count).SkipWhile(x => x < skip).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_Unordered_NotPipelined_Longrunning(int count, int skip)
        {
            SkipWhile_Unordered_NotPipelined(count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipData), new[] { 0, 1, 2, 16 })]
        public static void SkipWhile_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            Assert.All(query.SkipWhile(x => x < skip).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_NotPipelined(labeled, count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SkipWhile_Indexed_Unordered(int count, int skip)
        {
            // For unordered collections, which elements (if any) are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            foreach (int i in UnorderedSources.Default(count).SkipWhile((x, index) => index < skip))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_Indexed_Unordered_Longrunning(int count, int skip)
        {
            SkipWhile_Indexed_Unordered(count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipData), new[] { 0, 1, 2, 16 })]
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
        [MemberData(nameof(SkipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_Indexed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_Indexed(labeled, count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SkipWhile_Indexed_Unordered_NotPipelined(int count, int skip)
        {
            // For unordered collections, which elements (if any) are skipped isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(Math.Max(skip, 0), Math.Min(count, Math.Max(0, count - skip)));
            Assert.All(UnorderedSources.Default(count).SkipWhile((x, index) => index < skip), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_Indexed_Unordered_NotPipelined_Longrunning(int count, int skip)
        {
            SkipWhile_Indexed_Unordered_NotPipelined(count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipData), new[] { 0, 1, 2, 16 })]
        public static void SkipWhile_Indexed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = Math.Max(0, skip);
            Assert.All(query.SkipWhile((x, index) => index < skip), x => Assert.Equal(seen++, x));
            Assert.Equal(Math.Max(skip, count), seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_Indexed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_Indexed_NotPipelined(labeled, count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipData), new[] { 0, 1, 2, 16 })]
        public static void SkipWhile_AllFalse(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.SkipWhile(x => false), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_AllFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_AllFalse(labeled, count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipData), new[] { 0, 1, 2, 16 })]
        public static void SkipWhile_AllTrue(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.Empty(query.SkipWhile(x => seen.Add(x)));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_AllTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int skip)
        {
            SkipWhile_AllTrue(labeled, count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipWhileData), new[] { 2, 16 })]
        public static void SkipWhile_SomeTrue(Labeled<ParallelQuery<int>> labeled, int count, int[] skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = skip.Min() == 0 ? 1 : 0;
            Assert.All(query.SkipWhile(x => skip.Contains(x)), x => Assert.Equal(seen++, x));
            Assert.Equal(skip.Min() >= count ? 0 : count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipWhileData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_SomeTrue_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int[] skip)
        {
            SkipWhile_SomeTrue(labeled, count, skip);
        }

        [Theory]
        [MemberData(nameof(SkipWhileData), new[] { 2, 16 })]
        public static void SkipWhile_SomeFalse(Labeled<ParallelQuery<int>> labeled, int count, int[] skip)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = skip.Min();
            Assert.All(query.SkipWhile(x => !skip.Contains(x)), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SkipWhileData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SkipWhile_SomeFalse_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int[] skip)
        {
            SkipWhile_SomeFalse(labeled, count, skip);
        }

        [Fact]
        public static void SkipWhile_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).SkipWhile(x => true));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<bool>().SkipWhile((Func<bool, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => ParallelEnumerable.Empty<bool>().SkipWhile((Func<bool, int, bool>)null));
        }
    }
}
