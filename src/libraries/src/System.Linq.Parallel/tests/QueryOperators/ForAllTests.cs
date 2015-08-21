// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ForAllTests
    {
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ForAll(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            query.ForAll<int>(x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void ForAll_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ForAll(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void ForAll_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ForAll(x => { }));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void ForAll_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ForAll(x => { throw new DeliberateTestException(); }));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Select((Func<int, int>)(x => { throw new DeliberateTestException(); })).ForAll(x => { }));
        }

        [Fact]
        public static void ForAll_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).ForAll(x => { }));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).ForAll(null));
        }
    }
}
