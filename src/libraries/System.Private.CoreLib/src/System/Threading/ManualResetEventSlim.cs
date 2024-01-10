// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Threading
{
    // ManualResetEventSlim wraps a manual-reset event internally with a little bit of
    // spinning. When an event will be set imminently, it is often advantageous to avoid
    // a 4k+ cycle context switch in favor of briefly spinning. Therefore we layer on to
    // a brief amount of spinning that should, on the average, make using the slim event
    // cheaper than using Win32 events directly. This can be reset manually, much like
    // a Win32 manual-reset would be.
    //
    // Notes:
    //     We lazily allocate the Win32 event internally. Therefore, the caller should
    //     always call Dispose to clean it up, just in case. This API is a no-op of the
    //     event wasn't allocated, but if it was, ensures that the event goes away
    //     eagerly, instead of waiting for finalization.

    /// <summary>
    /// Provides a slimmed down version of <see cref="ManualResetEvent"/>.
    /// </summary>
    /// <remarks>
    /// All public and protected members of <see cref="ManualResetEventSlim"/> are thread-safe and may be used
    /// concurrently from multiple threads, with the exception of Dispose, which
    /// must only be used when all other operations on the <see cref="ManualResetEventSlim"/> have
    /// completed, and Reset, which should only be used when no other threads are
    /// accessing the event.
    /// </remarks>
    [DebuggerDisplay("Set = {IsSet}")]
    public class ManualResetEventSlim : IDisposable
    {
        // These are the default spin counts we use on single-proc and MP machines.
        private const int DEFAULT_SPIN_SP = 1;

        private volatile object? m_lock;
        // A lock used for waiting and pulsing. Lazily initialized via EnsureLockObjectCreated()

        private volatile ManualResetEvent? m_eventObj; // A true Win32 event used for waiting.

        // -- State -- //
        // For a packed word a uint would seem better, but Interlocked.* doesn't support them as uint isn't CLS-compliant.
        private volatile int m_combinedState; // ie a uint. Used for the state items listed below.

        // 1-bit for  signalled state
        private const int SignalledState_BitMask = unchecked((int)0x80000000); // 1000 0000 0000 0000 0000 0000 0000 0000
        private const int SignalledState_ShiftCount = 31;

        // 1-bit for disposed state
        private const int Dispose_BitMask = unchecked((int)0x40000000); // 0100 0000 0000 0000 0000 0000 0000 0000

        // 11-bits for m_spinCount
        private const int SpinCountState_BitMask = unchecked((int)0x3FF80000); // 0011 1111 1111 1000 0000 0000 0000 0000
        private const int SpinCountState_ShiftCount = 19;
        private const int SpinCountState_MaxValue = (1 << 11) - 1; // 2047

        // 19-bits for m_waiters.  This allows support of 512K threads waiting which should be ample
        private const int NumWaitersState_BitMask = unchecked((int)0x0007FFFF); // 0000 0000 0000 0111 1111 1111 1111 1111
        private const int NumWaitersState_ShiftCount = 0;
        private const int NumWaitersState_MaxValue = (1 << 19) - 1; // 512K-1
        // ----------- //

        /// <summary>
        /// Gets the underlying <see cref="Threading.WaitHandle"/> object for this <see
        /// cref="ManualResetEventSlim"/>.
        /// </summary>
        /// <value>The underlying <see cref="Threading.WaitHandle"/> event object fore this <see
        /// cref="ManualResetEventSlim"/>.</value>
        /// <remarks>
        /// Accessing this property forces initialization of an underlying event object if one hasn't
        /// already been created.  To simply wait on this <see cref="ManualResetEventSlim"/>,
        /// the public Wait methods should be preferred.
        /// </remarks>
        public WaitHandle WaitHandle
        {
            get
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                if (m_eventObj == null)
                {
                    // Lazily initialize the event object if needed.
                    LazyInitializeEvent();
                    Debug.Assert(m_eventObj != null);
                }

                return m_eventObj;
            }
        }

        /// <summary>
        /// Gets whether the event is set.
        /// </summary>
        /// <value>true if the event has is set; otherwise, false.</value>
        public bool IsSet
        {
            get => 0 != ExtractStatePortion(m_combinedState, SignalledState_BitMask);
            private set => UpdateStateAtomically(((value) ? 1 : 0) << SignalledState_ShiftCount, SignalledState_BitMask);
        }

        /// <summary>
        /// Gets the number of spin waits that will be occur before falling back to a true wait.
        /// </summary>
        public int SpinCount
        {
            get => ExtractStatePortionAndShiftRight(m_combinedState, SpinCountState_BitMask, SpinCountState_ShiftCount);
            private set
            {
                Debug.Assert(value >= 0, "SpinCount is a restricted-width integer. The value supplied is outside the legal range.");
                Debug.Assert(value <= SpinCountState_MaxValue, "SpinCount is a restricted-width integer. The value supplied is outside the legal range.");
                // Don't worry about thread safety because it's set one time from the constructor
                m_combinedState = (m_combinedState & ~SpinCountState_BitMask) | (value << SpinCountState_ShiftCount);
            }
        }

        /// <summary>
        /// How many threads are waiting.
        /// </summary>
        private int Waiters
        {
            get => ExtractStatePortionAndShiftRight(m_combinedState, NumWaitersState_BitMask, NumWaitersState_ShiftCount);
            set
            {
                // setting to <0 would indicate an internal flaw, hence Assert is appropriate.
                Debug.Assert(value >= 0, "NumWaiters should never be less than zero. This indicates an internal error.");

                // it is possible for the max number of waiters to be exceeded via user-code, hence we use a real exception here.
                if (value >= NumWaitersState_MaxValue)
                    throw new InvalidOperationException(SR.Format(SR.ManualResetEventSlim_ctor_TooManyWaiters, NumWaitersState_MaxValue));

                UpdateStateAtomically(value << NumWaitersState_ShiftCount, NumWaitersState_BitMask);
            }
        }

        //-----------------------------------------------------------------------------------
        // Constructs a new event, optionally specifying the initial state and spin count.
        // The defaults are that the event is unsignaled and some reasonable default spin.
        //

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualResetEventSlim"/>
        /// class with an initial state of nonsignaled.
        /// </summary>
        public ManualResetEventSlim()
            : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualResetEventSlim"/>
        /// class with a boolean value indicating whether to set the initial state to signaled.
        /// </summary>
        /// <param name="initialState">true to set the initial state signaled; false to set the initial state
        /// to nonsignaled.</param>
        public ManualResetEventSlim(bool initialState)
        {
            // Specify the default spin count, and use default spin if we're
            // on a multi-processor machine. Otherwise, we won't.
            Initialize(initialState, SpinWait.SpinCountforSpinBeforeWait);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualResetEventSlim"/>
        /// class with a Boolean value indicating whether to set the initial state to signaled and a specified
        /// spin count.
        /// </summary>
        /// <param name="initialState">true to set the initial state to signaled; false to set the initial state
        /// to nonsignaled.</param>
        /// <param name="spinCount">The number of spin waits that will occur before falling back to a true
        /// wait.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="spinCount"/> is less than
        /// 0 or greater than the maximum allowed value.</exception>
        public ManualResetEventSlim(bool initialState, int spinCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(spinCount);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(spinCount, SpinCountState_MaxValue);

            // We will suppress default spin  because the user specified a count.
            Initialize(initialState, spinCount);
        }

        /// <summary>
        /// Initializes the internal state of the event.
        /// </summary>
        /// <param name="initialState">Whether the event is set initially or not.</param>
        /// <param name="spinCount">The spin count that decides when the event will block.</param>
        private void Initialize(bool initialState, int spinCount)
        {
            m_combinedState = initialState ? (1 << SignalledState_ShiftCount) : 0;
            // the spinCount argument has been validated by the ctors.
            // but we now sanity check our predefined constants.
            Debug.Assert(DEFAULT_SPIN_SP >= 0, "Internal error - DEFAULT_SPIN_SP is outside the legal range.");
            Debug.Assert(DEFAULT_SPIN_SP <= SpinCountState_MaxValue, "Internal error - DEFAULT_SPIN_SP is outside the legal range.");

            SpinCount = Environment.IsSingleProcessor ? DEFAULT_SPIN_SP : spinCount;
        }

        /// <summary>
        /// Helper to ensure the lock object is created before first use.
        /// </summary>
        private void EnsureLockObjectCreated()
        {
            if (m_lock != null)
                return;

            object newObj = new object();
            Interlocked.CompareExchange(ref m_lock, newObj, null); // failure is benign. Someone else set the value.
        }

        /// <summary>
        /// This method lazily initializes the event object. It uses CAS to guarantee that
        /// many threads racing to call this at once don't result in more than one event
        /// being stored and used. The event will be signaled or unsignaled depending on
        /// the state of the thin-event itself, with synchronization taken into account.
        /// </summary>
        private void LazyInitializeEvent()
        {
            bool preInitializeIsSet = IsSet;
            ManualResetEvent newEventObj = new ManualResetEvent(preInitializeIsSet);

            // We have to CAS this in case we are racing with another thread. We must
            // guarantee only one event is actually stored in this field.
            if (Interlocked.CompareExchange(ref m_eventObj, newEventObj, null) != null)
            {
                // Someone else set the value due to a race condition. Destroy the garbage event.
                newEventObj.Dispose();
            }
            else
            {
                // Now that the event is published, verify that the state hasn't changed since
                // we snapped the preInitializeState. Another thread could have done that
                // between our initial observation above and here. The barrier incurred from
                // the CAS above (in addition to m_state being volatile) prevents this read
                // from moving earlier and being collapsed with our original one.
                bool currentIsSet = IsSet;
                if (currentIsSet != preInitializeIsSet)
                {
                    Debug.Assert(currentIsSet,
                        "The only safe concurrent transition is from unset->set: detected set->unset.");

                    // We saw it as unsignaled, but it has since become set.
                    lock (newEventObj)
                    {
                        // If our event hasn't already been disposed of, we must set it.
                        if (m_eventObj == newEventObj)
                        {
                            newEventObj.Set();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the state of the event to signaled, which allows one or more threads waiting on the event to
        /// proceed.
        /// </summary>
        public void Set()
        {
            Set(false);
        }

        /// <summary>
        /// Private helper to actually perform the Set.
        /// </summary>
        /// <param name="duringCancellation">Indicates whether we are calling Set() during cancellation.</param>
        /// <exception cref="OperationCanceledException">The object has been canceled.</exception>
        private void Set(bool duringCancellation)
        {
            // We need to ensure that IsSet=true does not get reordered past the read of m_eventObj
            // This would be a legal movement according to the .NET memory model.
            // The code is safe as IsSet involves an Interlocked.CompareExchange which provides a full memory barrier.
            IsSet = true;

            // If there are waiting threads, we need to pulse them.
            if (Waiters > 0)
            {
                Debug.Assert(m_lock != null); // if waiters>0, then m_lock has already been created.
                lock (m_lock)
                {
                    Monitor.PulseAll(m_lock);
                }
            }

            ManualResetEvent? eventObj = m_eventObj;

            // Design-decision: do not set the event if we are in cancellation -> better to deadlock than to wake up waiters incorrectly
            // It would be preferable to wake up the event and have it throw OCE. This requires MRE to implement cancellation logic

            if (eventObj != null && !duringCancellation)
            {
                // We must surround this call to Set in a lock.  The reason is fairly subtle.
                // Sometimes a thread will issue a Wait and wake up after we have set m_state,
                // but before we have gotten around to setting m_eventObj (just below). That's
                // because Wait first checks m_state and will only access the event if absolutely
                // necessary.  However, the coding pattern { event.Wait(); event.Dispose() } is
                // quite common, and we must support it.  If the waiter woke up and disposed of
                // the event object before the setter has finished, however, we would try to set a
                // now-disposed Win32 event. Crash! To deal with this race condition, we use a lock to
                // protect access to the event object when setting and disposing of it.  We also
                // double-check that the event has not become null in the meantime when in the lock.

                lock (eventObj)
                {
                    // If somebody is waiting, we must set the event.
                    m_eventObj?.Set();
                }
            }
        }

        /// <summary>
        /// Sets the state of the event to nonsignaled, which causes threads to block.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="ManualResetEventSlim"/>, <see cref="Reset()"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        public void Reset()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            // If there's an event, reset it.
            m_eventObj?.Reset();

            // There is a race condition here. If another thread Sets the event, we will get into a state
            // where m_state will be unsignaled, yet the Win32 event object will have been signaled.
            // This could cause waiting threads to wake up even though the event is in an
            // unsignaled state. This is fine -- those that are calling Reset concurrently are
            // responsible for doing "the right thing" -- e.g. rechecking the condition and
            // resetting the event manually.

            // And finally set our state back to unsignaled.
            IsSet = false;
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
        /// <remarks>
        /// The caller of this method blocks indefinitely until the current instance is set. The caller will
        /// return immediately if the event is currently in a set state.
        /// </remarks>
#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public void Wait()
        {
            Wait(Timeout.Infinite, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> receives a signal,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to
        /// observe.</param>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was
        /// canceled.</exception>
        /// <remarks>
        /// The caller of this method blocks indefinitely until the current instance is set. The caller will
        /// return immediately if the event is currently in a set state.
        /// </remarks>
#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public void Wait(CancellationToken cancellationToken)
        {
            Wait(Timeout.Infinite, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set, using a
        /// <see cref="TimeSpan"/> to measure the time interval.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <returns>true if the <see cref="ManualResetEventSlim"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater
        /// than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public bool Wait(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;

            ArgumentOutOfRangeException.ThrowIfLessThan(totalMilliseconds, -1, nameof(timeout));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(totalMilliseconds, int.MaxValue, nameof(timeout));

            return Wait((int)totalMilliseconds, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set, using a
        /// <see cref="TimeSpan"/> to measure the time interval, while observing a <see
        /// cref="CancellationToken"/>.
        /// </summary>
        /// <param name="timeout">A <see cref="TimeSpan"/> that represents the number of milliseconds
        /// to wait, or a <see cref="TimeSpan"/> that represents -1 milliseconds to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to
        /// observe.</param>
        /// <returns>true if the <see cref="ManualResetEventSlim"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative
        /// number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater
        /// than <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref
        /// name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;

            ArgumentOutOfRangeException.ThrowIfLessThan(totalMilliseconds, -1, nameof(timeout));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(totalMilliseconds, int.MaxValue, nameof(timeout));

            return Wait((int)totalMilliseconds, cancellationToken);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set, using a
        /// 32-bit signed integer to measure the time interval.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="Timeout.Infinite"/>(-1) to wait indefinitely.</param>
        /// <returns>true if the <see cref="ManualResetEventSlim"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public bool Wait(int millisecondsTimeout)
        {
            return Wait(millisecondsTimeout, CancellationToken.None);
        }

        /// <summary>
        /// Blocks the current thread until the current <see cref="ManualResetEventSlim"/> is set, using a
        /// 32-bit signed integer to measure the time interval, while observing a <see
        /// cref="CancellationToken"/>.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="Timeout.Infinite"/>(-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to
        /// observe.</param>
        /// <returns>true if the <see cref="ManualResetEventSlim"/> was set; otherwise,
        /// false.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="InvalidOperationException">
        /// The maximum number of waiters has been exceeded.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref
        /// name="cancellationToken"/> was canceled.</exception>
#if !FEATURE_WASM_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            cancellationToken.ThrowIfCancellationRequested(); // an early convenience check

            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            if (!IsSet)
            {
                if (millisecondsTimeout == 0)
                {
                    // For 0-timeouts, we just return immediately.
                    return false;
                }


                // We spin briefly before falling back to allocating and/or waiting on a true event.
                uint startTime = 0;
                bool bNeedTimeoutAdjustment = false;
                int realMillisecondsTimeout = millisecondsTimeout; // this will be adjusted if necessary.

                if (millisecondsTimeout != Timeout.Infinite)
                {
                    // We will account for time spent spinning, so that we can decrement it from our
                    // timeout.  In most cases the time spent in this section will be negligible.  But
                    // we can't discount the possibility of our thread being switched out for a lengthy
                    // period of time.  The timeout adjustments only take effect when and if we actually
                    // decide to block in the kernel below.

                    startTime = TimeoutHelper.GetTime();
                    bNeedTimeoutAdjustment = true;
                }

                // Spin
                int spinCount = SpinCount;
                SpinWait spinner = default;
                while (spinner.Count < spinCount)
                {
                    spinner.SpinOnce(sleep1Threshold: -1);

                    if (IsSet)
                    {
                        return true;
                    }

                    if (spinner.Count >= 100 && spinner.Count % 10 == 0) // check the cancellation token if the user passed a very large spin count
                        cancellationToken.ThrowIfCancellationRequested();
                }

                // Now enter the lock and wait. Must be created before registering the cancellation callback,
                // which will try to take this lock.
                EnsureLockObjectCreated();

                // We must register and unregister the token outside of the lock, to avoid deadlocks.
                using (cancellationToken.UnsafeRegister(s_cancellationTokenCallback, this))
                {
                    lock (m_lock!)
                    {
                        // Loop to cope with spurious wakeups from other waits being canceled
                        while (!IsSet)
                        {
                            // If our token was canceled, we must throw and exit.
                            cancellationToken.ThrowIfCancellationRequested();

                            // update timeout (delays in wait commencement are due to spinning and/or spurious wakeups from other waits being canceled)
                            if (bNeedTimeoutAdjustment)
                            {
                                realMillisecondsTimeout = TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
                                if (realMillisecondsTimeout <= 0)
                                    return false;
                            }

                            // There is a race condition that Set will fail to see that there are waiters as Set does not take the lock,
                            // so after updating waiters, we must check IsSet again.
                            // Also, we must ensure there cannot be any reordering of the assignment to Waiters and the
                            // read from IsSet.  This is guaranteed as Waiters{set;} involves an Interlocked.CompareExchange
                            // operation which provides a full memory barrier.
                            // If we see IsSet=false, then we are guaranteed that Set() will see that we are
                            // waiting and will pulse the monitor correctly.

                            Waiters++;

                            if (IsSet) // This check must occur after updating Waiters.
                            {
                                Waiters--; // revert the increment.
                                return true;
                            }

                            // Now finally perform the wait.
                            try
                            {
                                // ** the actual wait **
                                if (!Monitor.Wait(m_lock, realMillisecondsTimeout))
                                    return false; // return immediately if the timeout has expired.
                            }
                            finally
                            {
                                // Clean up: we're done waiting.
                                Waiters--;
                            }
                            // Now just loop back around, and the right thing will happen.  Either:
                            //     1. We had a spurious wake-up due to some other wait being canceled via a different cancellationToken (rewait)
                            // or  2. the wait was successful. (the loop will break)
                        }
                    }
                }
            } // automatically disposes (and unregisters) the callback

            return true; // done. The wait was satisfied.
        }

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="ManualResetEventSlim"/>.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="ManualResetEventSlim"/>, <see cref="Dispose()"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// When overridden in a derived class, releases the unmanaged resources used by the
        /// <see cref="ManualResetEventSlim"/>, and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.</param>
        /// <remarks>
        /// Unlike most of the members of <see cref="ManualResetEventSlim"/>, <see cref="Dispose(bool)"/> is not
        /// thread-safe and may not be used concurrently with other members of this instance.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if ((m_combinedState & Dispose_BitMask) != 0)
                return; // already disposed

            m_combinedState |= Dispose_BitMask; // set the dispose bit
            if (disposing)
            {
                // We will dispose of the event object.  We do this under a lock to protect
                // against the race condition outlined in the Set method above.
                ManualResetEvent? eventObj = m_eventObj;
                if (eventObj != null)
                {
                    lock (eventObj)
                    {
                        eventObj.Dispose();
                        m_eventObj = null;
                    }
                }
            }
        }

        private bool IsDisposed => (m_combinedState & Dispose_BitMask) != 0;

        /// <summary>
        /// Private helper method to wake up waiters when a cancellationToken gets canceled.
        /// </summary>
        private static readonly Action<object?> s_cancellationTokenCallback = new Action<object?>(CancellationTokenCallback);
        private static void CancellationTokenCallback(object? obj)
        {
            Debug.Assert(obj is ManualResetEventSlim, "Expected a ManualResetEventSlim");
            ManualResetEventSlim mre = (ManualResetEventSlim)obj;
            Debug.Assert(mre.m_lock != null); // the lock should have been created before this callback is registered for use.
            lock (mre.m_lock)
            {
                Monitor.PulseAll(mre.m_lock); // awaken all waiters
            }
        }

        /// <summary>
        /// Private helper method for updating parts of a bit-string state value.
        /// Mainly called from the IsSet and Waiters properties setters
        /// </summary>
        /// <remarks>
        /// Note: the parameter types must be int as CompareExchange cannot take a Uint
        /// </remarks>
        /// <param name="newBits">The new value</param>
        /// <param name="updateBitsMask">The mask used to set the bits</param>
        private void UpdateStateAtomically(int newBits, int updateBitsMask)
        {
            SpinWait sw = default;

            Debug.Assert((newBits | updateBitsMask) == updateBitsMask, "newBits do not fall within the updateBitsMask.");

            while (true)
            {
                int oldState = m_combinedState; // cache the old value for testing in CAS

                // Procedure:(1) zero the updateBits.  eg oldState = [11111111] flag= [00111000] newState = [11000111]
                //           then (2) map in the newBits. eg [11000111] newBits=00101000, newState=[11101111]
                int newState = (oldState & ~updateBitsMask) | newBits;

                if (Interlocked.CompareExchange(ref m_combinedState, newState, oldState) == oldState)
                {
                    return;
                }

                sw.SpinOnce(sleep1Threshold: -1);
            }
        }

        /// <summary>
        /// Private helper method - performs Mask and shift, particular helpful to extract a field from a packed word.
        /// eg ExtractStatePortionAndShiftRight(0x12345678, 0xFF000000, 24) => 0x12, ie extracting the top 8-bits as a simple integer
        ///
        /// ?? is there a common place to put this rather than being private to MRES?
        /// </summary>
        /// <param name="state"></param>
        /// <param name="mask"></param>
        /// <param name="rightBitShiftCount"></param>
        /// <returns></returns>
        private static int ExtractStatePortionAndShiftRight(int state, int mask, int rightBitShiftCount)
        {
            // convert to uint before shifting so that right-shift does not replicate the sign-bit,
            // then convert back to int.
            return unchecked((int)(((uint)(state & mask)) >> rightBitShiftCount));
        }

        /// <summary>
        /// Performs a Mask operation, but does not perform the shift.
        /// This is acceptable for boolean values for which the shift is unnecessary
        /// eg (val &amp; Mask) != 0 is an appropriate way to extract a boolean rather than using
        /// ((val &amp; Mask) &gt;&gt; shiftAmount) == 1
        ///
        /// ?? is there a common place to put this rather than being private to MRES?
        /// </summary>
        /// <param name="state"></param>
        /// <param name="mask"></param>
        private static int ExtractStatePortion(int state, int mask)
        {
            return state & mask;
        }
    }
}
