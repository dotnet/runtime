// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class EmptyTests : AsyncEnumerableTests
    {
        [Fact]
        public void Empty_Idempotent()
        {
            IAsyncEnumerable<int> ae = AsyncEnumerable.Empty<int>();
            Assert.NotNull(ae);
            Assert.Same(ae, AsyncEnumerable.Empty<int>());
            Assert.NotSame(ae, AsyncEnumerable.Empty<long>());

            IAsyncEnumerator<int> e = ae.GetAsyncEnumerator(default);
            Assert.Same(e, ae.GetAsyncEnumerator(default));
            Assert.Same(e, ae.GetAsyncEnumerator(new CancellationToken(true)));
        }

        [Fact]
        public void Empty_ContainsZeroElements()
        {
            IAsyncEnumerator<int> e = AsyncEnumerable.Empty<int>().GetAsyncEnumerator(default);

            for (int i = 0; i < 2; i++)
            {
                ValueTask<bool> mn = e.MoveNextAsync();
                Assert.True(mn.IsCompleted);
                Assert.False(mn.Result);
                Assert.Equal(0, e.Current); // implementation detail
            }

            ValueTask d = e.DisposeAsync();
            Assert.True(d.IsCompleted);
        }
    }
}
