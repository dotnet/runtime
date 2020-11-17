// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static class BarrierCancellationTests
    {
        [Fact]
        public static void BarrierCancellationTestsCancelBeforeWait()
        {
            Barrier barrier = new Barrier(3);

            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            CancellationToken ct = cs.Token;

            const int millisec = 100;
            TimeSpan timeSpan = new TimeSpan(100);

            EnsureOperationCanceledExceptionThrown(
                () => barrier.SignalAndWait(ct), ct);
            EnsureOperationCanceledExceptionThrown(
                () => barrier.SignalAndWait(millisec, ct), ct);
            EnsureOperationCanceledExceptionThrown(
                () => barrier.SignalAndWait(timeSpan, ct), ct);

            barrier.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void BarrierCancellationTestsCancelAfterWait_Negative()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            const int numberParticipants = 3;
            Barrier barrier = new Barrier(numberParticipants);

            Task.Run(() => cancellationTokenSource.Cancel());

            //Now wait.. the wait should abort and an exception should be thrown
            EnsureOperationCanceledExceptionThrown(
               () => barrier.SignalAndWait(cancellationToken),
               cancellationToken);

            // the token should not have any listeners.
            // currently we don't expose this.. but it was verified manually
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void BarrierCancellationTestsCancelAfterWait()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            const int numberParticipants = 3;
            Barrier barrier = new Barrier(numberParticipants);

            Task.Run(() => cancellationTokenSource.Cancel());

            //Test that backout occurred.
            Assert.Equal(numberParticipants, barrier.ParticipantsRemaining);

            // the token should not have any listeners.
            // currently we don't expose this.. but it was verified manually
        }

        private static void EnsureOperationCanceledExceptionThrown(Action action, CancellationToken token)
        {
            OperationCanceledException operationCanceledEx =
                Assert.Throws<OperationCanceledException>(action);
            Assert.Equal(token, operationCanceledEx.CancellationToken);
        }
    }
}
