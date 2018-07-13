// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class GetEnumeratorTests
    {
        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            while (enumerator.MoveNext())
            {
                int current = enumerator.Current;
                seen.Add(current);
                Assert.Equal(current, enumerator.Current);
            }
            seen.AssertComplete();

            if (labeled.ToString().StartsWith("Enumerable.Range") || labeled.ToString().StartsWith("Partitioner"))
            {
                if (count > 0)
                {
                    Assert.Throws<NotSupportedException>(() => enumerator.Reset());
                }
                // Reset behavior is undefined, and for count == 0, some singletons throw while others are nops.
            }
            else
            {
                enumerator.Reset();
                seen = new IntegerRangeSet(0, count);
                while (enumerator.MoveNext())
                {
                    Assert.True(seen.Add(enumerator.Current));
                }
                seen.AssertComplete();
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(UnorderedSources.OuterLoopRanges), MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GetEnumerator_Unordered(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void GetEnumerator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int seen = 0;
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            while (enumerator.MoveNext())
            {
                int current = enumerator.Current;
                Assert.Equal(seen++, current);
                Assert.Equal(current, enumerator.Current);
            }
            Assert.Equal(count, seen);

            if (labeled.ToString().StartsWith("Enumerable.Range") || labeled.ToString().StartsWith("Partitioner"))
            {
                if (count > 0)
                {
                    Assert.Throws<NotSupportedException>(() => enumerator.Reset());
                }
                // Reset behavior is undefined, and for count == 0, some singletons throw while others are nops.
            }
            else
            {
                enumerator.Reset();
                seen = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(seen++, enumerator.Current);
                }
                Assert.Equal(count, seen);
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void GetEnumerator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GetEnumerator(labeled, count);
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 128 }, MemberType = typeof(UnorderedSources))]
        [MemberData(nameof(Sources.Ranges), new[] { 128 }, MemberType = typeof(Sources))]
        public static void GetEnumerator_OperationCanceledException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            int countdown = 4;
            Action cancel = () => { if (Interlocked.Decrement(ref countdown) == 0) source.Cancel(); };

            OperationCanceledException oce = Assert.Throws<OperationCanceledException>(() => { foreach (var i in labeled.Item.WithCancellation(source.Token)) cancel(); });
            Assert.Equal(source.Token, oce.CancellationToken);
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1 }, MemberType = typeof(UnorderedSources))]
        [MemberData(nameof(Sources.Ranges), new[] { 1 }, MemberType = typeof(Sources))]
        public static void GetEnumerator_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Assert.Throws<OperationCanceledException>(() => { foreach (var i in labeled.Item.WithCancellation(new CancellationToken(canceled: true))) { throw new ShouldNotBeInvokedException(); }; });
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_MoveNextAfterQueryOpeningFailsIsIllegal(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item.Select<int, int>(x => { throw new DeliberateTestException(); }).OrderBy(x => x);

            IEnumerator<int> enumerator = query.GetEnumerator();

            //moveNext will cause queryOpening to fail (no element generated)
            AssertThrows.Wrapped<DeliberateTestException>(() => enumerator.MoveNext());

            //moveNext after queryOpening failed
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 16 }, MemberType = typeof(UnorderedSources))]
        [MemberData(nameof(Sources.Ranges), new[] { 16 }, MemberType = typeof(Sources))]
        public static void GetEnumerator_CurrentBeforeMoveNext(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            if (labeled.ToString().StartsWith("Partitioner")
                || labeled.ToString().StartsWith("Array"))
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            }
            else
            {
                Assert.InRange(enumerator.Current, 0, count);
            }
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void GetEnumerator_MoveNextAfterEnd(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IEnumerator<int> enumerator = labeled.Item.GetEnumerator();
            while (enumerator.MoveNext())
            {
                count--;
            }
            Assert.Equal(0, count);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public static void GetEnumerator_LargeQuery_PauseAfterOpening()
        {
            using (IEnumerator<int> e = Enumerable.Range(0, 8192).AsParallel().SkipWhile(i => true).GetEnumerator())
            {
                e.MoveNext();
                Task.Delay(100).Wait(); // verify nothing goes haywire when the internal buffer is allowed to fill
                while (e.MoveNext()) ;
                Assert.False(e.MoveNext());
            }
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2 }, MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_DisposeBeforeFirstMoveNext(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IEnumerator<int> e = labeled.Item.Select(i => i).GetEnumerator();
            e.Dispose();
            Assert.Throws<ObjectDisposedException>(() => e.MoveNext());
        }

        [Theory]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 1, 2 }, MemberType = typeof(UnorderedSources))]
        public static void GetEnumerator_DisposeAfterMoveNext(Labeled<ParallelQuery<int>> labeled, int count)
        {
            IEnumerator<int> e = labeled.Item.Select(i => i).GetEnumerator();
            e.MoveNext();
            e.Dispose();
            Assert.Throws<ObjectDisposedException>(() => e.MoveNext());
        }

    }
}
