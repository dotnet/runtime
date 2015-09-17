// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ToArrayTests
    {
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToArray_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.ToArray(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(UnorderedSources))]
        public static void ToArray_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToArray_Unordered(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void ToArray(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.ToArray(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(Sources))]
        public static void ToArray_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToArray(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(Sources))]
        [MemberData("ThrowOnFirstEnumeration", MemberType = typeof(UnorderedSources))]
        public static void ToArray_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ToArray());
        }

        [Fact]
        public static void ToArray_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<bool>)null).ToArray());
        }
    }
}
