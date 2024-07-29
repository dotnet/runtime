// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class TimerDisposeTests
    {
        [Fact]
        public void Dispose_NotFired_WaitHandleSignaledImmediately()
        {
            var t = new Timer(_ => { }, null, int.MaxValue, int.MaxValue);
            using (var mre = new ManualResetEvent(false))
            {
                Assert.True(t.Dispose(mre));
                Assert.True(mre.WaitOne(0));

                mre.Reset();
                Assert.False(t.Dispose(mre));
                Assert.False(mre.WaitOne(0));
            }
        }

        [Fact]
        public async Task DisposeAsync_DisposesTimer()
        {
            var t = new Timer(_ => { });
            await t.DisposeAsync();
            Assert.False(t.Change(-1, -1));
        }

        [Fact]
        public async Task DisposeAsync_CanBeCalledMultipleTimes()
        {
            var t = new Timer(_ => { });
            await t.DisposeAsync();
            t.Dispose();
            await t.DisposeAsync();
            t.Dispose(new ManualResetEvent(false));
            await t.DisposeAsync();
        }

        [Fact]
        public void DisposeAsync_SignalsImmediatelyWhenTaskNotRunning()
        {
            var t = new Timer(_ => { });
            Assert.True(t.DisposeAsync().IsCompletedSuccessfully);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task DisposeAsync_DisposeDelayedUntilCallbacksComplete()
        {
            using (var b = new Barrier(2))
            {
                var t = new Timer(_ =>
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                }, null, 1, -1);

                b.SignalAndWait();
                ValueTask vt = t.DisposeAsync();
                await Task.Delay(1);
                Assert.False(vt.IsCompleted);
                b.SignalAndWait();
                await vt;

                GC.KeepAlive(t);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task DisposeAsync_MultipleDisposesBeforeCompletionReturnSameTask()
        {
            using (var b = new Barrier(2))
            {
                var t = new Timer(_ =>
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                }, null, 1, -1);

                b.SignalAndWait();
                Task vt1 = t.DisposeAsync().AsTask();
                Task vt2 = t.DisposeAsync().AsTask();
                Assert.Same(vt2, vt1);
                await Task.Delay(1);
                Assert.False(vt1.IsCompleted);
                b.SignalAndWait();
                await vt1;
                await vt2;

                GC.KeepAlive(t);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task DisposeAsync_AfterDisposeWorks()
        {
            using (var b = new Barrier(2))
            {
                var t = new Timer(_ =>
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                }, null, 1, -1);

                b.SignalAndWait();
                t.Dispose();
                ValueTask vt = t.DisposeAsync();
                await Task.Delay(1);
                Assert.False(vt.IsCompleted);
                b.SignalAndWait();
                await vt;

                GC.KeepAlive(t);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task DisposeAsync_AfterDisposeWaitHandleThrows()
        {
            using (var b = new Barrier(2))
            {
                var t = new Timer(_ =>
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                }, null, 1, -1);

                var mre = new ManualResetEvent(false);
                b.SignalAndWait();
                t.Dispose(mre);
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await t.DisposeAsync());
                b.SignalAndWait();
                mre.WaitOne();

                GC.KeepAlive(t);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task DisposeAsync_ThenDisposeWaitHandleReturnsFalse()
        {
            using (var b = new Barrier(2))
            {
                var t = new Timer(_ =>
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                }, null, 1, -1);

                b.SignalAndWait();
                ValueTask vt = t.DisposeAsync();
                using (var mre = new ManualResetEvent(false))
                {
                    Assert.False(t.Dispose(mre));
                    Assert.False(mre.WaitOne(0));
                }
                b.SignalAndWait();
                await vt;

                GC.KeepAlive(t);
            }
        }
    }
}
