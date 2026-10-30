// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public partial class ManualResetEventSlim : IDisposable
    {

        // A lock used for waiting and pulsing. Lazily initialized via EnsureLockObjectCreated()
        // We are not using thin/object lock here since our use pattern makes it nearly certain that
        // a thin lock would be inflated even if we use m_lock only once.
        private Lock? m_lock;

        // -- State -- //
        // For a packed word a uint would seem better, but Interlocked.* doesn't support them as uint isn't CLS-compliant.
        private int m_combinedState; // ie a uint. Used for the state items listed below.

        // 1-bit for  signalled state
        private const int SignalledState_BitMask = unchecked((int)0x80000000); // 1000 0000 0000 0000 0000 0000 0000 0000

        // 1-bit for disposed state
        private const int Dispose_BitMask = unchecked((int)0x40000000); // 0100 0000 0000 0000 0000 0000 0000 0000

        // 11-bits for m_spinCount
        private const int SpinCountState_BitMask = unchecked((int)0x3FF80000); // 0011 1111 1111 1000 0000 0000 0000 0000
        private const int SpinCountState_ShiftCount = 19;

        // 19-bits for m_waiters.  This allows support of 512K threads waiting which should be ample
        private const int NumWaitersState_BitMask = unchecked((int)0x0007FFFF); // 0000 0000 0000 0111 1111 1111 1111 1111
        private const int NumWaitersState_MaxValue = (1 << 19) - 1; // 512K-1

        private bool IsSetCore
        {
            get
            {
                // Volatile to make sure checking IsSet is not optimized away or reordered
                // when called as a public API.
                return Volatile.Read(ref m_combinedState) < 0;
            }
            set
            {
                if (value)
                {
                    Interlocked.Or(ref m_combinedState, SignalledState_BitMask);
                }
                else
                {
                    Interlocked.And(ref m_combinedState, ~SignalledState_BitMask);
                }
            }
        }

        private int SpinCountCore
        {
            get => ExtractStatePortionAndShiftRight(m_combinedState, SpinCountState_BitMask, SpinCountState_ShiftCount);
            set
            {
                Debug.Assert(value >= 0, "SpinCount is a restricted-width integer. The value supplied is outside the legal range.");
                Debug.Assert(value <= SpinCountState_MaxValue, "SpinCount is a restricted-width integer. The value supplied is outside the legal range.");
                // Don't worry about thread safety because it's set one time from the constructor
                m_combinedState = (m_combinedState & ~SpinCountState_BitMask) | (value << SpinCountState_ShiftCount);
            }
        }

        private void InterlockedIncrementWaiters()
        {
            int currentState = m_combinedState;
            while (true)
            {
                // it is possible for the max number of waiters to be exceeded via user-code, hence we use a real exception here.
                if ((currentState & NumWaitersState_BitMask) >= NumWaitersState_MaxValue)
                    throw new InvalidOperationException(SR.Format(SR.ManualResetEventSlim_ctor_TooManyWaiters, NumWaitersState_MaxValue));

                // The waiters count has room for one more, so adding 1 to the full state
                // cannot carry out of NumWaitersState_BitMask.
                int oldState = Interlocked.CompareExchange(ref m_combinedState, currentState + 1, currentState);
                if (oldState == currentState)
                    return;

                // Highly unlikely contention, since we increment/decrement while holding a lock, just try again.
                currentState = oldState;
            }
        }

        private void InterlockedDecrementWaiters()
        {
            // setting to <0 would indicate an internal flaw, hence Assert is appropriate.
            Debug.Assert((m_combinedState & NumWaitersState_BitMask) != 0, "NumWaiters should never be less than zero. This indicates an internal error.");

            Interlocked.Decrement(ref m_combinedState);
        }

        /// <summary>
        /// Initializes the internal state of the event.
        /// </summary>
        /// <param name="initialState">Whether the event is set initially or not.</param>
        /// <param name="spinCount">The spin count that decides when the event will block.</param>
#pragma warning disable IDE0060 // Remove unused parameter spinCount, on single-threaded systems, the spin count is not used.
        private void Initialize(bool initialState, int spinCount)
        {
            m_combinedState = initialState ? SignalledState_BitMask : 0;

            // the spinCount argument has been validated by the ctors.
            // but we now sanity check our predefined constants.
            Debug.Assert(DEFAULT_SPIN_SP >= 0, "Internal error - DEFAULT_SPIN_SP is outside the legal range.");
            Debug.Assert(DEFAULT_SPIN_SP <= SpinCountState_MaxValue, "Internal error - DEFAULT_SPIN_SP is outside the legal range.");

            SpinCount = Environment.IsSingleProcessor ? DEFAULT_SPIN_SP : spinCount;
        }
#pragma warning restore IDE0060

        /// <summary>
        /// Helper to ensure the lock object is created before first use.
        /// </summary>
        [MemberNotNull(nameof(m_lock))]
        private void EnsureLockObjectCreated()
        {
            if (m_lock is null)
            {
                Interlocked.CompareExchange(ref m_lock, new Lock(), null); // failure is benign. Someone else set the value.
            }
        }

        /// <summary>
        /// This method lazily initializes the public wait object. It uses CAS to guarantee that
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
                // the CAS above prevents this read
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
        /// Private helper to actually perform the Set.
        /// </summary>
        /// <param name="duringCancellation">Indicates whether we are calling Set() during cancellation.</param>
        /// <exception cref="OperationCanceledException">The object has been canceled.</exception>
        private void Set(bool duringCancellation)
        {
            // We need to ensure that the state change happens before we read the m_eventObj.
            // Interlocked guarantees that.
            long origState = Interlocked.Or(ref m_combinedState, SignalledState_BitMask);

            // If there were waiting threads when we flipped the state, we need to pulse them.
            if ((origState & NumWaitersState_BitMask) != 0)
            {
                Debug.Assert(m_lock != null); // if waiters>0, then m_lock has already been created.
                using (m_lock.EnterScope())
                {
                    m_lock.PulseAll();
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

        private void ResetCore()
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

#if !FEATURE_WASM_MANAGED_THREADS
        [UnsupportedOSPlatform("browser")]
#endif
        private bool WaitCore(int millisecondsTimeout, CancellationToken cancellationToken)
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

                RuntimeFeature.ThrowIfBlockingIsNotSupported(millisecondsTimeout);

                // We spin briefly before falling back to allocating and/or waiting on a true event.
                long startTime = 0;
                int realMillisecondsTimeout = millisecondsTimeout; // this will be adjusted if necessary.

                if (millisecondsTimeout != Timeout.Infinite)
                {
                    // We will account for time spent spinning, so that we can decrement it from our
                    // timeout.  In most cases the time spent in this section will be negligible.  But
                    // we can't discount the possibility of our thread being switched out for a lengthy
                    // period of time.  The timeout adjustments only take effect when and if we actually
                    // decide to block in the kernel below.

                    startTime = Environment.TickCount64;
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
                    // Loop to cope with spurious wakeups from other waits being canceled
                    while (!IsSet)
                    {
                        // If our token was canceled, we must throw and exit.
                        cancellationToken.ThrowIfCancellationRequested();

                        // update timeout (delays in wait commencement are due to spinning and/or spurious wakeups from other waits being canceled)
                        if (startTime != 0)
                        {
                            // TimeoutHelper.UpdateTimeOut returns a long but the value is capped as millisecondsTimeout is an int.
                            realMillisecondsTimeout = (int)TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
                            if (realMillisecondsTimeout <= 0)
                                return false;
                        }

                        using (m_lock.EnterScope())
                        {
                            // There is a race condition that Set will fail to see that there are waiters as Set does not take the lock,
                            // so after updating waiters, we must check IsSet again.
                            // Also, we must ensure there cannot be any reordering of the assignment to Waiters and the
                            // read from IsSet.  This is guaranteed as InterlockedIncrementWaiters is a full memory barrier.
                            // If we see IsSet=false, then we are guaranteed that Set() will see that we are
                            // waiting and will pulse the monitor correctly.

                            InterlockedIncrementWaiters();

                            if (IsSet) // This check must occur after updating Waiters.
                            {
                                InterlockedDecrementWaiters(); // revert the increment.
                                return true;
                            }

                            // Now finally perform the wait.
                            try
                            {
                                // ** the actual wait **
                                if (!m_lock.Wait(realMillisecondsTimeout))
                                    return false; // return immediately if the timeout has expired.
                            }
                            finally
                            {
                                // Clean up: we're done waiting.
                                InterlockedDecrementWaiters();
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

        private void DisposeCore(bool disposing)
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
            using (mre.m_lock.EnterScope())
            {
                mre.m_lock.PulseAll(); // awaken all waiters
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
    }
}
