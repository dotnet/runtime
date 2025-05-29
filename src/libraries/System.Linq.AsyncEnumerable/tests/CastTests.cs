// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class CastTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.Cast<string>(null));
        }

        [Fact]
        public void Empty_ProducesEmpty() // validating an optimization / implementation detail
        {
            Assert.Same(AsyncEnumerable.Empty<string>(), AsyncEnumerable.Empty<object>().Cast<string>());
            Assert.Same(AsyncEnumerable.Empty<double>(), AsyncEnumerable.Empty<object>().Cast<double>());
        }

        [Fact]
        public async Task NullAndNonNull_IncludesNulls()
        {
            await AssertEqual(["2", null, "8", null], CreateSource("2", null, "8", null).Cast<string>());
            await AssertEqual(["2", null, "8", null], CreateSource("2", null, "8", null).Cast<object>());
            await AssertEqual(["2", null, "8", null], CreateSource<object>("2", null, "8", null).Cast<string>());
            await AssertEqual([2, null, 8, null], CreateSource<object>(2, null, 8, null).Cast<int?>());
            await AssertEqual([2, 8], CreateSource<object>(2, 8).Cast<int>());
        }

        [Fact]
        public async Task IncorrectType_Throws()
        {
            await Assert.ThrowsAsync<InvalidCastException>(async () => await ConsumeAsync(CreateSource<object>(2, 8).Cast<string>()));
            await Assert.ThrowsAsync<InvalidCastException>(async () => await ConsumeAsync(CreateSource("2", "8").Cast<int>()));
            await Assert.ThrowsAsync<InvalidCastException>(async () => await ConsumeAsync(CreateSource("2", "8").Cast<CastTests>()));
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<string> source = CreateSource("2", null, "8", null);
            CancellationTokenSource cts = new();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (string item in source.Cast<string>().WithCancellation(cts.Token))
                {
                    cts.Cancel();
                }
            });
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<object> source = CreateSource<object>("1", "2", "3").Track();
            await ConsumeAsync(source.Cast<string>());
            Assert.Equal(4, source.MoveNextAsyncCount);
            Assert.Equal(3, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
