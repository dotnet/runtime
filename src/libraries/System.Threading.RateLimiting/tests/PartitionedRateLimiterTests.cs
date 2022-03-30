// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.RateLimiting.Tests
{
    public class PartitionedRateLimiterTests
    {
        [Fact]
        public void ThrowsWhenAcquiringLessThanZero()
        {
            using var limiter = new NotImplementedPartitionedRateLimiter<string>();
            Assert.Throws<ArgumentOutOfRangeException>(() => limiter.Acquire(string.Empty, -1));
        }

        [Fact]
        public async Task ThrowsWhenWaitingForLessThanZero()
        {
            using var limiter = new NotImplementedPartitionedRateLimiter<string>();
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await limiter.WaitAsync(string.Empty, -1));
        }

        [Fact]
        public async Task WaitAsyncThrowsWhenPassedACanceledToken()
        {
            using var limiter = new NotImplementedPartitionedRateLimiter<string>();
            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await limiter.WaitAsync(string.Empty, 1, new CancellationToken(true)));
        }

        internal class NotImplementedPartitionedRateLimiter<T> : PartitionedRateLimiter<T>
        {
            public override int GetAvailablePermits(T resourceID) => throw new NotImplementedException();
            protected override RateLimitLease AcquireCore(T resourceID, int permitCount) => throw new NotImplementedException();
            protected override ValueTask<RateLimitLease> WaitAsyncCore(T resourceID, int permitCount, CancellationToken cancellationToken) => throw new NotImplementedException();
        }
    }
}
