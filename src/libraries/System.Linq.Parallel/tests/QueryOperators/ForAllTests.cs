// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ForAllTests
    {
        [Theory]
        [MemberData(nameof(Sources.Ranges), (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData(nameof(Sources.Ranges), (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ForAll(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            query.ForAll<int>(x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(Sources))]
        [MemberData(nameof(Sources.Ranges), (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }), MemberType = typeof(UnorderedSources))]
        public static void ForAll_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ForAll(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void ForAll_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ForAll(x => { }));
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
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
