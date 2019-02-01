// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class ConfiguredAsyncDisposableTests
    {
        [Fact]
        public void Default_GetAsyncEnumerator_Throws()
        {
            ConfiguredAsyncDisposable d = default;
            Assert.Throws<NullReferenceException>(() => d.DisposeAsync());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DisposeAsync_InvokesUnderlyingDisposeAsync(bool continueOnCapturedContext)
        {
            int invokeCount = 0;
            var tcs = new TaskCompletionSource<int>();
            var vt = new ValueTask(tcs.Task);

            var d = new CustomAsyncDisposable(() =>
            {
                invokeCount++;
                return vt;
            });

            Assert.Equal(vt.ConfigureAwait(continueOnCapturedContext), d.ConfigureAwait(continueOnCapturedContext).DisposeAsync());
            Assert.Equal(1, invokeCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DisposeAsync_ContinuesOnCapturedContextIfExpected(bool continueOnCapturedContext)
        {
            var d = new TrackingAsyncDisposable();
            d.ConfigureAwait(continueOnCapturedContext).DisposeAsync().GetAwaiter().UnsafeOnCompleted(() => { });
            Assert.Equal(
                continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None,
                d.Flags);
        }

        private sealed class CustomAsyncDisposable : IAsyncDisposable
        {
            private readonly Func<ValueTask> _action;

            public CustomAsyncDisposable(Func<ValueTask> action) => _action = action;

            public ValueTask DisposeAsync() => _action();
        }

        private sealed class TrackingAsyncDisposable : IAsyncDisposable, IValueTaskSource
        {
            public ValueTaskSourceOnCompletedFlags Flags;

            public ValueTask DisposeAsync() => new ValueTask(this, 0);

            public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => Flags = flags;
            public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Pending;
            public bool GetResult(short token) => throw new NotImplementedException();
            void IValueTaskSource.GetResult(short token) => throw new NotImplementedException();
        }
    }
}
