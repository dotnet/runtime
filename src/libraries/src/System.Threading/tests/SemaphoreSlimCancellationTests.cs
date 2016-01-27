// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(ct), ct, "CancelBeforeWait:  An OCE should have been thrown.");
            EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(millisec, ct), ct, "CancelBeforeWait:  An OCE should have been thrown.");
            EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(timeSpan, ct), ct, "CancelBeforeWait:  An OCE should have been thrown.");
            semaphoreSlim.Dispose();
        }

        [Fact]
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
               cancellationToken, "CancelAfterWait:  An OCE(null) should have been thrown that references the cancellationToken.");

            // the token should not have any listeners.
            // currently we don't expose this.. but it was verified manually
        }

        private static void EnsureOperationCanceledExceptionThrown(Action action, CancellationToken token, string message)
        {
            OperationCanceledException operationCanceledEx =
                Assert.Throws<OperationCanceledException>(action);
            Assert.Equal(token, operationCanceledEx.CancellationToken);
        }
    }
}
