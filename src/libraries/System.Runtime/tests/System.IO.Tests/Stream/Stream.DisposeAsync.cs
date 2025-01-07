// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class StreamDisposeAsyncTests
    {
        [Fact]
        public async Task DisposeAsync_MultipleDisposalSuccessful()
        {
            var s = new DelegateStream();
            await s.DisposeAsync();
            await s.DisposeAsync();
        }

        [Fact]
        public void DisposeAsync_InvokesDisposeSynchronously()
        {
            int id = Environment.CurrentManagedThreadId;
            bool invoked = false;
            var s = new DelegateStream(disposeFunc: disposing =>
            {
                invoked = true;
                Assert.Equal(id, Environment.CurrentManagedThreadId);
            });
            Assert.True(s.DisposeAsync().IsCompletedSuccessfully);
            Assert.True(invoked);
        }
    }
}
