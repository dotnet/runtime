// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static class SemaphoreSlimCancellationTests
    {
        [Fact]
        public static void CancelBeforeWait()
        {
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(2);

            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            CancellationToken ct = cs.Token;

            const int millisec = 100;
            TimeSpan timeSpan = new TimeSpan(100);
            EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(ct), ct);
            EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(millisec, ct), ct);
            EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(timeSpan, ct), ct);
            semaphoreSlim.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        public static void CancelAfterWait()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0); // semaphore that will block all waiters

            Task.Run(
                () =>
                {
                    for (int i = 0; i < 300; i++) ;
                    cancellationTokenSource.Cancel();
                });

            //Now wait.. the wait should abort and an exception should be thrown
            EnsureOperationCanceledExceptionThrown(
               () => semaphoreSlim.Wait(cancellationToken),
               cancellationToken);

            // the token should not have any listeners.
            // currently we don't expose this.. but it was verified manually
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task Cancel_WaitAsync_ContinuationInvokedAsynchronously(bool withTimeout)
        {
            await Task.Run(async () => // escape xunit's SynchronizationContext
            {
                var cts = new CancellationTokenSource();
                var tl = new ThreadLocal<object>();

                var sentinel = new object();

                var sem = new SemaphoreSlim(0);
                Task waitTask = withTimeout ?
                    sem.WaitAsync(TimeSpan.FromDays(1), cts.Token) :
                    sem.WaitAsync(cts.Token);
                Task continuation = waitTask.ContinueWith(prev =>
                {
                    Assert.Equal(TaskStatus.Canceled, prev.Status);
                    Assert.NotSame(sentinel, tl.Value);
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                Assert.Equal(TaskStatus.WaitingForActivation, continuation.Status);
                Assert.Equal(0, sem.CurrentCount);

                tl.Value = sentinel;
                cts.Cancel();
                tl.Value = null;

                await continuation;
            });
        }

        private static void EnsureOperationCanceledExceptionThrown(Action action, CancellationToken token)
        {
            OperationCanceledException operationCanceledEx =
                Assert.Throws<OperationCanceledException>(action);
            Assert.Equal(token, operationCanceledEx.CancellationToken);
        }
    }
}
