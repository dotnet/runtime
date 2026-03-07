// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
        private readonly partial struct SocketEventHandler
        {
            private enum EarlyBufferFailureReason : byte
            {
                None = 0,
                MissingBufferFlag,
                ProvidedRingUnavailable,
                AcquireBufferFailed,
                BufferQueueRejected,
                ResultExceedsBuffer,
                RecycleFailed,
            }

            /// <summary>Delivers a completed operation to its owning socket context.</summary>
            private void DispatchCompletedIoUringOperation(SocketAsyncContext.AsyncOperation operation)
            {
                operation.AssociatedContext.TryCompleteIoUringOperation(operation);
            }

            /// <summary>Completes a deferred SEND_ZC operation when its NOTIF CQE arrives.</summary>
            public void DispatchZeroCopyIoUringNotification(ulong payload)
            {
                ulong userData = EncodeIoUringUserData(IoUringConstants.TagReservedCompletion, payload);
                if (!_engine.TryTakeTrackedIoUringOperation(userData, out SocketAsyncContext.AsyncOperation? operation) || operation is null)
                {
                    return;
                }

                Debug.Assert(
                    !_engine.IsZeroCopyNotificationPending(userData),
                    "NOTIF CQE dispatch must occur only after clearing SEND_ZC pending slot state.");
                Debug.Assert(
                    operation.IoUringUserData == userData,
                    "Deferred SEND_ZC operation must still be tracked with its original user_data at NOTIF dispatch.");
                AssertIoUringLifecycleTransition(
                    IoUringOperationLifecycleState.Submitted,
                    IoUringOperationLifecycleState.Completed);
                operation.ClearIoUringUserData();
                DispatchCompletedIoUringOperation(operation);
            }

            /// <summary>Processes a single completion and dispatches it to its owning operation.</summary>
            public void DispatchSingleIoUringCompletion(
                ulong userData,
                int result,
                uint flags,
                int socketAddressLen,
                int controlBufferLen,
                uint auxiliaryData,
                bool hasFixedRecvBuffer,
                ushort fixedRecvBufferId,
                ref bool enqueuedFallbackEvent)
            {
                Debug.Assert(_engine.IsCurrentThreadEventLoopThread(),
                    "DispatchSingleIoUringCompletion must only run on the event-loop thread.");
                if (userData == 0)
                {
                    RecycleUntrackedReceiveCompletionBuffers(flags, hasFixedRecvBuffer, fixedRecvBufferId);
                    return;
                }

                // Benign race: cancellation/abort paths may have already removed this tracked entry.
                if (!_engine.TryTakeTrackedIoUringOperation(userData, out SocketAsyncContext.AsyncOperation? operation))
                {
                    RecycleUntrackedReceiveCompletionBuffers(flags, hasFixedRecvBuffer, fixedRecvBufferId);
                    return;
                }

                if (operation is null)
                {
                    RecycleUntrackedReceiveCompletionBuffers(flags, hasFixedRecvBuffer, fixedRecvBufferId);
                    return;
                }

                SocketAsyncContext receiveContext = operation.AssociatedContext;
                if (receiveContext.IsPersistentMultishotRecvArmed() &&
                    receiveContext.PersistentMultishotRecvUserData == userData)
                {
                    // Terminal CQE for persistent multishot recv: clear armed-state so the
                    // next receive can re-arm.
                    receiveContext.ClearPersistentMultishotRecvArmed();

                    // If a new operation piggybacked on this multishot via TryReplace, its
                    // IoUringUserData was set to the multishot's userData. Let it through so
                    // ProcessIoUringCompletionResult delivers the terminal error and the
                    // operation can retry with a new SQE. If the operation was recycled
                    // (IoUringUserData cleared in DispatchMultishotIoUringCompletion),
                    // discard to prevent corrupting the recycled operation's state.
                    if (operation.IoUringUserData != userData)
                    {
                        RecycleUntrackedReceiveCompletionBuffers(flags, hasFixedRecvBuffer, fixedRecvBufferId);
                        return;
                    }
                }

                if (operation is SocketAsyncContext.AcceptOperation acceptOperation)
                {
                    SocketAsyncContext acceptContext = acceptOperation.AssociatedContext;
                    if (acceptContext.MultishotAcceptUserData == userData)
                    {
                        acceptContext.DisarmMultishotAccept();
                    }
                    else if (operation.IoUringUserData != userData)
                    {
                        // The multishot accept was already disarmed and completed by
                        // DispatchMultishotAcceptIoUringCompletion (which cleared IoUringUserData).
                        // This is the terminal ECANCELED CQE for a recycled operation — discard.
                        RecycleUntrackedReceiveCompletionBuffers(flags, hasFixedRecvBuffer, fixedRecvBufferId);
                        return;
                    }
                }

                uint completionAuxiliaryData = auxiliaryData;
                int completionResultCode = result;
                if (!TryMaterializeIoUringReceiveCompletion(
                        operation!,
                        completionResultCode,
                        flags,
                        hasFixedRecvBuffer,
                        fixedRecvBufferId,
                        ref completionAuxiliaryData))
                {
                    completionResultCode = -Interop.Sys.ConvertErrorPalToPlatform(Interop.Error.ENOBUFS);
                    completionAuxiliaryData = 0;
                }

                // Process completion metadata before processing result to allow message post-processing.
                operation!.SetIoUringCompletionMessageMetadata(socketAddressLen, controlBufferLen);
                SocketAsyncContext.AsyncOperation.IoUringCompletionResult completionDispatchResult =
                    operation.ProcessIoUringCompletionResult(completionResultCode, flags, completionAuxiliaryData);
                if (completionDispatchResult == SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Completed &&
                    _engine.IsZeroCopyNotificationPending(userData))
                {
                    // SEND_ZC API contract: complete managed operation only once NOTIF confirms
                    // the kernel/NIC no longer references the caller buffer.
                    _engine.AssertZeroCopyDeferredCompletionState(userData, operation);
                    if (!_engine.TryReattachTrackedIoUringOperation(userData, operation))
                    {
                        Debug.Fail("SEND_ZC deferred completion reattach failed; completing operation with EINVAL and releasing deferred slot.");
                        bool cleanedDeferredSlot = _engine.TryCleanupDeferredZeroCopyCompletionSlot(userData);
                        Debug.Assert(
                            cleanedDeferredSlot,
                            "SEND_ZC deferred completion reattach failure should release the deferred completion slot.");
                        operation.ErrorCode = SocketPal.GetSocketErrorForErrorCode(Interop.Error.EINVAL);
                        AssertIoUringLifecycleTransition(
                            IoUringOperationLifecycleState.Submitted,
                            IoUringOperationLifecycleState.Completed);
                        operation.ClearIoUringUserData();
                        DispatchCompletedIoUringOperation(operation);
                        return;
                    }

                    return;
                }

                DispatchIoUringCompletionResult(
                    operation,
                    completionDispatchResult,
                    ref enqueuedFallbackEvent);
            }

            /// <summary>
            /// Processes a multishot completion by completing the current operation and
            /// preserving persistent multishot ownership unless terminal completion requires disarm.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void DispatchMultishotIoUringCompletion(
                ulong userData,
                int result,
                uint flags,
                int socketAddressLen,
                int controlBufferLen,
                uint auxiliaryData,
                bool hasFixedRecvBuffer,
                ushort fixedRecvBufferId,
                ref bool enqueuedFallbackEvent)
            {
                Debug.Assert(_engine.IsCurrentThreadEventLoopThread(),
                    "DispatchMultishotIoUringCompletion must only run on the event-loop thread.");
                _ = enqueuedFallbackEvent; // Transitional path never requeues via readiness fallback.
                _ = hasFixedRecvBuffer;
                _ = fixedRecvBufferId;
                Debug.Assert((flags & IoUringConstants.CqeFMore) != 0,
                    "Multishot dispatch must only be used for non-terminal CQEs (IORING_CQE_F_MORE).");

                if (userData == 0)
                {
                    RecycleUntrackedReceiveCompletionBuffers(flags, hasFixedRecvBuffer: false, fixedRecvBufferId: 0);
                    return;
                }

                if (!_engine.TryGetTrackedIoUringOperation(userData, out SocketAsyncContext.AsyncOperation? operation) || operation is null)
                {
                    RecycleUntrackedReceiveCompletionBuffers(flags, hasFixedRecvBuffer: false, fixedRecvBufferId: 0);
                    return;
                }

                if (operation is SocketAsyncContext.AcceptOperation acceptOperation)
                {
                    DispatchMultishotAcceptIoUringCompletion(
                        acceptOperation,
                        userData,
                        result,
                        flags,
                        socketAddressLen,
                        auxiliaryData);
                    return;
                }

                // Guard against ThreadPool recycling: when a persistent multishot recv
                // completion is dispatched (QueueIoUringCompletionCallback), the ThreadPool
                // may recycle the operation (reset state to Waiting) before the event loop
                // finishes draining the CQE batch. IoUringUserData is zeroed on the event-loop
                // thread at completion (line 339) and only restored during prepare-queue drain
                // (after CQE processing), so IoUringUserData==0 reliably detects a completed-
                // but-not-yet-retracked operation regardless of ThreadPool-driven state changes.
                if (operation.IoUringUserData == 0 || !operation.IsInWaitingState())
                {
                    if (result <= 0)
                    {
                        // Terminal/error shots observed without a waiting managed receiver must
                        // still drive cancel/disarm so the tracked multishot slot cannot stall.
                        _engine.TryRequestIoUringCancellation(userData);
                        return;
                    }

                    SocketAsyncContext opContext = operation.AssociatedContext;
                    if (!TryBufferEarlyPersistentMultishotRecvCompletion(opContext, result, flags, out EarlyBufferFailureReason bufferFailureReason))
                    {
                        if (ShouldCancelPersistentMultishotAfterEarlyBufferFailure(bufferFailureReason))
                        {
                            _engine.TryRequestIoUringCancellation(userData);
                        }
                    }

                    return;
                }

                SocketAsyncContext context = operation.AssociatedContext;
                bool isPersistentMultishotRecv =
                    context.IsPersistentMultishotRecvArmed() &&
                    context.PersistentMultishotRecvUserData == userData;
                uint completionAuxiliaryData = auxiliaryData;
                int completionResultCode = result;
                if (!TryMaterializeIoUringReceiveCompletion(
                        operation,
                        completionResultCode,
                        flags,
                        hasFixedRecvBuffer: false,
                        fixedRecvBufferId: 0,
                        ref completionAuxiliaryData))
                {
                    if (isPersistentMultishotRecv && completionResultCode > 0)
                    {
                        // Under transient provided-buffer pressure, drop this shot and keep the
                        // persistent multishot request armed instead of surfacing ENOBUFS.
                        return;
                    }

                    completionResultCode = -Interop.Sys.ConvertErrorPalToPlatform(Interop.Error.ENOBUFS);
                    completionAuxiliaryData = 0;
                }

                operation.SetIoUringCompletionMessageMetadata(socketAddressLen, controlBufferLen);
                SocketAsyncContext.AsyncOperation.IoUringCompletionResult completionDispatchResult =
                    operation.ProcessIoUringCompletionResult(completionResultCode, flags, completionAuxiliaryData);
                bool shouldCancelPersistentMultishotRecv =
                    isPersistentMultishotRecv &&
                    completionDispatchResult == SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Completed &&
                    completionResultCode <= 0;

                if (!isPersistentMultishotRecv || shouldCancelPersistentMultishotRecv)
                {
                    _engine.TryRequestIoUringCancellation(userData);
                }

                switch (completionDispatchResult)
                {
                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Completed:
                        if (isPersistentMultishotRecv)
                        {
                            // Zero only IoUringUserData (not the full ClearIoUringUserData which
                            // wipes completion metadata needed by the callback). The terminal CQE
                            // uses this to distinguish recycled (userData=0) from piggybacked
                            // (userData=armedUserData) operations.
                            operation.IoUringUserData = 0;
                        }

                        DispatchCompletedIoUringOperation(operation);
                        break;

                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Pending:
                        // Persistent multishot receives stay armed; intermediate shots are
                        // delivered through completion-mode dispatch without readiness fallback.
                        break;

                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Canceled:
                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Ignored:
                        break;

                    default:
                        Debug.Fail($"Unexpected io_uring multishot completion result: {completionDispatchResult}");
                        break;
                }
            }

            /// <summary>
            /// Handles transitional multishot-accept CQEs by completing one waiting operation and
            /// canceling the multishot request. Extra successful shots are queued for dequeue on
            /// the accept operation queue when possible.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void DispatchMultishotAcceptIoUringCompletion(
                SocketAsyncContext.AcceptOperation operation,
                ulong userData,
                int result,
                uint flags,
                int socketAddressLen,
                uint auxiliaryData)
            {
                Debug.Assert(_engine.IsCurrentThreadEventLoopThread(),
                    "DispatchMultishotAcceptIoUringCompletion must only run on the event-loop thread.");
                operation.SetIoUringCompletionMessageMetadata(socketAddressLen, 0);
                SocketAsyncContext context = operation.AssociatedContext;

                if (result >= 0 && s_fdEngineAffinity is not null)
                    SetFdEngineAffinity(result, _engine.EngineIndex);

                SocketAsyncContext.AsyncOperation.IoUringCompletionResult completionDispatchResult =
                    operation.ProcessIoUringCompletionResult(result, flags, auxiliaryData);

                bool hasMoreShots = (flags & IoUringConstants.CqeFMore) != 0;
                bool shouldCancelMultishotAccept =
                    completionDispatchResult == SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Completed ||
                    result < 0 ||
                    !hasMoreShots;
                if (shouldCancelMultishotAccept)
                {
                    _engine.TryRequestIoUringCancellation(userData);
                }

                switch (completionDispatchResult)
                {
                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Completed:
                        // Disarm multishot state and tag the operation before dispatching.
                        // Zero only IoUringUserData (not the full ClearIoUringUserData which
                        // wipes completion metadata needed by the callback). The terminal
                        // ECANCELED CQE uses IoUringUserData to distinguish recycled (userData=0)
                        // from piggybacked (userData=armedUserData) operations.
                        context.DisarmMultishotAccept();
                        operation.IoUringUserData = 0;
                        DispatchCompletedIoUringOperation(operation);
                        break;

                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Pending:
                        break;

                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Canceled:
                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Ignored:
                        if (result >= 0)
                        {
                            int addressLength = auxiliaryData > (uint)operation.SocketAddress.Length ?
                                operation.SocketAddress.Length :
                                (int)auxiliaryData;
                            if (context.TryEnqueuePreAcceptedConnection((IntPtr)result, operation.SocketAddress.Span, addressLength))
                            {
                                _engine.EnqueueReadinessFallbackEvent(context, Interop.Sys.SocketEvents.Read);
                            }
                            else
                            {
                                CloseAcceptedFd(result);
                            }
                        }
                        break;

                    default:
                        Debug.Fail($"Unexpected io_uring multishot accept completion result: {completionDispatchResult}");
                        break;
                }
            }

            /// <summary>
            /// Dispatches a CQE from a SO_REUSEPORT shadow listener's multishot accept.
            /// Shadow listeners have no pending AcceptAsync operations — accepted fds are
            /// forwarded directly to the primary listener's pre-accept queue.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void DispatchReusePortAcceptIoUringCompletion(
                ulong userData,
                int result,
                uint flags,
                int socketAddressLen,
                uint auxiliaryData)
            {
                _ = flags;
                // Reuse-port multishot accept SQEs are intentionally armed without sockaddr writeback
                // (shared-address writeback is race-prone for multishot batches). Enqueued accepted
                // sockets carry no peer address metadata here; endpoint resolution is deferred.
                _ = socketAddressLen;
                _ = auxiliaryData;
                ulong payload = userData & IoUringUserDataPayloadMask;
                int slotIndex = DecodeCompletionSlotIndex(payload);
                IoUringCompletionSlotStorage[]? storageArray = _engine._completionSlotStorage;
                if (storageArray is null || (uint)slotIndex >= (uint)storageArray.Length)
                {
                    // Stale or invalid slot; close any accepted fd to prevent leak.
                    if (result >= 0)
                    {
                        CloseAcceptedFd(result);
                    }
                    return;
                }

                ref IoUringCompletionSlotStorage slotStorage = ref storageArray[slotIndex];
                SocketAsyncContext? primaryContext = slotStorage.ReusePortPrimaryContext;
                SocketAsyncEngine? primaryEngine = slotStorage.ReusePortPrimaryEngine;

                if (result < 0)
                {
                    // Error CQE — nothing to enqueue. If this is a terminal CQE (no MORE flag),
                    // the slot will be freed by the caller after we return.
                    return;
                }

                // Successful accept: forward the fd to the primary listener's pre-accept queue.
                SetFdEngineAffinity(result, _engine.EngineIndex);
                if (primaryContext is not null && primaryEngine is not null)
                {
                    if (primaryContext.TryEnqueuePreAcceptedConnection((IntPtr)result, ReadOnlySpan<byte>.Empty, 0))
                    {
                        primaryEngine.EnqueueReadinessFallbackEvent(primaryContext, Interop.Sys.SocketEvents.Read);
                    }
                    else
                    {
                        CloseAcceptedFd(result);
                    }
                }
                else
                {
                    // Primary context/engine not set — orphaned slot; close the fd.
                    CloseAcceptedFd(result);
                }
            }

            /// <summary>
            /// For receive completions that used provided buffers (buffer-select or fixed receive),
            /// materializes payload bytes into the operation target and recycles checked-out buffers.
            /// </summary>
            private unsafe bool TryMaterializeIoUringReceiveCompletion(
                SocketAsyncContext.AsyncOperation operation,
                int result,
                uint flags,
                bool hasFixedRecvBuffer,
                ushort fixedRecvBufferId,
                ref uint auxiliaryData)
            {
                bool hasSelectedBuffer = (flags & IoUringConstants.CqeFBuffer) != 0;
                if (!hasFixedRecvBuffer && !hasSelectedBuffer)
                {
                    return true;
                }

                IoUringProvidedBufferRing? providedBufferRing = _engine._ioUringProvidedBufferRing;
                if (providedBufferRing is null)
                {
                    return false;
                }

                ushort bufferId;
                bool reportRecycleFailureAsDepletion;
                byte* providedBuffer = null;
                int providedBufferLength = 0;
                if (hasFixedRecvBuffer)
                {
                    bufferId = fixedRecvBufferId;
                    reportRecycleFailureAsDepletion = true;

                    if (result > 0 &&
                        !providedBufferRing.TryGetCheckedOutBuffer(
                            bufferId,
                            out providedBuffer,
                            out providedBufferLength))
                    {
                        _engine.RecordIoUringProvidedBufferDepletionForDrainBatch();
                        return false;
                    }
                }
                else
                {
                    bufferId = (ushort)(flags >> IoUringConstants.CqeBufferShift);
                    reportRecycleFailureAsDepletion = false;
                    if (!providedBufferRing.TryAcquireBufferForCompletion(
                            bufferId,
                            out providedBuffer,
                            out providedBufferLength))
                    {
                        _engine.RecordIoUringProvidedBufferDepletionForDrainBatch();
                        return false;
                    }
                }

                bool handled = result <= 0;
                try
                {
                    if (result > 0)
                    {
                        handled =
                        operation.TryProcessIoUringProvidedBufferCompletion(
                            providedBuffer,
                            providedBufferLength,
                            result,
                            ref auxiliaryData);
                    }

                    RecordProvidedBufferUtilizationIfEnabled(providedBufferRing, result);
                }
                finally
                {
                    handled &= TryRecycleProvidedBufferFromCheckedOutState(
                        providedBufferRing,
                        bufferId,
                        reportFailureAsDepletion: reportRecycleFailureAsDepletion);
                }

                return handled;
            }

            /// <summary>
            /// For persistent multishot recv, buffers payload bytes that arrive while no
            /// managed receive operation is in the Waiting state.
            /// </summary>
            private unsafe bool TryBufferEarlyPersistentMultishotRecvCompletion(
                SocketAsyncContext context,
                int result,
                uint flags,
                out EarlyBufferFailureReason failureReason)
            {
                failureReason = EarlyBufferFailureReason.None;
                Debug.Assert(result > 0, $"Expected positive result for early-buffered multishot recv, got {result}");

                if ((flags & IoUringConstants.CqeFBuffer) == 0)
                {
                    failureReason = EarlyBufferFailureReason.MissingBufferFlag;
                    return false;
                }

                IoUringProvidedBufferRing? providedBufferRing = _engine._ioUringProvidedBufferRing;
                if (providedBufferRing is null)
                {
                    failureReason = EarlyBufferFailureReason.ProvidedRingUnavailable;
                    return false;
                }

                ushort bufferId = (ushort)(flags >> IoUringConstants.CqeBufferShift);
                if (!providedBufferRing.TryAcquireBufferForCompletion(
                        bufferId,
                        out byte* providedBuffer,
                        out int providedBufferLength))
                {
                    _engine.RecordIoUringProvidedBufferDepletionForDrainBatch();
                    failureReason = EarlyBufferFailureReason.AcquireBufferFailed;
                    return false;
                }

                bool buffered = false;
                try
                {
                    if ((uint)result <= (uint)providedBufferLength)
                    {
                        buffered = context.TryBufferEarlyPersistentMultishotRecvData(
                            new ReadOnlySpan<byte>(providedBuffer, result));
                        if (buffered)
                        {
                            RecordProvidedBufferUtilizationIfEnabled(providedBufferRing, result);
                        }
                        else
                        {
                            failureReason = EarlyBufferFailureReason.BufferQueueRejected;
                        }
                    }
                    else
                    {
                        failureReason = EarlyBufferFailureReason.ResultExceedsBuffer;
                    }
                }
                finally
                {
                    buffered &= TryRecycleProvidedBufferFromCheckedOutState(
                        providedBufferRing,
                        bufferId,
                        reportFailureAsDepletion: false);
                }

                if (!buffered && failureReason == EarlyBufferFailureReason.None)
                {
                    failureReason = EarlyBufferFailureReason.RecycleFailed;
                }

                return buffered;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool ShouldCancelPersistentMultishotAfterEarlyBufferFailure(EarlyBufferFailureReason failureReason) =>
                failureReason switch
                {
                    // Transient pressure: keep multishot armed and drop this shot.
                    EarlyBufferFailureReason.AcquireBufferFailed => false,
                    // Back-pressure: cancel multishot when the early-buffer queue is full to prevent
                    // further data loss. The multishot will be re-armed when the next recv operation
                    // drains the buffer and submits a fresh SQE.
                    EarlyBufferFailureReason.BufferQueueRejected => true,
                    _ => true,
                };

            /// <summary>
            /// Recycles a provided-buffer selection for completions that can no longer be
            /// dispatched to a tracked operation (e.g., late multishot CQEs after cancel).
            /// </summary>
            private unsafe void RecycleUntrackedReceiveCompletionBuffers(
                uint flags,
                bool hasFixedRecvBuffer,
                ushort fixedRecvBufferId)
            {
                IoUringProvidedBufferRing? providedBufferRing = _engine._ioUringProvidedBufferRing;
                if (providedBufferRing is null)
                {
                    return;
                }

                if ((flags & IoUringConstants.CqeFBuffer) == 0)
                {
                    if (hasFixedRecvBuffer)
                    {
                        _ = TryRecycleProvidedBufferFromCheckedOutState(
                            providedBufferRing,
                            fixedRecvBufferId,
                            reportFailureAsDepletion: true);
                    }

                    return;
                }

                ushort bufferId = (ushort)(flags >> IoUringConstants.CqeBufferShift);
                if (!providedBufferRing.TryAcquireBufferForCompletion(
                        bufferId,
                        out _,
                        out _))
                {
                    _engine.RecordIoUringProvidedBufferDepletionForDrainBatch();
                }
                else
                {
                    _ = TryRecycleProvidedBufferFromCheckedOutState(
                        providedBufferRing,
                        bufferId,
                        reportFailureAsDepletion: false);
                }

                if (hasFixedRecvBuffer)
                {
                    _ = TryRecycleProvidedBufferFromCheckedOutState(
                        providedBufferRing,
                        fixedRecvBufferId,
                        reportFailureAsDepletion: true);
                }
            }

            private void RecordProvidedBufferUtilizationIfEnabled(
                IoUringProvidedBufferRing providedBufferRing,
                int bytesTransferred)
            {
                if (bytesTransferred <= 0 || !_engine._adaptiveBufferSizingEnabled)
                {
                    return;
                }

                Debug.Assert(_engine.IsCurrentThreadEventLoopThread(),
                    "Adaptive provided-buffer utilization tracking must run on the event-loop thread.");
                providedBufferRing.RecordCompletionUtilization(bytesTransferred);
            }

            private bool TryRecycleProvidedBufferFromCheckedOutState(
                IoUringProvidedBufferRing providedBufferRing,
                ushort bufferId,
                bool reportFailureAsDepletion)
            {
                bool recycled = providedBufferRing.TryRecycleBufferFromCompletion(bufferId);
                if (!recycled && reportFailureAsDepletion)
                {
                    _engine.RecordIoUringProvidedBufferDepletionForDrainBatch();
                }

                return recycled;
            }

            /// <summary>Requeues a pending operation or falls back to readiness notification.</summary>
            private bool DispatchPendingIoUringOperation(SocketAsyncContext.AsyncOperation operation)
            {
                PendingIoUringReprepareResult inlineReprepareResult = TryDispatchPendingIoUringOperationInline(operation);
                if (inlineReprepareResult == PendingIoUringReprepareResult.Prepared)
                {
                    return false;
                }

                if (inlineReprepareResult == PendingIoUringReprepareResult.NotAttempted &&
                    operation.TryQueueIoUringPreparation())
                {
                    _engine._ioUringPendingRetryQueuedToPrepareQueueCount++;
                    return false;
                }

                Debug.Assert(
                    inlineReprepareResult == PendingIoUringReprepareResult.Failed ||
                    !_engine._ioUringCapabilities.IsCompletionMode,
                    "Requeue should not fail in pure io_uring completion mode when inline re-prepare was not attempted.");

                operation.ClearIoUringUserData();
                Interop.Sys.SocketEvents fallbackEvents = operation.GetIoUringFallbackSocketEvents();
                if (fallbackEvents == Interop.Sys.SocketEvents.None)
                {
                    return false;
                }

                _eventQueue.Enqueue(new SocketIOEvent(operation.AssociatedContext, fallbackEvents));
                return true;
            }

            /// <summary>
            /// Attempts to re-prepare and re-track a pending operation inline on the event loop thread.
            /// This avoids an extra prepare-queue round-trip for completion-mode retries.
            /// </summary>
            private enum PendingIoUringReprepareResult : byte
            {
                NotAttempted = 0,
                Prepared = 1,
                Failed = 2
            }

            /// <summary>
            /// Attempts to re-prepare a pending operation inline.
            /// Returns whether inline re-prepare was prepared, skipped, or failed without producing an SQE.
            /// </summary>
            private PendingIoUringReprepareResult TryDispatchPendingIoUringOperationInline(SocketAsyncContext.AsyncOperation operation)
            {
                if (!_engine._ioUringCapabilities.IsCompletionMode || !_engine.IsCurrentThreadEventLoopThread())
                {
                    return PendingIoUringReprepareResult.NotAttempted;
                }

                long prepareSequence = operation.MarkReadyForIoUringPreparation();
                Interop.Error prepareError = _engine.TryPrepareAndTrackIoUringOperation(
                    operation,
                    prepareSequence,
                    out bool preparedSqe);
                if (prepareError != Interop.Error.SUCCESS)
                {
                    Debug.Fail($"io_uring inline re-prepare failed: {prepareError}");

                    return PendingIoUringReprepareResult.Failed;
                }

                return preparedSqe ? PendingIoUringReprepareResult.Prepared : PendingIoUringReprepareResult.Failed;
            }

            /// <summary>Routes a CQE completion result to the appropriate dispatch behavior.</summary>
            private void DispatchIoUringCompletionResult(
                SocketAsyncContext.AsyncOperation operation,
                SocketAsyncContext.AsyncOperation.IoUringCompletionResult completionResult,
                ref bool enqueuedFallbackEvent)
            {
                switch (completionResult)
                {
                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Completed:
                        AssertIoUringLifecycleTransition(
                            IoUringOperationLifecycleState.Submitted,
                            IoUringOperationLifecycleState.Completed);
                        operation.ClearIoUringUserData();
                        DispatchCompletedIoUringOperation(operation);
                        break;

                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Pending:
                        AssertIoUringLifecycleTransition(
                            IoUringOperationLifecycleState.Submitted,
                            IoUringOperationLifecycleState.Queued);
                        if (operation.ShouldReuseIoUringPreparationResourcesOnPending)
                        {
                            operation.MarkIoUringPreparationReusable();
                            operation.ResetIoUringUserDataForRequeue();
                        }
                        else
                        {
                            operation.ClearIoUringUserData();
                        }

                        enqueuedFallbackEvent |= DispatchPendingIoUringOperation(operation);
                        break;

                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Canceled:
                    case SocketAsyncContext.AsyncOperation.IoUringCompletionResult.Ignored:
                        AssertIoUringLifecycleTransition(
                            IoUringOperationLifecycleState.Submitted,
                            IoUringOperationLifecycleState.Canceled);
                        operation.ClearIoUringUserData();
                        break;

                    default:
                        Debug.Fail($"Unexpected io_uring completion result: {completionResult}");
                        AssertIoUringLifecycleTransition(
                            IoUringOperationLifecycleState.Submitted,
                            IoUringOperationLifecycleState.Detached);
                        operation.ClearIoUringUserData();
                        break;
                }
            }
        }
    }
}
