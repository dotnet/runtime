// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;

public sealed class AsyncTests : IDisposable
{
    private bool _completed;
    private bool _disposed;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TaskTest(bool suspend)
    {
        if (suspend)
        {
            await Task.Yield();
        }

        Assert.False(_disposed);
        _completed = true;
    }

    public static IEnumerable<object[]> SuspensionData => new object[][] { new object[] { false }, new object[] { true } };

    [Theory]
    [MemberData(nameof(SuspensionData))]
    public async ValueTask ValueTaskTest(bool suspend)
    {
        if (suspend)
        {
            await Task.Yield();
        }

        Assert.False(_disposed);
        _completed = true;
    }

    [Fact]
    public ValueTask ValueTaskSourceTest() => new(new SingleConsumptionSource(this), 0);

    private sealed class SingleConsumptionSource : IValueTaskSource
    {
        private readonly AsyncTests _owner;
        private bool _consumed;
        private bool _ready;

        public SingleConsumptionSource(AsyncTests owner) => _owner = owner;

        public ValueTaskSourceStatus GetStatus(short token) => _ready ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Pending;

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            Complete();

            async void Complete()
            {
                await Task.Yield();
                _ready = true;
                continuation(state);
            }
        }

        public void GetResult(short token)
        {
            Assert.False(_consumed);
            Assert.True(_ready);
            Assert.False(_owner._disposed);
            _consumed = true;
            _owner._completed = true;
        }
    }

    public void Dispose()
    {
        Assert.True(_completed, "The runner must await the test before disposing its instance.");
        _disposed = true;
    }
}

public static class AsyncExitCodeTests
{
    [Fact]
    public static async Task<int> TaskExitCode()
    {
        await Task.Yield();
        return Success();
    }

    [Fact]
    public static async ValueTask<int> ValueTaskExitCode()
    {
        await Task.Yield();
        return Success();
    }

    private static int Success() => 100;
}
