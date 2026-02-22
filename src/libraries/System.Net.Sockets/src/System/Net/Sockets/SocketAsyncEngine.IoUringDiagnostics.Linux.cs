// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
        /// <summary>Resets the native diagnostics poll countdown.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeLinuxIoUringDiagnosticsState() =>
            _ioUringDiagnosticsPollCountdown = IoUringDiagnosticsPollInterval;

        /// <summary>Publishes prepare queue depth delta to telemetry and resets the counter.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetIoUringPrepareQueueDepthTelemetry()
        {
            long publishedDepth = Interlocked.Exchange(ref _ioUringPublishedPrepareQueueLength, 0);
            if (publishedDepth != 0)
            {
                SocketsTelemetry.Log.IoUringPrepareQueueDepthDelta(-publishedDepth);
            }
        }

        /// <summary>Periodically polls native counters and publishes deltas to telemetry.</summary>
        private void PollIoUringDiagnosticsIfNeeded(bool force)
        {
            if (!_ioUringCapabilities.IsIoUringPort)
            {
                return;
            }

            if (!force)
            {
                int countdown = _ioUringDiagnosticsPollCountdown - 1;
                _ioUringDiagnosticsPollCountdown = countdown;
                if (countdown > 0)
                {
                    return;
                }
            }

            _ioUringDiagnosticsPollCountdown = IoUringDiagnosticsPollInterval;
            PublishIoUringManagedDiagnosticsDelta();

            if (!force)
            {
                EvaluateProvidedBufferRingResize();
            }
        }

        /// <summary>Returns the non-negative delta between two counter snapshots.</summary>
        private static long ComputeManagedCounterDelta(long previous, long current) =>
            current >= previous ? current - previous : current;

        /// <summary>Publishes a managed counter delta from source to published baseline.</summary>
        private static bool TryPublishManagedCounterDelta(
            ref long sourceCounter,
            ref long publishedCounter,
            out long delta,
            bool monotonic = true)
        {
            long current = Interlocked.Read(ref sourceCounter);
            long previous = Interlocked.Exchange(ref publishedCounter, current);
            delta = monotonic ? ComputeManagedCounterDelta(previous, current) : current - previous;
            return delta != 0;
        }

        /// <summary>Computes and publishes this engine's non-pinnable fallback counter delta.</summary>
        private bool TryPublishIoUringNonPinnablePrepareFallbackDelta(out long delta)
        {
            long current = Interlocked.Read(ref _ioUringNonPinnablePrepareFallbackCount);
            long previous = Interlocked.Exchange(ref _ioUringPublishedNonPinnablePrepareFallbackCount, current);
            delta = ComputeManagedCounterDelta(previous, current);
            return delta != 0;
        }

        /// <summary>Publishes all managed diagnostic counter deltas to telemetry.</summary>
        private void PublishIoUringManagedDiagnosticsDelta()
        {
            // Sample pending SEND_ZC NOTIF state directly from completion slots so
            // reset/teardown paths that bypass normal completion dispatch still publish accurate gauge data.
            SocketsTelemetry.Log.IoUringZeroCopyNotificationPendingSlots(CountZeroCopyNotificationPendingSlots());
            if (TryPublishManagedCounterDelta(
                ref _ioUringCompletionRequeueFailureCount,
                ref _ioUringPublishedCompletionRequeueFailureCount,
                out long requeueFailureDelta))
            {
                SocketsTelemetry.Log.IoUringCompletionRequeueFailure(requeueFailureDelta);
            }

            if (TryPublishIoUringNonPinnablePrepareFallbackDelta(out long nonPinnableFallbackDelta))
            {
                SocketsTelemetry.Log.IoUringPrepareNonPinnableFallback(nonPinnableFallbackDelta);
            }

            if (TryPublishManagedCounterDelta(
                ref _ioUringPrepareQueueOverflowCount,
                ref _ioUringPublishedPrepareQueueOverflowCount,
                out long prepareQueueOverflowDelta))
            {
                SocketsTelemetry.Log.IoUringPrepareQueueOverflow(prepareQueueOverflowDelta);
            }

            if (TryPublishManagedCounterDelta(
                ref _ioUringPrepareQueueOverflowFallbackCount,
                ref _ioUringPublishedPrepareQueueOverflowFallbackCount,
                out long prepareQueueOverflowFallbackDelta))
            {
                SocketsTelemetry.Log.IoUringPrepareQueueOverflowFallback(prepareQueueOverflowFallbackDelta);
            }

            if (TryPublishManagedCounterDelta(
                ref _ioUringPrepareQueueLength,
                ref _ioUringPublishedPrepareQueueLength,
                out long prepareQueueDepthDelta,
                monotonic: false))
            {
                SocketsTelemetry.Log.IoUringPrepareQueueDepthDelta(prepareQueueDepthDelta);
            }

            if (TryPublishManagedCounterDelta(
                ref _ioUringCompletionSlotExhaustionCount,
                ref _ioUringPublishedCompletionSlotExhaustionCount,
                out long completionSlotExhaustionDelta))
            {
                SocketsTelemetry.Log.IoUringCompletionSlotExhaustion(completionSlotExhaustionDelta);
            }

            if (TryPublishManagedCounterDelta(
                ref _ioUringCompletionSlotDrainRecoveryCount,
                ref _ioUringPublishedCompletionSlotDrainRecoveryCount,
                out long completionSlotDrainRecoveryDelta))
            {
                SocketsTelemetry.Log.IoUringCompletionSlotDrainRecovery(completionSlotDrainRecoveryDelta);
            }
        }

        /// <summary>Counts completion slots currently waiting for SEND_ZC NOTIF CQEs.</summary>
        private int CountZeroCopyNotificationPendingSlots()
        {
            IoUringCompletionSlot[]? completionEntries = _completionSlots;
            if (completionEntries is null)
            {
                return 0;
            }

            int pendingNotificationSlots = 0;
            for (int i = 0; i < completionEntries.Length; i++)
            {
                ref IoUringCompletionSlot slot = ref completionEntries[i];
                if (slot.IsZeroCopySend && slot.ZeroCopyNotificationPending)
                {
                    pendingNotificationSlots++;
                }
            }

            return pendingNotificationSlots;
        }
    }
}
