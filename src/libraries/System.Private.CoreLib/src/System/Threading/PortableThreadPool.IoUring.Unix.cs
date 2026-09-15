// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        /// <summary>
        /// Implements the "Option 3" io_uring/ThreadPool integration described in the io_uring design doc:
        /// a single, plain (non-<c>SINGLE_ISSUER</c>) ring is shared by every Thread Pool worker thread.
        /// Submission is guarded by an ordinary lock. The role of "the thread currently reaping
        /// completions" rotates: any worker thread that is about to park first attempts a non-blocking
        /// CAS on a single "driver" slot. The thread that wins blocks in <c>io_uring_enter</c> waiting for
        /// at least one completion, then (without running any continuation inline) queues the
        /// corresponding continuations as ordinary Thread Pool work items. At any given moment, at most
        /// one thread is ever reaping completions - the CAS guarantees this remains a single-driver
        /// design even under high submission concurrency.
        /// </summary>
        internal static class IoUringThreadPool
        {
            // Depth of the shared submission/completion queues. Not currently configurable; may become
            // adaptive (or sharded across multiple rings) in a future iteration.
            private const int QueueDepth = 1024;

            // Maximum number of completions fetched per IoRingWaitForCompletions call. Batching here
            // means a driver that wakes up to many simultaneously-ready completions (e.g. under high
            // concurrency) drains all of them via a single syscall instead of one syscall per
            // completion.
            private const int MaxCompletionsPerWait = 64;

            // Scratch buffer used by the driver to collect the work items returned from a batch of
            // completions before queuing them all via a single ThreadPool.UnsafeQueueUserWorkItems call.
            // Reused across calls: only ever accessed by the single thread currently holding the
            // s_isDriving CAS (see TryBecomeDriverAndDrive/DispatchBatch), so no synchronization is
            // needed for this array itself.
            private static readonly IThreadPoolWorkItem[] s_workItemBatch = new IThreadPoolWorkItem[MaxCompletionsPerWait];

            // Opt-out switch: io_uring integration is used by default on Linux when the kernel supports
            // it. Set DOTNET_USE_IO_URING=0 to fall back to the pre-existing (blocking-call-on-a-
            // ThreadPool-work-item) implementation unconditionally.
            private static readonly bool s_isEnabled;

            // The shared ring handle, or IntPtr.Zero if unavailable/disabled. Set at most once.
            private static readonly IntPtr s_ringHandle;

            // Guards calls to Interop.Sys.IoRingSubmit (which fills SQEs and publishes the SQ tail,
            // but does not call io_uring_enter) in TrySubmit. This mirrors liburing's own documented
            // thread-safety contract: io_uring_get_sqe/io_uring_submit touch this ring's local
            // (non-atomic) submission-queue bookkeeping and are not safe to call concurrently from
            // multiple threads without external synchronization, so all submitting threads must be
            // serialized through this lock for that step. The actual io_uring_enter(2) syscall
            // (Interop.Sys.IoRingKick) is safe to call concurrently and is deliberately called *after*
            // releasing this lock, so the (relatively expensive) syscall never serializes concurrent
            // submitters. The CQ ring needs no lock at all: exclusive access to it is guaranteed by the
            // s_isDriving CAS below (only the elected driver ever reads completions), and the SQ/CQ
            // rings are independent ring buffers, so the two don't contend with each other.
            private static readonly Lock s_lock = new Lock();

            // CAS slot: 0 == no one is currently driving completions, 1 == a driver is active. At most
            // one thread ever holds this at a time - completions are always reaped by a single thread.
            private static int s_isDriving;

            // Opportunistic batching for the post-lock IoRingKick call: incremented before a thread
            // attempts to submit, decremented after it finishes (successfully or not). When multiple
            // threads submit around the same time, only the last one to finish (the decrement that
            // observes the counter back at 0) actually calls IoRingKick; the others skip it, since their
            // SQEs will be picked up by that same kick. The kick is issued whenever the counter reaches 0
            // - not only when *this* thread's own submission succeeded - because an Interlocked.Decrement
            // only ever executes after that thread's own submit-or-fail attempt has fully completed, so
            // observing 0 guarantees every submission in the current "wave" (successful or not) has
            // already been durably published to the SQ ring. Gating the kick on this thread's own
            // success instead would be unsound: a successful submitter that isn't the last one out could
            // have its SQE left un-kicked forever if the actual last-out thread's own submission failed
            // (e.g., the queue was momentarily full) - io_uring_wait_cqe does not submit pending SQEs on
            // its own, so a never-kicked SQE's completion would never arrive, hanging that operation
            // indefinitely.
            private static int s_submittersInFlight;

            // Number of io_uring operations submitted but not yet completed. Used to avoid a thread
            // blocking forever in io_uring_enter when there is nothing outstanding to wait for.
            private static int s_inFlightCount;

            // Gates the "wake a worker so it can become the driver" call in TrySubmit (see its comment)
            // to at most once per "no driver currently active" window, instead of once per submitting
            // thread. Without this, a burst of concurrent TrySubmit calls that all observe s_isDriving
            // == 0 (e.g. under high concurrency, before any of them has had a chance to actually win the
            // CAS and start driving) would each separately call MaybeAddWorkingWorker, which can wake (or
            // even create) multiple worker threads even though only one of them will ever succeed in
            // becoming the driver - the rest just burn a wake/park cycle for nothing. Reset back to 0 as
            // soon as any worker thread visits TryBecomeDriverAndDrive while an operation is in flight
            // (whether or not that thread goes on to win the s_isDriving CAS), so the gate can never get
            // stuck at 1 - the next round of submissions remains free to request a fresh wake if needed.
            private static int s_driverWakeRequested;

            static IoUringThreadPool()
            {
                (s_isEnabled, s_ringHandle) = DetermineIsEnabledAndCreateRing();
            }

            /// <summary>Whether the io_uring Thread Pool integration is enabled and usable on this system.</summary>
            public static bool IsEnabled => s_isEnabled;

            private static (bool IsEnabled, IntPtr RingHandle) DetermineIsEnabledAndCreateRing()
            {
                if (!OperatingSystem.IsLinux())
                {
                    return (false, IntPtr.Zero);
                }

                bool configuredOn =
                    AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.UseIoUring", "DOTNET_USE_IO_URING", defaultValue: true);
                if (!configuredOn)
                {
                    return (false, IntPtr.Zero);
                }

                if (Interop.Sys.IoRingIsAvailable() == 0)
                {
                    return (false, IntPtr.Zero);
                }

                int result = Interop.Sys.IoRingCreate(QueueDepth, QueueDepth, out IntPtr ringHandle);
                if (result != 0)
                {
                    return (false, IntPtr.Zero);
                }

                return (true, ringHandle);
            }

            /// <summary>
            /// Attempts to submit a single request to the shared ring. On success, the operation is now
            /// in flight and its completion will eventually be delivered via
            /// <see cref="IIoUringOperation.CompleteFromIoUring(int)"/>, invoked on a Thread Pool work item.
            /// Returns false if the request could not be submitted (e.g., the submission queue is
            /// currently full); callers should fall back to their non-io_uring code path in that case, as
            /// no partial state is left behind.
            /// </summary>
            public static unsafe bool TrySubmit(IIoUringOperation operation, in Interop.Sys.IoRingRequest request)
            {
                Debug.Assert(s_isEnabled);

                GCHandle handle = GCHandle.Alloc(operation);
                Interop.Sys.IoRingRequest localRequest = request;
                localRequest.UserData = (ulong)GCHandle.ToIntPtr(handle);

                Interlocked.Increment(ref s_submittersInFlight);

                bool submitted;
                using (s_lock.EnterScope())
                {
                    int result = Interop.Sys.IoRingSubmit(s_ringHandle, &localRequest, 1, out int submittedCount);
                    submitted = result == 0 && submittedCount == 1;
                }

                // See s_submittersInFlight's doc comment: the kick must fire whenever this "wave" of
                // concurrent submitters has quiesced, regardless of whether *this* thread's own
                // submission succeeded - not just when `submitted` is true.
                bool moreSubmittersComing = Interlocked.Decrement(ref s_submittersInFlight) != 0;

                if (!moreSubmittersComing)
                {
                    // Ask the kernel to start processing whatever SQEs were just published by this
                    // (possibly multi-thread) batch. This is a plain io_uring_enter(2) call (safe to call
                    // concurrently, unlike IoRingSubmit above) and is deliberately done outside s_lock so
                    // it never serializes concurrent submitters behind one another's syscalls.
                    Interop.Sys.IoRingKick(s_ringHandle);
                }

                if (submitted)
                {
                    Interlocked.Increment(ref s_inFlightCount);

                    // A worker only re-checks TryBecomeDriverAndDrive() opportunistically, right before it
                    // would otherwise park, and submitting via io_uring does not go through the normal
                    // work-queue/semaphore signaling path that would normally wake such a check. Without
                    // this, if every existing worker thread is already parked (blocked in the semaphore
                    // wait) when this operation is submitted, no thread would ever revisit the loop to
                    // notice the new in-flight operation, and its completion would never be reaped - a
                    // permanent hang. Explicitly wake (or create) a worker so it loops back to the top of
                    // its dispatch loop and gets a chance to become the driver. This mirrors exactly what
                    // enqueuing an ordinary Thread Pool work item already does to guarantee a worker runs.
                    //
                    // The s_driverWakeRequested CAS ensures only the first submitter to notice "no driver
                    // active" in a given window actually pays for the wake; concurrent submitters piling
                    // in behind it (common under high concurrency) skip this, since one wake is enough to
                    // get some thread circling back to attempt the CAS in TryBecomeDriverAndDrive.
                    if (Volatile.Read(ref s_isDriving) == 0 &&
                        Interlocked.CompareExchange(ref s_driverWakeRequested, 1, 0) == 0)
                    {
                        WorkerThread.MaybeAddWorkingWorker(ThreadPoolInstance);
                    }
                }
                else
                {
                    handle.Free();
                }

                return submitted;
            }

            /// <summary>
            /// Called by a worker thread that is about to park (has no work left). If this thread wins
            /// the CAS to become the driver and there is at least one in-flight operation, it blocks
            /// in-kernel waiting for at least one completion, then dispatches the corresponding
            /// continuations as ordinary Thread Pool work items (never inline) before returning.
            /// Returns true if this thread drove (and should re-check for normal work before parking),
            /// false if it should proceed to park normally.
            /// </summary>
            public static unsafe bool TryBecomeDriverAndDrive()
            {
                if (!s_isEnabled)
                {
                    return false;
                }

                if (Volatile.Read(ref s_inFlightCount) == 0)
                {
                    // Nothing to wait for; avoid parking forever in io_uring_enter.
                    return false;
                }

                // Clear the wake-request gate as soon as any thread visits here to check on driving,
                // regardless of whether it goes on to win the CAS below. This guarantees the gate can
                // never get stuck at 1 forever - e.g. if the thread that was woken specifically to
                // request this ends up losing the race to another thread that was already cycling
                // through its own dispatch loop and grabs s_isDriving first. As long as at least one
                // worker thread visits this method while an operation is in flight (which happens on
                // every dispatch-loop iteration of every worker), the next round of submissions remains
                // free to request a fresh wake if one is still needed.
                Volatile.Write(ref s_driverWakeRequested, 0);

                if (Interlocked.CompareExchange(ref s_isDriving, 1, 0) != 0)
                {
                    // Someone else is already driving.
                    return false;
                }

                try
                {
                    // No lock is needed here: s_lock only ever guards the SQ ring's producer-side
                    // bookkeeping (pushing new SQEs in TrySubmit), which is entirely separate mmap'd
                    // memory from the CQ ring read here. Exclusive access to the CQ ring is instead
                    // guaranteed by the s_isDriving CAS above - only the winning thread ever calls
                    // IoRingWaitForCompletions, for the whole duration of this method. Taking s_lock
                    // around the first (blocking) call would also risk stalling every concurrent
                    // TrySubmit caller for as long as this thread waits in-kernel for a completion,
                    // which can be indefinite.
                    //
                    // Each call below fetches up to MaxCompletionsPerWait completions in a single
                    // syscall (the native side already drains everything currently available up to
                    // that count), so a driver that wakes up to many simultaneously-ready completions
                    // does not need one syscall per completion.
                    Span<Interop.Sys.IoRingCompletion> completions = stackalloc Interop.Sys.IoRingCompletion[MaxCompletionsPerWait];

                    // Reused across every batch drained by this call. Only ever accessed by the single
                    // thread currently holding the s_isDriving CAS, so no synchronization is needed here.
                    IThreadPoolWorkItem[] workItemBatch = s_workItemBatch;

                    int completedCount;
                    fixed (Interop.Sys.IoRingCompletion* completionsPtr = completions)
                    {
                        int result = Interop.Sys.IoRingWaitForCompletions(s_ringHandle, completionsPtr, MaxCompletionsPerWait, minComplete: 1, out completedCount);
                        if (result != 0 || completedCount == 0)
                        {
                            return true;
                        }
                    }

                    DispatchBatch(completions.Slice(0, completedCount), workItemBatch);

                    // Drain any additional completions that are already available without waiting again.
                    while (true)
                    {
                        int nextCompletedCount;
                        fixed (Interop.Sys.IoRingCompletion* completionsPtr = completions)
                        {
                            int nextResult = Interop.Sys.IoRingWaitForCompletions(s_ringHandle, completionsPtr, MaxCompletionsPerWait, minComplete: 0, out nextCompletedCount);
                            if (nextResult != 0 || nextCompletedCount == 0)
                            {
                                break;
                            }
                        }

                        DispatchBatch(completions.Slice(0, nextCompletedCount), workItemBatch);
                    }
                }
                finally
                {
                    Volatile.Write(ref s_isDriving, 0);
                }

                return true;
            }

            /// <summary>
            /// Completes the operation associated with each of the given completions, collecting the
            /// (non-null) returned work items and queuing them all via a single batched
            /// <see cref="ThreadPool.UnsafeQueueUserWorkItems"/> call instead of once per completion.
            /// </summary>
            private static void DispatchBatch(ReadOnlySpan<Interop.Sys.IoRingCompletion> completions, IThreadPoolWorkItem[] workItemBatch)
            {
                int batchCount = 0;
                foreach (ref readonly Interop.Sys.IoRingCompletion completion in completions)
                {
                    Interlocked.Decrement(ref s_inFlightCount);

                    GCHandle handle = GCHandle.FromIntPtr((IntPtr)completion.UserData);
                    var operation = (IIoUringOperation)handle.Target!;
                    handle.Free();

                    // The driver must not run the continuation inline; CompleteFromIoUring only does
                    // minimal bookkeeping and returns the work item (if any) to be queued, so it can be
                    // batched together with the other completions drained in this pass.
                    IThreadPoolWorkItem? workItem = operation.CompleteFromIoUring(completion.Result);
                    if (workItem is not null)
                    {
                        workItemBatch[batchCount++] = workItem;
                    }
                }

                if (batchCount > 0)
                {
                    // Also wakes the normal idle-worker primitive for any parked sibling to pick these up.
                    ThreadPool.UnsafeQueueUserWorkItems(workItemBatch.AsSpan(0, batchCount), preferLocal: false);
                    Array.Clear(workItemBatch, 0, batchCount);
                }
            }
        }

        /// <summary>
        /// Implemented by types that can be submitted to <see cref="IoUringThreadPool"/> and receive
        /// their completion result back.
        /// </summary>
        internal interface IIoUringOperation
        {
            /// <summary>
            /// Called directly by the driver thread (synchronously, as part of draining the completion
            /// queue) with the raw io_uring completion result: the number of bytes transferred on
            /// success, or <c>-errno</c> on failure. Implementations must only do the minimal bookkeeping
            /// required (e.g., unpinning buffers, storing the result) and must NOT run the continuation
            /// body inline on the driver thread, nor queue it to the Thread Pool themselves. Instead,
            /// return the <see cref="IThreadPoolWorkItem"/> representing the continuation to run, so the
            /// driver can batch it together with the other completions drained in the same pass and
            /// queue them all via a single <see cref="ThreadPool.UnsafeQueueUserWorkItems"/> call, instead
            /// of calling <see cref="ThreadPool.UnsafeQueueUserWorkItem(IThreadPoolWorkItem, bool)"/> once
            /// per completion. Return <see langword="null"/> if this completion does not (yet) require a
            /// continuation to be queued - e.g. a partial write was resubmitted via a new io_uring request
            /// and remains in flight.
            /// </summary>
            IThreadPoolWorkItem? CompleteFromIoUring(int result);
        }
    }
}
