// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static class CountdownEventCancellationTests
    {
        [Fact]
        public static void CancelBeforeWait()
        {
            CountdownEvent countdownEvent = new CountdownEvent(2);
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            CancellationToken ct = cs.Token;

            const int millisec = 100;
            TimeSpan timeSpan = new TimeSpan(100);
            string message = "CancelBeforeWait:  > Cancellation token does not match.";
            EnsureOperationCanceledExceptionThrown(() => countdownEvent.Wait(ct), ct, message);
            EnsureOperationCanceledExceptionThrown(() => countdownEvent.Wait(millisec, ct), ct, message);
            EnsureOperationCanceledExceptionThrown(() => countdownEvent.Wait(timeSpan, ct), ct, message);

            countdownEvent.Dispose();
        }

        [Fact]
        public static void CancelAfterWait()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            CountdownEvent countdownEvent = new CountdownEvent(2); ;  // countdownEvent that will block all waiters

            Task.Run(() =>
            {
                cancellationTokenSource.Cancel();
            });

            //Now wait.. the wait should abort and an exception should be thrown
            EnsureOperationCanceledExceptionThrown(() => countdownEvent.Wait(cancellationToken), cancellationToken,
               "CancelAfterWait:  An OCE(null) should have been thrown that references the cancellationToken.");

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

