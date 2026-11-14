// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace System.Threading
{
    public partial class SemaphoreSlim : IDisposable
    {

        // The semaphore count, initialized in the constructor to the initial value, every release call increments it
        // and every wait call decrements it as long as its value is positive otherwise the wait will block.
        // Its value must be between the maximum semaphore value and zero
        private volatile int m_currentCount;

        // The number of synchronously waiting threads, it is set to zero in the constructor and increments before blocking the
        // threading and decrements it back after that. It is used as flag for the release call to know if there are
        // waiting threads in the monitor or not.
        // Volatile so the lock-free WaitAsync fast path observes the increment via release/acquire pairing rather than
        // depending on the lock release of the writer (which the fast path bypasses).
        private volatile int m_waitCount;

        /// <summary>
        /// This is used to help prevent waking more waiters than necessary. It's not perfect and sometimes more waiters than
        /// necessary may still be woken, see <see cref="WaitUntilCountOrTimeout"/>.
        /// </summary>
        private int m_countOfWaitersPulsedToWake;

        // Object used to synchronize access to state on the instance.  The contained
        // Boolean value indicates whether the instance has been disposed.
        private readonly StrongBox<bool> m_lockObjAndDisposed;

        // Act as the semaphore wait handle, it's lazily initialized if needed, the first WaitHandle call initialize it
        // and wait an release sets and resets it respectively as long as it is not null
        private volatile ManualResetEvent? m_waitHandle;

        private bool IsDisposed => m_lockObjAndDisposed.Value;

        [Conditional("DEBUG")]
        private void AssertIsLocked() =>
            Debug.Assert(Monitor.IsEntered(m_lockObjAndDisposed), "Requires the lock be held");

        private bool RemoveAsyncWaiterWithLock(TaskNode task)
        {
            lock (m_lockObjAndDisposed)
            {
                return RemoveAsyncWaiter(task);
            }
        }

        private WaitHandle AvailableWaitHandleCore
        {
            get
            {
                CheckDispose();

                // Return it directly if it is not null
                if (m_waitHandle is null)
                {
                    // lock the count to avoid multiple threads initializing the handle if it is null
                    lock (m_lockObjAndDisposed)
                    {
                        if (m_waitHandle is null)
                        {
                            // Publish the handle in the unsignaled state first, then reflect the current count.
                            // Once m_waitHandle is non-null, the lock-free WaitAsync fast path is excluded (it gates
                            // on m_waitHandle being null). The barrier prevents the m_currentCount read from being
                            // reordered before the publish on weakly-ordered architectures: without it, a concurrent
                            // fast-path CAS that already happened could be missed here, leaving the handle Set when
                            // count == 0. Any fast path that completes between the publish and the count read is
                            // covered by its own post-CAS recovery branch.
                            var handle = new ManualResetEvent(false);
                            m_waitHandle = handle;
                            Interlocked.MemoryBarrier();
                            if (m_currentCount > 0)
                            {
                                handle.Set();
                            }
                        }
                    }
                }

                return m_waitHandle;
            }
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="SemaphoreSlim"/>,
        /// using a 32-bit unsigned integer to measure the time interval,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="Timeout.UnsignedInfinite"/> to
        /// wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>true if the current thread successfully entered the <see cref="SemaphoreSlim"/>; otherwise, false.</returns>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        [UnsupportedOSPlatform("browser")]
        private bool WaitCore(long millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDispose();

            cancellationToken.ThrowIfCancellationRequested();

            // Perf: Check the stack timeout parameter before checking the volatile count
            if (millisecondsTimeout == 0 && m_currentCount == 0)
            {
                // Pessimistic fail fast, check volatile count outside lock (only when timeout is zero!)
                return false;
            }

#if FEATURE_WASM_MANAGED_THREADS
            if (millisecondsTimeout > 0 &&
                (Thread.ThrowOnBlockingWaitOnJSInteropThread || Thread.WarnOnBlockingWaitOnJSInteropThread))
            {
                // Async waiter timeouts may need this thread's event loop, so poll without queuing another async waiter.
                long waitStartTime = Environment.TickCount64;
                SpinWait spinner = default;
                do
                {
                    if (WaitCore(0, cancellationToken))
                    {
                        return true;
                    }

                    spinner.SpinOnce();
                }
                while (TimeoutHelper.UpdateTimeOut(waitStartTime, millisecondsTimeout) != 0);

                return WaitCore(0, cancellationToken);
            }
#endif

            long startTime = 0;
            if (millisecondsTimeout != Timeout.Infinite && millisecondsTimeout > 0)
            {
                startTime = Environment.TickCount64;
            }

            bool waitSuccessful = false;
            Task<bool>? asyncWaitTask = null;
            bool lockTaken = false;

            // Register for cancellation outside of the main lock.
            // NOTE: Register/unregister inside the lock can deadlock as different lock acquisition orders could
            //      occur for (1)this.m_lockObjAndDisposed and (2)cts.internalLock
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.UnsafeRegister(s_cancellationTokenCanceledEventHandler, this);
            try
            {
                // Perf: first spin wait for the count to be positive.
                // This additional amount of spinwaiting in addition
                // to Monitor.Enter()'s spinwaiting has shown measurable perf gains in test scenarios.
                if (m_currentCount == 0)
                {
                    // Monitor.Enter followed by Monitor.Wait is much more expensive than waiting on an event as it involves another
                    // spin, contention, etc. The usual number of spin iterations that would otherwise be used here is increased to
                    // lessen that extra expense of doing a proper wait.
                    int spinCount = SpinWait.SpinCountForSpinBeforeWait * 4;

                    SpinWait spinner = default;
                    while (spinner.Count < spinCount)
                    {
                        spinner.SpinOnce(sleep1Threshold: -1);

                        if (m_currentCount != 0)
                        {
                            break;
                        }
                    }
                }
                Monitor.Enter(m_lockObjAndDisposed, ref lockTaken);
                m_waitCount++;

                // If there are any async waiters, for fairness we'll get in line behind
                // then by translating our synchronous wait into an asynchronous one that we
                // then block on (once we've released the lock).
                if (m_asyncHead is not null)
                {
                    RuntimeFeature.ThrowIfBlockingIsNotSupported(millisecondsTimeout);

                    Debug.Assert(m_asyncTail is not null, "tail should not be null if head isn't");
                    asyncWaitTask = WaitAsyncCore(millisecondsTimeout, cancellationToken);
                }
                // There are no async waiters, so we can proceed with normal synchronous waiting.
                else
                {
                    // Loop to handle the case where the lock-free WaitAsync fast path raced and decremented the
                    // count between our wait/check and TryDecrementCount. With m_waitCount visibly > 0 the fast
                    // path defers, so the loop typically runs once; the residual race during m_waitCount's
                    // publication makes the retry necessary for correctness.
                    while (true)
                    {
                        OperationCanceledException? oce = null;
                        bool timedOut = false;
                        if (m_currentCount == 0)
                        {
                            if (millisecondsTimeout == 0)
                            {
                                return false;
                            }

                            // Prepare for the main wait...
                            // wait until the count becomes greater than zero or the timeout is expired
                            try
                            {
                                timedOut = !WaitUntilCountOrTimeout(millisecondsTimeout, startTime, cancellationToken);
                            }
                            catch (OperationCanceledException e) { oce = e; }
                        }

                        // Now try to acquire.  We prioritize acquisition over cancellation/timeout so that we don't
                        // lose any counts when there are asynchronous waiters in the mix.  Asynchronous waiters
                        // defer to synchronous waiters in priority, which means that if it's possible an asynchronous
                        // waiter didn't get released because a synchronous waiter was present, we need to ensure
                        // that synchronous waiter succeeds so that they have a chance to release.
                        if (TryDecrementCount() > 0)
                        {
                            waitSuccessful = true;
                            break;
                        }

                        if (oce is not null)
                        {
                            throw oce;
                        }
                        if (timedOut)
                        {
                            break;
                        }
                    }

                    // Exposing wait handle which is lazily initialized if needed
                    if (m_waitHandle is not null && m_currentCount == 0)
                    {
                        m_waitHandle.Reset();
                    }
                }
            }
            finally
            {
                // Release the lock
                if (lockTaken)
                {
                    m_waitCount--;
                    Monitor.Exit(m_lockObjAndDisposed);
                }

                // Unregister the cancellation callback.
                cancellationTokenRegistration.Dispose();
            }

            // If we had to fall back to asynchronous waiting, block on it
            // here now that we've released the lock, and return its
            // result when available.  Otherwise, this was a synchronous
            // wait, and whether we successfully acquired the semaphore is
            // stored in waitSuccessful.

            return (asyncWaitTask is not null) ? asyncWaitTask.GetAwaiter().GetResult() : waitSuccessful;
        }

        /// <summary>
        /// Local helper function, waits on the monitor until the monitor receives signal or the
        /// timeout is expired
        /// </summary>
        /// <param name="millisecondsTimeout">The maximum timeout</param>
        /// <param name="startTime">The start ticks to calculate the elapsed time</param>
        /// <param name="cancellationToken">The CancellationToken to observe.</param>
        /// <returns>true if the monitor received a signal, false if the timeout expired</returns>
        [UnsupportedOSPlatform("browser")]
        private bool WaitUntilCountOrTimeout(long millisecondsTimeout, long startTime, CancellationToken cancellationToken)
        {
            RuntimeFeature.ThrowIfBlockingIsNotSupported(millisecondsTimeout);

            int monitorWaitMilliseconds = Timeout.Infinite;

            // Wait on the monitor as long as the count is zero
            while (m_currentCount == 0)
            {
                // If cancelled, we throw. Trying to wait could lead to deadlock.
                cancellationToken.ThrowIfCancellationRequested();

                // Since Monitor.Wait will handle the actual wait and it accepts an int timeout,
                // we may need to cap the timeout to int.MaxValue.
                bool timeoutIsCapped = false;
                if (millisecondsTimeout != Timeout.Infinite)
                {
                    long remainingWaitMilliseconds = TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
                    if (remainingWaitMilliseconds <= 0)
                    {
                        // The thread has expires its timeout
                        return false;
                    }
                    if (remainingWaitMilliseconds <= int.MaxValue)
                    {
                        monitorWaitMilliseconds = (int)remainingWaitMilliseconds;
                    }
                    else
                    {
                        timeoutIsCapped = true;
                        monitorWaitMilliseconds = int.MaxValue;
                    }
                }


                // The actual wait. If the timeout was capped and waitSuccessful is false, it doesn't imply
                // a timeout, we are just limited by Monitor.Wait's maximum timeout value.
                bool waitSuccessful = Monitor.Wait(m_lockObjAndDisposed, monitorWaitMilliseconds);

                // This waiter has woken up and this needs to be reflected in the count of waiters pulsed to wake. Since we
                // don't have thread-specific pulse state, there is not enough information to tell whether this thread woke up
                // because it was pulsed. For instance, this thread may have timed out and may have been waiting to reacquire
                // the lock before returning from Monitor.Wait, in which case we don't know whether this thread got pulsed. So
                // in any woken case, decrement the count if possible. As such, timeouts could cause more waiters to wake than
                // necessary.
                if (m_countOfWaitersPulsedToWake != 0)
                {
                    --m_countOfWaitersPulsedToWake;
                }

                if (!timeoutIsCapped && !waitSuccessful)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Asynchronously waits to enter the <see cref="SemaphoreSlim"/>,
        /// using a 32-bit unsigned integer to measure the time interval,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="Timeout.UnsignedInfinite"/> to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the <see cref="SemaphoreSlim"/>, otherwise with a result of false.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        private Task<bool> WaitAsyncCore(long millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDispose();

            // Bail early for cancellation
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<bool>(cancellationToken);

            // Fast path: try a lock-free acquire; falls through to the lock if it fails.
            // Skipped when m_waitHandle is non-null to keep its state consistent under the lock.
            if (m_waitHandle is null)
            {
                int current = m_currentCount;
                // Best-effort waiter checks (m_asyncHead and m_waitCount are volatile, so plain reads
                // are acquire-ordered): they may be updated after this read, but the CAS will fail if
                // m_currentCount was concurrently decremented.
                if (current > 0
                    && m_asyncHead is null
                    && m_waitCount == 0
                    && Interlocked.CompareExchange(ref m_currentCount, current - 1, current) == current)
                {
                    // Handle the rare race where AvailableWaitHandle was initialized concurrently.
                    if (current == 1 && m_waitHandle is not null)
                    {
                        ResetWaitHandleIfEmpty();
                    }
                    return Task.FromResult(true);
                }
            }

            return WaitAsyncCoreSlow(millisecondsTimeout, cancellationToken);
        }

        // Kept out of line so WaitAsyncCore has no EH: the fast path then needs no frame setup or lock bookkeeping.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Task<bool> WaitAsyncCoreSlow(long millisecondsTimeout, CancellationToken cancellationToken)
        {
            lock (m_lockObjAndDisposed)
            {
                return WaitAsyncCoreLocked(millisecondsTimeout, cancellationToken);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResetWaitHandleIfEmpty()
        {
            lock (m_lockObjAndDisposed)
            {
                if (m_waitHandle is not null && m_currentCount == 0)
                    m_waitHandle.Reset();
            }
        }

        /// <summary>
        /// Atomically decrements <see cref="m_currentCount"/> if it is positive, using a CAS loop
        /// rather than a plain decrement because the lock-free fast path in <see cref="WaitAsyncCore"/>
        /// can decrement <see cref="m_currentCount"/> concurrently without holding the lock.
        /// </summary>
        /// <returns>The pre-decrement value. A return value of 0 means no count was available.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int TryDecrementCount()
        {
            int count = m_currentCount;
            while (count > 0 && Interlocked.CompareExchange(ref m_currentCount, count - 1, count) != count)
                count = m_currentCount;
            return count;
        }

        private int ReleaseCore(int releaseCount)
        {
            CheckDispose();

            if (releaseCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(releaseCount), releaseCount, SR.SemaphoreSlim_Release_CountWrong);
            }
            int returnCount;

            lock (m_lockObjAndDisposed)
            {
                // Snapshot the live count. A lock-free WaitAsync fast path can decrement m_currentCount
                // concurrently (it bypasses this lock); nothing increments it concurrently (every increment
                // path holds this lock). So the real count can only be <= this snapshot until we update it.
                int observed = m_currentCount;

                // Validate against m_maxCount. Re-read on a mismatch so a racing fast-path decrement (which
                // only lowers the real count) can't make us throw SemaphoreFullException spuriously off a
                // stale, too-high snapshot. Because only decrements race, once observed + releaseCount <=
                // m_maxCount holds for a snapshot >= the real count, the real count + releaseCount can't
                // exceed m_maxCount either, so the bound we enforce on the atomic add below still holds.
                while (m_maxCount - observed < releaseCount)
                {
                    int reread = m_currentCount;
                    if (reread == observed)
                    {
                        throw new SemaphoreFullException();
                    }
                    observed = reread;
                }
                returnCount = observed;

                // Compute the post-release count in a LOCAL only. We must never store this inflated value into
                // m_currentCount: it includes permits earmarked for the waiters released below, and the
                // lock-free fast path (which reads m_currentCount without the lock) would observe and steal
                // them in the window before we corrected the count. Instead we apply only the net delta once,
                // atomically, at the end. Whenever waiters are present the count is 0 and no fast path can be
                // racing (it requires count > 0 and no waiters), so this snapshot is stable here.
                int currentCount = observed + releaseCount;

                // Signal synchronous waiters, accounting for those already pulsed but not yet woken.
                int waitCount = m_waitCount;
                Debug.Assert(m_countOfWaitersPulsedToWake <= waitCount);
                int waitersToNotify = Math.Min(currentCount, waitCount) - m_countOfWaitersPulsedToWake;
                if (waitersToNotify > 0)
                {
                    // Ideally, limiting to a maximum of releaseCount would not be necessary and could be an assert instead, but
                    // since WaitUntilCountOrTimeout() does not have enough information to tell whether a woken thread was
                    // pulsed, it's possible for m_countOfWaitersPulsedToWake to be less than the number of threads that have
                    // actually been pulsed to wake.
                    if (waitersToNotify > releaseCount)
                    {
                        waitersToNotify = releaseCount;
                    }

                    m_countOfWaitersPulsedToWake += waitersToNotify;
                    for (int i = 0; i < waitersToNotify; i++)
                    {
                        Monitor.Pulse(m_lockObjAndDisposed);
                    }
                }

                // Now signal to any asynchronous waiters, if there are any.  While we've already
                // signaled the synchronous waiters, we still hold the lock, and thus
                // they won't have had an opportunity to acquire this yet.  So, when releasing
                // asynchronous waiters, we assume that all synchronous waiters will eventually
                // acquire the semaphore.  That could be a faulty assumption if those synchronous
                // waits are canceled, but the wait code path will handle that.
                // Permits handed to async waiters go straight to their tasks rather than into
                // m_currentCount, so they're excluded from the net delta applied below.
                int asyncReleased = 0;
                if (m_asyncHead is not null)
                {
                    Debug.Assert(m_asyncTail is not null, "tail should not be null if head isn't null");
                    int maxAsyncToRelease = currentCount - waitCount;
                    while (asyncReleased < maxAsyncToRelease && m_asyncHead is not null)
                    {
                        ++asyncReleased;

                        TaskNode waiterTask = m_asyncHead;
                        RemoveAsyncWaiter(waiterTask); // ensures waiterTask.Next/Prev are null
                        waiterTask.TrySetResult(result: true);
                    }
                    currentCount -= asyncReleased;
                }

                // Apply the net change (permits released minus those handed straight to async waiters) in a
                // single atomic add. A relative add (not an absolute store) folds in any fast-path decrements
                // that raced since we snapshotted, and we never publish a count above the number of genuinely
                // free permits, so the fast path can never observe a permit reserved for a waiter. The
                // pre-validated snapshot bounds the result at or below m_maxCount.
                int delta = releaseCount - asyncReleased;
                int newCount = delta != 0 ? Interlocked.Add(ref m_currentCount, delta) : observed;

                if (m_waitHandle is not null && observed == 0 && newCount > 0)
                {
                    m_waitHandle.Set();
                }
            }

            return returnCount;
        }

        private void DisposeCore(bool disposing)
        {
            if (disposing)
            {
                WaitHandle? wh = m_waitHandle;
                if (wh is not null)
                {
                    wh.Dispose();
                    m_waitHandle = null;
                }

                m_lockObjAndDisposed.Value = true;

                m_asyncHead = null;
                m_asyncTail = null;
            }
        }

        /// <summary>
        /// Private helper method to wake up waiters when a cancellationToken gets canceled.
        /// </summary>
        private static readonly Action<object?> s_cancellationTokenCanceledEventHandler = new Action<object?>(CancellationTokenCanceledEventHandler);
        private static void CancellationTokenCanceledEventHandler(object? obj)
        {
            Debug.Assert(obj is SemaphoreSlim, "Expected a SemaphoreSlim");
            SemaphoreSlim semaphore = (SemaphoreSlim)obj;
            lock (semaphore.m_lockObjAndDisposed)
            {
                Monitor.PulseAll(semaphore.m_lockObjAndDisposed); // wake up all waiters.
            }
        }
    }
}
