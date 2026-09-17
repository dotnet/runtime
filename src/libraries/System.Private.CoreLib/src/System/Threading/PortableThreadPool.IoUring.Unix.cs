// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        /// <summary>
        /// Implements a single-issuer io_uring/ThreadPool integration: a single ring is created with
        /// <c>IORING_SETUP_SINGLE_ISSUER</c> together with <c>IORING_SETUP_DEFER_TASKRUN</c>, so the
        /// kernel can skip its internal ring-wide lock - at the cost of requiring every
        /// <c>io_uring_enter</c> call (submission *and* completion-wait calls alike) to come from the
        /// same fixed OS thread for the ring's whole lifetime. This was originally attempted with
        /// completion-reaping still rotating across arbitrary Thread Pool worker threads (as in the
        /// shared-ring design) and without <c>DEFER_TASKRUN</c>, but that does not work: the kernel's
        /// single-issuer check applies to *any* <c>io_uring_enter</c> call, including a plain
        /// <c>IORING_ENTER_GETEVENTS</c> wait with nothing to submit - so whichever thread happened to
        /// call in first (a submission, or an unrelated worker thread reaping completions) would
        /// permanently "claim" the ring, and every other thread's calls would fail with <c>-EEXIST</c>
        /// forever. This was confirmed empirically (a worker thread won the race to reap completions
        /// before the intended submitter thread ever got to submit, hanging the process) and matches the
        /// kernel's actual <c>submitter_task</c> check, which is not scoped to "submission calls only".
        ///
        /// Consequently, a single dedicated background thread (not a Thread Pool worker, and not counted
        /// in Thread Pool accounting/hill-climbing) owns *both* submission and completion-reaping for the
        /// ring's entire lifetime - this is the only way to actually use <c>IORING_SETUP_SINGLE_ISSUER</c>
        /// correctly, and since that constraint already forces both roles onto one thread, requesting
        /// <c>DEFER_TASKRUN</c> as well is free extra performance with no further downside: it just means
        /// this same thread must periodically call <c>io_uring_enter(..., IORING_ENTER_GETEVENTS)</c> -
        /// which it already needs to do to reap completions - to pump the kernel's deferred task-work (see
        /// <c>SystemNative_IoRingWaitForCompletions</c>'s native-side doc comment for why this call cannot
        /// be skipped even when nothing is known to be ready). Any other thread that wants to submit a
        /// request enqueues it into a lock-free MPSC queue and wakes this thread by writing to a shared
        /// eventfd registered on the ring (<c>IORING_REGISTER_EVENTFD</c>, see
        /// <see cref="s_wakeEventFd"/>'s doc comment); the issuer thread drains the queue in batches,
        /// submits them, then polls for completions and waits on that same eventfd for either a new
        /// submission or a completion becoming ready (see <see cref="IssuerLoop"/>), rather than spinning
        /// or busy-polling. The submission queue is unbounded, so <see cref="TrySubmit"/> always succeeds
        /// (once enabled) - there is no "ring is full, fall back" signal in this design. Worker threads no
        /// longer participate in reaping completions at all; the dedicated issuer thread owns that
        /// exclusively, since IORING_SETUP_SINGLE_ISSUER requires it.
        ///
        /// A second, related gotcha (also confirmed empirically via a standalone native repro, not
        /// documented in the man page): a IORING_SETUP_SINGLE_ISSUER ring's fixed "owning" thread is
        /// whichever thread calls <c>io_uring_setup(2)</c> - not whichever thread happens to make the
        /// first <c>io_uring_enter(2)</c> call afterwards. So the ring cannot be created in the static
        /// constructor and then only *entered* from the dedicated issuer thread; the ring must be
        /// created by the issuer thread itself, as the very first thing it does.
        ///
        /// That, in turn, creates a third gotcha, this one a plain CLR type-initialization deadlock
        /// rather than anything io_uring-specific: the static constructor cannot simply start the
        /// issuer thread with <see cref="IssuerLoop"/> as its entry point and then block waiting for it
        /// to report back the ring handle, because entering <see cref="IssuerLoop"/> - a member of this
        /// very type - requires this type to have finished initializing first, and the CLR blocks any
        /// thread other than the one currently running a type's static constructor from doing that. The
        /// static constructor below therefore performs the ring-creation handshake using only captured
        /// locals - never touching this type's own static fields from the new thread - and only once
        /// that handshake completes does *this* (the constructor's own) thread publish
        /// <see cref="s_isEnabled"/>/<see cref="s_ringHandle"/>/<see cref="s_wakeEventFd"/> itself, before
        /// returning. Only after that does the new thread go on to call <see cref="IssuerLoop"/>.
        /// </summary>
        internal static class IoUringThreadPool
        {
            // Depth of the shared submission/completion queues. Not currently configurable; may become
            // adaptive (or sharded across multiple rings) in a future iteration.
            private const int QueueDepth = 1024;

            // Maximum number of completions fetched per IoRingWaitForCompletions call. Batching here
            // means the issuer thread, when it wakes up to many simultaneously-ready completions (e.g.
            // under high concurrency), drains all of them via a single syscall instead of one syscall per
            // completion.
            private const int MaxCompletionsPerWait = 64;

            // Defensive safety-net timeout (milliseconds) for the issuer thread's wait when operations
            // are in flight but nothing is immediately ready. In the common/expected case this timeout
            // never actually elapses: the registered eventfd (see s_wakeEventFd) is expected to wake the
            // issuer thread directly whenever deferred completion task-work becomes ready to run - this
            // is the documented intent of pairing IORING_SETUP_DEFER_TASKRUN with a registered eventfd
            // (see SystemNative_IoRingRegisterEventFd's doc comment). This bound exists only to
            // self-heal (within at most this many milliseconds) if that assumption ever turns out to be
            // wrong for some request type/kernel version - trading a small amount of worst-case
            // completion-latency for defense in depth, without reintroducing the tight busy-poll loop
            // this design replaced.
            private const int InFlightWaitTimeoutMs = 1000;

            // Number of Thread.SpinWait(1) iterations the issuer thread performs, checking
            // s_pendingSubmissions between each, before falling back to the real (blocking) EventFdWait
            // call - see the spin loop in IssuerLoop for the full rationale. Only used when something is
            // already in flight (i.e. the ring is actively being used), so this never spins while fully
            // idle.
            private const int SpinCountBeforeBlocking = 64;

            // Opt-out switch: io_uring integration is used by default on Linux when the kernel supports
            // it. Set DOTNET_USE_IO_URING=0 to fall back to the pre-existing (blocking-call-on-a-
            // ThreadPool-work-item) implementation unconditionally.
            //
            // Not readonly, unlike s_ringHandle's usual pattern in the other io_uring architectures: its
            // final value depends on whether the dedicated issuer thread (started from the static
            // constructor) manages to create the ring - see the static constructor's doc comment for the
            // full explanation, including why it is the constructor's own thread, not the issuer thread,
            // that actually assigns this field.
            private static bool s_isEnabled;

            // The shared ring handle, or IntPtr.Zero if unavailable/disabled. See s_isEnabled's doc
            // comment: assigned at most once, by the static constructor's own thread, right before it
            // returns.
            private static IntPtr s_ringHandle;

            // Number of io_uring operations submitted (i.e., enqueued via TrySubmit) but not yet
            // completed. Incremented as soon as a request is handed off to the issuer thread (not only
            // once it has actually been published to the kernel SQ ring), so the issuer thread can tell
            // "is anything in flight at all" apart from "nothing in flight, safe to fully park" - see
            // IssuerLoop.
            private static int s_inFlightCount;

            // Maximum number of requests the issuer thread pulls off s_pendingSubmissions and passes to a
            // single Interop.Sys.IoRingSubmit call. Bounds the size of the reused scratch array and gives
            // the ring a chance to be kicked (and start processing) partway through a very large burst,
            // rather than waiting for the entire burst to be dequeued first.
            private const int MaxRequestsPerSubmitBatch = 256;

            // MPSC hand-off from any thread calling TrySubmit to the single dedicated issuer thread (see
            // IssuerLoop). This ring was created with IORING_SETUP_SINGLE_ISSUER, so only that one thread
            // is permitted to ever call Interop.Sys.IoRingSubmit/IoRingKick/IoRingWaitForCompletions for
            // it - every other thread must go through this queue instead. Unbounded: TrySubmit never
            // blocks or fails due to this queue being "full".
            private static readonly ConcurrentQueue<Interop.Sys.IoRingRequest> s_pendingSubmissions = new();

            // An eventfd registered with the ring via IORING_REGISTER_EVENTFD (see
            // Interop.Sys.IoRingRegisterEventFd), or -1 if unavailable. The kernel bumps its counter
            // (making it readable) whenever a CQE is posted - including, per the documented intent of
            // pairing IORING_SETUP_DEFER_TASKRUN with a registered eventfd, when *deferred* completion
            // task-work becomes ready to run, even though it has not been posted to the CQ yet. TrySubmit
            // also writes to this same fd directly (see Interop.Sys.EventFdWrite) to wake the issuer
            // thread when it enqueues a new request. This replaces a previous
            // ManualResetEventSlim-based design: profiling showed that design's CLR-level
            // spin-before-blocking behavior in Wait() was responsible for a large, measurable CPU cost
            // (ThreadNative_SpinWait) under load, since IssuerLoop calls Wait in a tight cycle whenever
            // anything is in flight. Interop.Sys.EventFdWait is a real (poll(2)-based) kernel wait with
            // no userland spin, and unifies both wake reasons (new submission, and completion becoming
            // ready) onto the one fd/one wait call instead of needing a separate bounded poll interval
            // for each. Assigned at most once, by the static constructor's own thread, right before it
            // returns - see s_ringHandle's doc comment for why.
            private static int s_wakeEventFd = -1;

            // Coalescing flag for TrySubmit's wake-up signal: 0 means no thread has signaled the issuer
            // since its last reset, 1 means one already has (so no further EventFdWrite syscall is
            // needed until the issuer resets it again). Profiling a real workload (TechEmpower JSON
            // benchmark under wrk load) showed thousands of individual EventFdWrite syscalls - one per
            // TrySubmit call - even though the issuer thread only ends up needing a small fraction of
            // that many actual wake-ups, since many concurrent TrySubmit calls from different Thread Pool
            // worker threads land in the same "the issuer thread is already awake and about to drain the
            // queue anyway" window. This flag turns any number of concurrent TrySubmit calls between two
            // issuer wake cycles into at most one EventFdWrite syscall, without risking a missed wake-up:
            // see TrySubmit and IssuerLoop for the reset-then-recheck protocol that makes this safe.
            private static int s_wakeSignaled;

#pragma warning disable CA1810 // remove the explicit static constructor
            static IoUringThreadPool()
            {
                bool isEligible = IsEligible();
                if (!isEligible)
                {
                    s_isEnabled = false;
                    return;
                }

                // The ring itself cannot be created here (on this, the static constructor's own thread):
                // IORING_SETUP_SINGLE_ISSUER binds a ring's single fixed owning thread to whichever
                // thread calls io_uring_setup(2) - *not* to whichever thread happens to make the first
                // io_uring_enter(2) call, as originally (incorrectly) assumed. This was confirmed
                // empirically with a standalone native repro: a second thread's very first
                // io_uring_enter call on a ring created by another thread fails with -EEXIST
                // immediately, even though it is that second thread's first-ever call on the ring. So
                // the ring must be created by the same dedicated thread that will go on to be the one
                // and only thread ever calling IoRingSubmit/IoRingKick/IoRingWaitForCompletions for it -
                // i.e., by a new, dedicated issuer thread, as the very first thing it does.
                //
                // That handshake below is deliberately written to avoid touching any static member of
                // IoUringThreadPool from the new thread: the CLR only allows the thread that is
                // currently running a type's static constructor to freely access that type's own static
                // members while doing so; any *other* thread's attempt to access them (including merely
                // calling one of the type's other static methods, such as IssuerLoop) blocks until the
                // constructor completes. Publishing the ring-creation result via s_ringHandle/
                // s_isEnabled directly from the new thread - before this constructor returns - would
                // therefore deadlock: this thread would be blocked in readyToRun.Wait() below, while the
                // new thread would in turn be blocked trying to write those very fields. Using only
                // captured locals here (created/createdRingHandle/readyToRun) avoids that entirely; only
                // *this* thread - which is allowed to, since it is the one actually running the static
                // constructor - assigns s_isEnabled/s_ringHandle themselves, once the handshake
                // completes.
                using ManualResetEventSlim readyToRun = new(initialState: false);
                bool created = false;
                IntPtr createdRingHandle = IntPtr.Zero;
                int createdEventFd = -1;

                var issuerThread = new Thread(() =>
                {
                    // singleIssuer: true - the whole point of this architecture is that only this
                    // thread (which just called io_uring_setup(2) here, and will be the only thread that
                    // ever calls into this ring from now on) ever touches it, so the kernel can skip its
                    // internal ring-wide lock.
                    int result = Interop.Sys.IoRingCreate(QueueDepth, QueueDepth, singleIssuer: 1, out IntPtr ringHandle);
                    created = result == 0;
                    createdRingHandle = ringHandle;

                    if (created)
                    {
                        createdEventFd = Interop.Sys.IoRingRegisterEventFd(createdRingHandle);
                        created = createdEventFd >= 0;
                    }

                    readyToRun.Set();

                    if (created)
                    {
                        // IssuerLoop is a member of IoUringThreadPool, so entering it may briefly block
                        // this thread here until the static constructor below - which is waiting on
                        // readyToRun.Wait() right after starting this thread - observes the Set() above
                        // and returns. That is expected, bounded, and not a deadlock: by this point the
                        // constructor no longer depends on this thread for anything, so it will finish
                        // and return almost immediately, unblocking this call.
                        IssuerLoop();
                    }
                })
                {
                    IsBackground = true,
                    Name = ".NET IoUring Issuer",
                };
                issuerThread.Start();

                // Block until the issuer thread has created the ring (or failed to). This keeps the
                // external contract identical to every other io_uring architecture in this codebase:
                // once this static constructor returns, IsEnabled/TrySubmit are immediately usable with
                // their final, fully-initialized values, regardless of which thread actually performed
                // the ring creation.
                readyToRun.Wait();

                s_isEnabled = created;
                if (created)
                {
                    s_ringHandle = createdRingHandle;
                    s_wakeEventFd = createdEventFd;
                }
            }
#pragma warning restore CA1810

            /// <summary>Whether the io_uring Thread Pool integration is enabled and usable on this system.</summary>
            public static bool IsEnabled => s_isEnabled;

            private static bool IsEligible()
            {
                if (!OperatingSystem.IsLinux())
                {
                    return false;
                }

                bool configuredOn =
                    AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.UseIoUring", "DOTNET_USE_IO_URING", defaultValue: true);
                if (!configuredOn)
                {
                    return false;
                }

                return Interop.Sys.IoRingIsAvailable() != 0;
            }

            /// <summary>
            /// Attempts to submit a single request to the shared ring. Unlike the other io_uring
            /// architectures in this codebase, this never actually fails once <see cref="IsEnabled"/> is
            /// true: the request is simply enqueued for the dedicated issuer thread to submit, and this
            /// method returns immediately. The operation is now considered in flight; its completion will
            /// eventually be delivered via <see cref="IIoUringOperation.CompleteFromIoUring(int)"/>,
            /// invoked on a Thread Pool work item. The queue backing this hand-off is unbounded - under
            /// sustained overload (submissions arriving faster than the kernel/NIC can drain them),
            /// memory usage here could grow without bound; this is a known, accepted limitation of this
            /// experimental architecture, not an oversight.
            /// </summary>
            public static bool TrySubmit(IIoUringOperation operation, in Interop.Sys.IoRingRequest request)
            {
                Debug.Assert(s_isEnabled);

                GCHandle handle = GCHandle.Alloc(operation);
                Interop.Sys.IoRingRequest localRequest = request;
                localRequest.UserData = (ulong)GCHandle.ToIntPtr(handle);

                Interlocked.Increment(ref s_inFlightCount);
                s_pendingSubmissions.Enqueue(localRequest);

                // Only the thread that wins the 0->1 transition actually writes to the eventfd; every
                // other concurrent caller can rely on that single write to wake the issuer, since the
                // issuer only resets this flag back to 0 immediately before it is about to re-check the
                // queue/wait (see IssuerLoop) - so any enqueue that raced with a reset either gets
                // "counted" by winning this Exchange itself, or is safely picked up by the issuer's own
                // post-reset recheck of the queue.
                if (Interlocked.Exchange(ref s_wakeSignaled, 1) == 0)
                {
                    Interop.Sys.EventFdWrite(s_wakeEventFd);
                }

                return true;
            }

            /// <summary>
            /// Body of the single dedicated issuer thread, once the ring has already been created (by
            /// this same thread - see the static constructor) and <see cref="s_ringHandle"/>/
            /// <see cref="s_wakeEventFd"/>/<see cref="s_isEnabled"/> have been published by it. Every
            /// iteration submits whatever is currently queued in <see cref="s_pendingSubmissions"/>, then
            /// drains and dispatches whatever completions are already available. If nothing at all is in
            /// flight and the queue is empty, parks indefinitely on <see cref="s_wakeEventFd"/> until
            /// <see cref="TrySubmit"/> writes to it. If something is in flight but nothing was
            /// immediately ready, waits on that same fd with a defensive bounded timeout
            /// (<see cref="InFlightWaitTimeoutMs"/>) instead of an indefinite one, purely as a safety net
            /// - see <see cref="InFlightWaitTimeoutMs"/>'s doc comment for why the expected/common case
            /// does not actually rely on this bound elapsing.
            /// </summary>
            private static void IssuerLoop()
            {
                // Reused across every iteration. Only ever accessed by this single dedicated thread, so
                // no synchronization is needed for these arrays.
                var submitBatch = new Interop.Sys.IoRingRequest[MaxRequestsPerSubmitBatch];
                var completionsBatch = new Interop.Sys.IoRingCompletion[MaxCompletionsPerWait];
                var workItemBatch = new IThreadPoolWorkItem[MaxCompletionsPerWait];

                while (true)
                {
                    DrainAndSubmit(submitBatch);
                    DrainCompletions(completionsBatch, workItemBatch);

                    if (!s_pendingSubmissions.IsEmpty)
                    {
                        // Something was enqueued while we were draining; go around again immediately
                        // instead of waiting.
                        continue;
                    }

                    // Reset the wake-coalescing flag (see TrySubmit and s_wakeSignaled) before waiting,
                    // so that any TrySubmit call from here on is guaranteed to win the 0->1 transition and
                    // signal us. Then re-check the queue: a TrySubmit call could have raced with this very
                    // reset (observed the flag as still 1 from a *previous* cycle, so skipped its own
                    // EventFdWrite, right before we set it back to 0) - the recheck below is what catches
                    // that case and avoids a missed wake-up, instead of relying on the write that thread
                    // decided not to do.
                    Volatile.Write(ref s_wakeSignaled, 0);
                    if (!s_pendingSubmissions.IsEmpty)
                    {
                        continue;
                    }

                    bool anyInFlight = Volatile.Read(ref s_inFlightCount) > 0;

                    // Under active use (something already in flight), briefly spin before parking via
                    // the real blocking wait: at low-to-moderate concurrency, requests tend to arrive in
                    // small, closely-spaced bursts rather than in a steady stream large enough to always
                    // find this thread already awake and mid-drain. A full EventFdWait park-then-wake
                    // round trip costs a real scheduling wake-up (which can be many microseconds under
                    // load), whereas a short spin lets an imminent TrySubmit call be observed here almost
                    // immediately instead, at the cost of a bounded bit of otherwise-idle CPU time on this
                    // one dedicated thread. Skipped entirely when nothing is in flight, so a fully idle
                    // ring never spins.
                    if (anyInFlight)
                    {
                        for (int i = 0; i < SpinCountBeforeBlocking; i++)
                        {
                            if (!s_pendingSubmissions.IsEmpty)
                            {
                                break;
                            }

                            Thread.SpinWait(1);
                        }

                        if (!s_pendingSubmissions.IsEmpty)
                        {
                            continue;
                        }
                    }

                    int timeoutMs = anyInFlight ? InFlightWaitTimeoutMs : -1;
                    Interop.Sys.EventFdWait(s_wakeEventFd, timeoutMs);
                }
            }

            /// <summary>
            /// Repeatedly pulls up to <see cref="MaxRequestsPerSubmitBatch"/> requests at a time off
            /// <see cref="s_pendingSubmissions"/> and submits each such batch, until the queue is empty.
            /// Deliberately does *not* call <see cref="Interop.Sys.IoRingKick"/> after the final batch:
            /// the entries it fills are left published to the SQ tail but not yet asked of the kernel,
            /// since the <see cref="DrainCompletions"/> call that always immediately follows this one (see
            /// <see cref="IssuerLoop"/>) submits them together with reaping completions, in a single
            /// syscall - see <see cref="Interop.Sys.IoRingWaitForCompletions"/>'s doc comment. A kick is
            /// only issued between batches, when there is more still queued to drain: that indicates an
            /// unusually large burst (more than one batch's worth arrived at once), in which case it is
            /// worth giving the kernel a chance to make room in the ring before filling more, rather than
            /// leaving arbitrarily many batches' worth of entries unsubmitted until the end.
            /// </summary>
            private static unsafe void DrainAndSubmit(Interop.Sys.IoRingRequest[] batch)
            {
                while (true)
                {
                    int count = 0;
                    while (count < batch.Length && s_pendingSubmissions.TryDequeue(out Interop.Sys.IoRingRequest request))
                    {
                        batch[count++] = request;
                    }

                    if (count == 0)
                    {
                        return;
                    }

                    fixed (Interop.Sys.IoRingRequest* batchPtr = batch)
                    {
                        SubmitBatchWithRetry(batchPtr, count);
                    }

                    if (!s_pendingSubmissions.IsEmpty)
                    {
                        // More still queued - this ring was created with IORING_SETUP_SINGLE_ISSUER
                        // (and IORING_SETUP_DEFER_TASKRUN), so this call - like every other call
                        // touching this ring - is only ever made from this one dedicated thread.
                        Interop.Sys.IoRingKick(s_ringHandle);
                    }
                }
            }

            /// <summary>
            /// Submits every request in <paramref name="requestsPtr"/>[0..<paramref name="count"/>),
            /// retrying only the not-yet-submitted remainder if the ring's submission queue is
            /// momentarily full (<c>submittedCount</c> less than requested), instead of re-enqueueing the
            /// remainder back into <see cref="s_pendingSubmissions"/> - doing the latter could reorder
            /// this batch behind requests enqueued by other threads afterwards, and would also
            /// unnecessarily perturb FIFO-ish submission order for no benefit, since this thread is the
            /// only one that will ever process the queue anyway.
            /// </summary>
            private static unsafe void SubmitBatchWithRetry(Interop.Sys.IoRingRequest* requestsPtr, int count)
            {
                int remaining = count;
                Interop.Sys.IoRingRequest* remainingPtr = requestsPtr;

                while (remaining > 0)
                {
                    int result = Interop.Sys.IoRingSubmit(s_ringHandle, remainingPtr, remaining, out int submittedCount);
                    if (result != 0)
                    {
                        // Unexpected/fatal - nothing more we can safely do for this batch.
                        return;
                    }

                    if (submittedCount >= remaining)
                    {
                        return;
                    }

                    remainingPtr += submittedCount;
                    remaining -= submittedCount;

                    // The ring's SQ was momentarily full - give the kernel a brief chance to make room
                    // (e.g. by processing already-submitted entries) before retrying the remainder.
                    Thread.SpinWait(100);
                }
            }

            /// <summary>
            /// Drains and dispatches every completion currently available, looping until none are left,
            /// without blocking if none are ready yet (<c>minComplete: 0</c>) - any actual waiting for new
            /// completions to arrive is done by the caller, in <see cref="IssuerLoop"/>. Its first
            /// underlying <c>io_uring_enter</c> call (see
            /// <see cref="Interop.Sys.IoRingWaitForCompletions"/>) also flushes any SQEs
            /// <see cref="DrainAndSubmit"/> published just before this call but did not itself submit to
            /// the kernel, so in the common case (a single submit batch per <see cref="IssuerLoop"/>
            /// iteration) submission and completion-reaping happen via one syscall total, not two.
            /// </summary>
            private static unsafe void DrainCompletions(Interop.Sys.IoRingCompletion[] completionsBatch, IThreadPoolWorkItem[] workItemBatch)
            {
                while (true)
                {
                    int completedCount;
                    fixed (Interop.Sys.IoRingCompletion* completionsPtr = completionsBatch)
                    {
                        int result = Interop.Sys.IoRingWaitForCompletions(s_ringHandle, completionsPtr, completionsBatch.Length, minComplete: 0, out completedCount);
                        if (result != 0 || completedCount == 0)
                        {
                            return;
                        }
                    }

                    DispatchBatch(completionsBatch.AsSpan(0, completedCount), workItemBatch);
                }
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

                    // The issuer thread must not run the continuation inline; CompleteFromIoUring only
                    // does minimal bookkeeping and returns the work item (if any) to be queued, so it can
                    // be batched together with the other completions drained in this pass.
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
            /// Called directly by the issuer thread (synchronously, as part of draining the completion
            /// queue) with the raw io_uring completion result: the number of bytes transferred on
            /// success, or <c>-errno</c> on failure. Implementations must only do the minimal bookkeeping
            /// required (e.g., unpinning buffers, storing the result) and must NOT run the continuation
            /// body inline on the issuer thread, nor queue it to the Thread Pool themselves. Instead,
            /// return the <see cref="IThreadPoolWorkItem"/> representing the continuation to run, so the
            /// issuer thread can batch it together with the other completions drained in the same pass and
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
