// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class PeriodicTimerTests
    {
        [Fact]
        public void Ctor_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => new PeriodicTimer(TimeSpan.FromMilliseconds(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => new PeriodicTimer(TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => new PeriodicTimer(TimeSpan.FromMilliseconds(uint.MaxValue)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(uint.MaxValue - 1)]
        public void Ctor_ValidArguments_Succeeds(uint milliseconds)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(milliseconds));
        }

        [Fact]
        public async Task Dispose_Idempotent()
        {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));

            Assert.True(await timer.WaitForNextTickAsync());

            for (int i = 0; i < 2; i++)
            {
                timer.Dispose();
                Assert.False(timer.WaitForNextTickAsync().Result);

                ((IDisposable)timer).Dispose();
                Assert.False(timer.WaitForNextTickAsync().Result);
            }
        }

        [Fact]
        public async Task WaitForNextTickAsync_TimerFires_ReturnsTrue()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));
            await Task.Delay(100);
            for (int i = 0; i < 3; i++)
            {
                Assert.True(await timer.WaitForNextTickAsync());
            }
            timer.Dispose();
            Assert.False(timer.WaitForNextTickAsync().Result);
        }

        [Fact]
        public async Task WaitForNextTickAsync_Dispose_ReturnsFalse()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(uint.MaxValue - 1));
            ValueTask<bool> task = timer.WaitForNextTickAsync();
            timer.Dispose();
            Assert.False(await task);
        }

        [Fact]
        public async Task WaitForNextTickAsync_ConcurrentDispose_ReturnsFalse()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(uint.MaxValue - 1));

            _ = Task.Run(async delegate
            {
                await Task.Delay(1);
                timer.Dispose();
            });

            Assert.False(await timer.WaitForNextTickAsync());
        }

        [Fact]
        public async Task WaitForNextTickAsync_ConcurrentDisposeAfterTicks_EventuallyReturnsFalse()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));

            for (int i = 0; i < 5; i++)
            {
                Assert.True(await timer.WaitForNextTickAsync());
            }

            _ = Task.Run(async delegate
            {
                await Task.Delay(1);
                timer.Dispose();
            });

            while (await timer.WaitForNextTickAsync());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void PeriodicTimer_NoActiveOperations_TimerNotRooted()
        {
            WeakReference<PeriodicTimer> timer = Create();

            WaitForTimerToBeCollected(timer, expected: true);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static WeakReference<PeriodicTimer> Create() =>
                new WeakReference<PeriodicTimer>(new PeriodicTimer(TimeSpan.FromMilliseconds(1)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public async Task PeriodicTimer_ActiveOperations_TimerRooted()
        {
            (WeakReference<PeriodicTimer> timer, ValueTask<bool> task) = Create();

            WaitForTimerToBeCollected(timer, expected: false);

            Assert.True(await task);

            WaitForTimerToBeCollected(timer, expected: true);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static (WeakReference<PeriodicTimer>, ValueTask<bool>) Create()
            {
                var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));
                ValueTask<bool> task = timer.WaitForNextTickAsync();
                return (new WeakReference<PeriodicTimer>(timer), task);
            }
        }

        [Fact]
        public void WaitForNextTickAsync_WaitAlreadyInProgress_Throws()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(uint.MaxValue - 1));

            ValueTask<bool> task = timer.WaitForNextTickAsync();
            Assert.False(task.IsCompleted);

            Assert.Throws<InvalidOperationException>(() => timer.WaitForNextTickAsync());

            Assert.False(task.IsCompleted);

            timer.Dispose();
            Assert.True(task.IsCompleted);
            Assert.False(task.Result);
        }

        [Fact]
        public void WaitForNextTickAsync_CanceledBeforeWait_CompletesSynchronously()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(uint.MaxValue - 1));

            var cts = new CancellationTokenSource();
            cts.Cancel();

            ValueTask<bool> task = timer.WaitForNextTickAsync(cts.Token);
            Assert.True(task.IsCanceled);
            Assert.Equal(cts.Token, Assert.ThrowsAny<OperationCanceledException>(() => task.Result).CancellationToken);
        }

        [Fact]
        public void WaitForNextTickAsync_CanceledAfterWait_CancelsOperation()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(uint.MaxValue - 1));

            var cts = new CancellationTokenSource();

            ValueTask<bool> task = timer.WaitForNextTickAsync(cts.Token);
            cts.Cancel();

            Assert.Equal(cts.Token, Assert.ThrowsAny<OperationCanceledException>(() => task.Result).CancellationToken);
        }

        [Fact]
        public async Task WaitForNextTickAsync_CanceledWaitThenWaitAgain_Succeeds()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1));

            var cts = new CancellationTokenSource();
            ValueTask<bool> task = timer.WaitForNextTickAsync(cts.Token);
            cts.Cancel();

            try
            {
                // If the task happens to succeed because the operation completes fast enough
                // that it beats the cancellation request, then make sure it completed successfully.
                Assert.True(await task);
            }
            catch (OperationCanceledException oce)
            {
                // Otherwise, it must have been canceled with the relevant token.
                Assert.Equal(cts.Token, oce.CancellationToken);
            }

            // We should be able to await the next tick either way.
            Assert.True(await timer.WaitForNextTickAsync());
        }

        private static void WaitForTimerToBeCollected(WeakReference<PeriodicTimer> timer, bool expected)
        {
            Assert.Equal(expected, SpinWait.SpinUntil(() =>
            {
                GC.Collect();
                return !timer.TryGetTarget(out _);
            }, TimeSpan.FromSeconds(expected ? 5 : 0.5)));
        }
    }
}
