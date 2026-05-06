// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class OfTypeTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.OfType<string>(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.OfType<int>(null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<object>().OfType<string>());
            Assert.Same(AsyncEnumerable.Empty<int>(), AsyncEnumerable.Empty<object>().OfType<int>());
        }

        [Fact]
        public async Task NullAndNonNull_SkipsNulls()
        {
            await AssertEqual(["2", "8"], CreateSource("2", null, "8", null).OfType<string>());
            await AssertEqual(["2", "8"], CreateSource<object>("2", null, "8", null).OfType<object>());
            await AssertEqual([2, 8], CreateSource<object>(2, null, 8, null).OfType<int>());
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<string> source = CreateSource("2", null, "8", null);
            CancellationTokenSource cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (string item in source.OfType<string>().WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<object> source = CreateSource<object>(2, null, 8, 16).Track();
            await ConsumeAsync(source.OfType<int>());
            Assert.Equal(5, source.MoveNextAsyncCount);
            Assert.Equal(4, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
