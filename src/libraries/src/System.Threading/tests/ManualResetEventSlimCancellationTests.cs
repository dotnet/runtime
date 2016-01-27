// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static class ManualResetEventCancellationTests
    {
        [Fact]
        public static void CancelBeforeWait()
        {
            ManualResetEventSlim mres = new ManualResetEventSlim();
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            CancellationToken ct = cs.Token;

            const int millisec = 100;
            TimeSpan timeSpan = new TimeSpan(100);

            EnsureOperationCanceledExceptionThrown(
               () => mres.Wait(ct), ct, "CancelBeforeWait:  An OCE should have been thrown.");
            EnsureOperationCanceledExceptionThrown(
               () => mres.Wait(millisec, ct), ct, "CancelBeforeWait:  An OCE should have been thrown.");
            EnsureOperationCanceledExceptionThrown(
               () => mres.Wait(timeSpan, ct), ct, "CancelBeforeWait:  An OCE should have been thrown.");
            mres.Dispose();
        }

        [Fact]
        public static void CancelAfterWait()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            ManualResetEventSlim mres = new ManualResetEventSlim();

            Task.Run(
                () =>
                {
                    for (int i = 0; i < 400; i++) ;
                    cancellationTokenSource.Cancel();
                }
                );

            //Now wait.. the wait should abort and an exception should be thrown
            EnsureOperationCanceledExceptionThrown(
               () => mres.Wait(cancellationToken), cancellationToken,
               "CancelBeforeWait:  An OCE(null) should have been thrown that references the cancellationToken.");

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

