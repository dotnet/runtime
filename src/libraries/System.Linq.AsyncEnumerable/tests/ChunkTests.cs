// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ChunkTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Chunk<int>(null, 42));

            AsyncEnumerable.Chunk(AsyncEnumerable.Empty<int>(), 1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => AsyncEnumerable.Chunk(AsyncEnumerable.Empty<int>(), 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => AsyncEnumerable.Chunk(AsyncEnumerable.Empty<int>(), -1));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<int[]>(), AsyncEnumerable.Empty<int>().Chunk(1));
        }

#if NET
        [Fact]
        public async Task VariousValues_MatchesEnumerable()
        {
            Random rand = new(42);
            foreach (int collectionSize in new[] { 0, 1, 10, 50 })
            {
                foreach (int chunkSize in new[] { 1, 2, 3, 5, 60 })
                {
                    int[] ints = new int[collectionSize];
                    FillRandom(rand, ints);

                    foreach (IAsyncEnumerable<int> source in CreateSources(ints))
                    {
                        IEnumerable<int[]> chunksExpected = ints.Chunk(chunkSize);
                        IAsyncEnumerable<int[]> chunksActual = source.Chunk(chunkSize);

                        IEnumerator<int[]> e1 = chunksExpected.GetEnumerator();
                        IAsyncEnumerator<int[]> e2 = chunksActual.GetAsyncEnumerator();

                        while (e1.MoveNext())
                        {
                            Assert.True(await e2.MoveNextAsync());
                            Assert.Equal(e1.Current, e2.Current);
                        }

                        Assert.False(await e2.MoveNextAsync());

                        e1.Dispose();
                        await e2.DisposeAsync();
                    }
                }
            }
        }
#endif

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(2, 4, 8, 16);
            CancellationTokenSource cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (int[] item in source.Chunk(2).WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source = CreateSource(1, 2, 3, 4, 5, 6, 7, 8, 9, 10).Track();
            await ConsumeAsync(source.Chunk(3));
            Assert.Equal(11, source.MoveNextAsyncCount);
            Assert.Equal(10, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
