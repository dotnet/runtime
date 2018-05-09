// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Win32.SafeHandles;

using Internal.Runtime.Augments;

namespace System.Threading
{
    public delegate void TimerCallback(Object state);

    //
    // TimerQueue maintains a list of active timers in this AppDomain.  We use a single native timer, supplied by the VM,
    // to schedule all managed timers in the AppDomain.
    //
    // Perf assumptions:  We assume that timers are created and destroyed frequently, but rarely actually fire.
    // There are roughly two types of timer:
    //
    //  - timeouts for operations.  These are created and destroyed very frequently, but almost never fire, because
    //    the whole point is that the timer only fires if something has gone wrong.
    //
    //  - scheduled background tasks.  These typically do fire, but they usually have quite long durations.
    //    So the impact of spending a few extra cycles to fire these is negligible.
    //
    // Because of this, we want to choose a data structure with very fast insert and delete times, but we can live
    // with linear traversal times when firing timers.
    //
    // The data structure we've chosen is an unordered doubly-linked list of active timers.  This gives O(1) insertion
    // and removal, and O(N) traversal when finding expired timers.
    //
    // Note that all instance methods of this class require that the caller hold a lock on the TimerQueue instance.
    //
    internal class TimerQueue
    {
        #region Shared TimerQueue instances

        public static TimerQueue[] Instances { get; } = CreateTimerQueues();

        private TimerQueue(int id)
        {
            m_id = id;
        }

        private static TimerQueue[] CreateTimerQueues()
        {
            var queues = new TimerQueue[Environment.ProcessorCount];
            for (int i = 0; i < queues.Length; i++)
            {
                queues[i] = new TimerQueue(i);
            }
            return queues;
        }

        #endregion

        #region interface to native per-AppDomain timer

        //
        // We need to keep our notion of time synchronized with the calls to SleepEx that drive
        // the underlying native timer.  In Win8, SleepEx does not count the time the machine spends
        // sleeping/hibernating.  Environment.TickCount (GetTickCount) *does* count that time,
        // so we will get out of sync with SleepEx if we use that method.
        //
        // So, on Win8, we use QueryUnbiasedInterruptTime instead; this does not count time spent
        // in sleep/hibernate mode.
        //
        private static int TickCount
        {
            get
            {
#if !FEATURE_PAL
                if (Environment.IsWindows8OrAbove)
                {
                    ulong time100ns;

                    bool result = Win32Native.QueryUnbiasedInterruptTime(out time100ns);
                    if (!result)
                        throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());

                    // convert to 100ns to milliseconds, and truncate to 32 bits.
                    return (int)(uint)(time100ns / 10000);
                }
                else
#endif
                {
                    return Environment.TickCount;
                }
            }
        }

        //
        // We use a SafeHandle to ensure that the native timer is destroyed when the AppDomain is unloaded.
        //
        private class AppDomainTimerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public AppDomainTimerSafeHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return DeleteAppDomainTimer(handle);
            }
        }

        private readonly int m_id; // TimerQueues[m_id] == this
        private AppDomainTimerSafeHandle m_appDomainTimer;

        private bool m_isAppDomainTimerScheduled;
        private int m_currentAppDomainTimerStartTicks;
        private uint m_currentAppDomainTimerDuration;

        private bool EnsureAppDomainTimerFiresBy(uint requestedDuration)
        {
            //
            // The VM's timer implementation does not work well for very long-duration timers.
            // See kb 950807.
            // So we'll limit our native timer duration to a "small" value.
            // This may cause us to attempt to fire timers early, but that's ok - 
            // we'll just see that none of our timers has actually reached its due time,
            // and schedule the native timer again.
            //
            const uint maxPossibleDuration = 0x0fffffff;
            uint actualDuration = Math.Min(requestedDuration, maxPossibleDuration);

            if (m_isAppDomainTimerScheduled)
            {
                uint elapsed = (uint)(TickCount - m_currentAppDomainTimerStartTicks);
                if (elapsed >= m_currentAppDomainTimerDuration)
                    return true; //the timer's about to fire

                uint remainingDuration = m_currentAppDomainTimerDuration - elapsed;
                if (actualDuration >= remainingDuration)
                    return true; //the timer will fire earlier than this request
            }

            // If Pause is underway then do not schedule the timers
            // A later update during resume will re-schedule
            if (m_pauseTicks != 0)
            {
                Debug.Assert(!m_isAppDomainTimerScheduled);
                Debug.Assert(m_appDomainTimer == null);
                return true;
            }

            if (m_appDomainTimer == null || m_appDomainTimer.IsInvalid)
            {
                Debug.Assert(!m_isAppDomainTimerScheduled);
                Debug.Assert(m_id >= 0 && m_id < Instances.Length && this == Instances[m_id]);

                m_appDomainTimer = CreateAppDomainTimer(actualDuration, m_id);
                if (!m_appDomainTimer.IsInvalid)
                {
                    m_isAppDomainTimerScheduled = true;
                    m_currentAppDomainTimerStartTicks = TickCount;
                    m_currentAppDomainTimerDuration = actualDuration;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (ChangeAppDomainTimer(m_appDomainTimer, actualDuration))
                {
                    m_isAppDomainTimerScheduled = true;
                    m_currentAppDomainTimerStartTicks = TickCount;
                    m_currentAppDomainTimerDuration = actualDuration;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        //
        // The VM calls this when a native timer fires.
        //
        internal static void AppDomainTimerCallback(int id)
        {
            Debug.Assert(id >= 0 && id < Instances.Length && Instances[id].m_id == id);
            Instances[id].FireNextTimers();
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern AppDomainTimerSafeHandle CreateAppDomainTimer(uint dueTime, int id);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool ChangeAppDomainTimer(AppDomainTimerSafeHandle handle, uint dueTime);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool DeleteAppDomainTimer(IntPtr handle);

        #endregion

        #region Firing timers

        //
        // The list of timers
        //
        private TimerQueueTimer m_timers;


        private volatile int m_pauseTicks = 0; // Time when Pause was called


        //
        // Fire any timers that have expired, and update the native timer to schedule the rest of them.
        // We're in a thread pool work item here, and if there are multiple timers to be fired, we want
        // to queue all but the first one.  The first may can then be invoked synchronously or queued,
        // a task left up to our caller, which might be firing timers from multiple queues.
        //
        private void FireNextTimers()
        {
            //
            // we fire the first timer on this thread; any other timers that might have fired are queued
            // to the ThreadPool.
            //
            TimerQueueTimer timerToFireOnThisThread = null;

            lock (this)
            {
                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    //
                    // since we got here, that means our previous timer has fired.
                    //
                    m_isAppDomainTimerScheduled = false;
                    bool haveTimerToSchedule = false;
                    uint nextAppDomainTimerDuration = uint.MaxValue;

                    int nowTicks = TickCount;

                    //
                    // Sweep through all timers.  The ones that have reached their due time
                    // will fire.  We will calculate the next native timer due time from the
                    // other timers.
                    //
                    TimerQueueTimer timer = m_timers;
                    while (timer != null)
                    {
                        Debug.Assert(timer.m_dueTime != Timeout.UnsignedInfinite);

                        uint elapsed = (uint)(nowTicks - timer.m_startTicks);
                        if (elapsed >= timer.m_dueTime)
                        {
                            //
                            // Remember the next timer in case we delete this one
                            //
                            TimerQueueTimer nextTimer = timer.m_next;

                            if (timer.m_period != Timeout.UnsignedInfinite)
                            {
                                timer.m_startTicks = nowTicks;
                                uint elapsedForNextDueTime = elapsed - timer.m_dueTime;
                                if (elapsedForNextDueTime < timer.m_period)
                                {
                                    // Discount the extra amount of time that has elapsed since the previous firing time to
                                    // prevent timer ticks from drifting
                                    timer.m_dueTime = timer.m_period - elapsedForNextDueTime;
                                }
                                else
                                {
                                    // Enough time has elapsed to fire the timer yet again. The timer is not able to keep up
                                    // with the short period, have it fire 1 ms from now to avoid spinning without a delay.
                                    timer.m_dueTime = 1;
                                }

                                //
                                // This is a repeating timer; schedule it to run again.
                                //
                                if (timer.m_dueTime < nextAppDomainTimerDuration)
                                {
                                    haveTimerToSchedule = true;
                                    nextAppDomainTimerDuration = timer.m_dueTime;
                                }
                            }
                            else
                            {
                                //
                                // Not repeating; remove it from the queue
                                //
                                DeleteTimer(timer);
                            }

                            //
                            // If this is the first timer, we'll fire it on this thread.  Otherwise, queue it
                            // to the ThreadPool.
                            //
                            if (timerToFireOnThisThread == null)
                                timerToFireOnThisThread = timer;
                            else
                                QueueTimerCompletion(timer);

                            timer = nextTimer;
                        }
                        else
                        {
                            //
                            // This timer hasn't fired yet.  Just update the next time the native timer fires.
                            //
                            uint remaining = timer.m_dueTime - elapsed;
                            if (remaining < nextAppDomainTimerDuration)
                            {
                                haveTimerToSchedule = true;
                                nextAppDomainTimerDuration = remaining;
                            }
                            timer = timer.m_next;
                        }
                    }

                    if (haveTimerToSchedule)
                        EnsureAppDomainTimerFiresBy(nextAppDomainTimerDuration);
                }
            }

            //
            // Fire the user timer outside of the lock!
            //
            if (timerToFireOnThisThread != null)
                timerToFireOnThisThread.Fire();
        }

        private static void QueueTimerCompletion(TimerQueueTimer timer)
        {
            // Can use "unsafe" variant because we take care of capturing and restoring the ExecutionContext.
            ThreadPool.UnsafeQueueCustomWorkItem(timer, forceGlobal: true);
        }

        #endregion

        #region Queue implementation

        public bool UpdateTimer(TimerQueueTimer timer, uint dueTime, uint period)
        {
            if (timer.m_dueTime == Timeout.UnsignedInfinite)
            {
                // the timer is not in the list; add it (as the head of the list).
                timer.m_next = m_timers;
                timer.m_prev = null;
                if (timer.m_next != null)
                    timer.m_next.m_prev = timer;
                m_timers = timer;
            }
            timer.m_dueTime = dueTime;
            timer.m_period = (period == 0) ? Timeout.UnsignedInfinite : period;
            timer.m_startTicks = TickCount;
            return EnsureAppDomainTimerFiresBy(dueTime);
        }

        public void DeleteTimer(TimerQueueTimer timer)
        {
            if (timer.m_dueTime != Timeout.UnsignedInfinite)
            {
                if (timer.m_next != null)
                    timer.m_next.m_prev = timer.m_prev;
                if (timer.m_prev != null)
                    timer.m_prev.m_next = timer.m_next;
                if (m_timers == timer)
                    m_timers = timer.m_next;

                timer.m_dueTime = Timeout.UnsignedInfinite;
                timer.m_period = Timeout.UnsignedInfinite;
                timer.m_startTicks = 0;
                timer.m_prev = null;
                timer.m_next = null;
            }
        }

        #endregion
    }

    //
    // A timer in our TimerQueue.
    //
    internal sealed class TimerQueueTimer : IThreadPoolWorkItem
    {
        //
        // The associated timer queue.
        //
        private readonly TimerQueue m_associatedTimerQueue;

        //
        // All fields of this class are protected by a lock on TimerQueue.Instance.
        //
        // The first four fields are maintained by TimerQueue itself.
        //
        internal TimerQueueTimer m_next;
        internal TimerQueueTimer m_prev;

        //
        // The time, according to TimerQueue.TickCount, when this timer's current interval started.
        //
        internal int m_startTicks;

        //
        // Timeout.UnsignedInfinite if we are not going to fire.  Otherwise, the offset from m_startTime when we will fire.
        //
        internal uint m_dueTime;

        //
        // Timeout.UnsignedInfinite if we are a single-shot timer.  Otherwise, the repeat interval.
        //
        internal uint m_period;

        //
        // Info about the user's callback
        //
        private readonly TimerCallback m_timerCallback;
        private readonly Object m_state;
        private readonly ExecutionContext m_executionContext;


        //
        // When Timer.Dispose(WaitHandle) is used, we need to signal the wait handle only
        // after all pending callbacks are complete.  We set m_canceled to prevent any callbacks that
        // are already queued from running.  We track the number of callbacks currently executing in 
        // m_callbacksRunning.  We set m_notifyWhenNoCallbacksRunning only when m_callbacksRunning
        // reaches zero.
        //
        private int m_callbacksRunning;
        private volatile bool m_canceled;
        private volatile WaitHandle m_notifyWhenNoCallbacksRunning;


        internal TimerQueueTimer(TimerCallback timerCallback, object state, uint dueTime, uint period)
        {
            m_timerCallback = timerCallback;
            m_state = state;
            m_dueTime = Timeout.UnsignedInfinite;
            m_period = Timeout.UnsignedInfinite;
            m_executionContext = ExecutionContext.Capture();
            m_associatedTimerQueue = TimerQueue.Instances[RuntimeThread.GetCurrentProcessorId() % TimerQueue.Instances.Length];

            //
            // After the following statement, the timer may fire.  No more manipulation of timer state outside of
            // the lock is permitted beyond this point!
            //
            if (dueTime != Timeout.UnsignedInfinite)
                Change(dueTime, period);
        }

        internal bool Change(uint dueTime, uint period)
        {
            bool success;

            lock (m_associatedTimerQueue)
            {
                if (m_canceled)
                    throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);

                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    m_period = period;

                    if (dueTime == Timeout.UnsignedInfinite)
                    {
                        m_associatedTimerQueue.DeleteTimer(this);
                        success = true;
                    }
                    else
                    {
                        if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                            FrameworkEventSource.Log.ThreadTransferSendObj(this, 1, string.Empty, true);

                        success = m_associatedTimerQueue.UpdateTimer(this, dueTime, period);
                    }
                }
            }

            return success;
        }


        public void Close()
        {
            lock (m_associatedTimerQueue)
            {
                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    if (!m_canceled)
                    {
                        m_canceled = true;
                        m_associatedTimerQueue.DeleteTimer(this);
                    }
                }
            }
        }


        public bool Close(WaitHandle toSignal)
        {
            bool success;
            bool shouldSignal = false;

            lock (m_associatedTimerQueue)
            {
                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    if (m_canceled)
                    {
                        success = false;
                    }
                    else
                    {
                        m_canceled = true;
                        m_notifyWhenNoCallbacksRunning = toSignal;
                        m_associatedTimerQueue.DeleteTimer(this);

                        if (m_callbacksRunning == 0)
                            shouldSignal = true;

                        success = true;
                    }
                }
            }

            if (shouldSignal)
                SignalNoCallbacksRunning();

            return success;
        }


        internal void Fire()
        {
            bool canceled = false;

            lock (m_associatedTimerQueue)
            {
                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    canceled = m_canceled;
                    if (!canceled)
                        m_callbacksRunning++;
                }
            }

            if (canceled)
                return;

            CallCallback();

            bool shouldSignal = false;
            lock (m_associatedTimerQueue)
            {
                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    m_callbacksRunning--;
                    if (m_canceled && m_callbacksRunning == 0 && m_notifyWhenNoCallbacksRunning != null)
                        shouldSignal = true;
                }
            }

            if (shouldSignal)
                SignalNoCallbacksRunning();
        }

        void IThreadPoolWorkItem.ExecuteWorkItem() => Fire();

        void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae) { }

        internal void SignalNoCallbacksRunning()
        {
            Win32Native.SetEvent(m_notifyWhenNoCallbacksRunning.SafeWaitHandle);
        }

        internal void CallCallback()
        {
            if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                FrameworkEventSource.Log.ThreadTransferReceiveObj(this, 1, string.Empty);

            // call directly if EC flow is suppressed
            ExecutionContext context = m_executionContext;
            if (context == null)
            {
                m_timerCallback(m_state);
            }
            else
            {
                ExecutionContext.RunInternal(context, s_callCallbackInContext, this);
            }
        }

        private static readonly ContextCallback s_callCallbackInContext = state =>
        {
            TimerQueueTimer t = (TimerQueueTimer)state;
            t.m_timerCallback(t.m_state);
        };
    }

    //
    // TimerHolder serves as an intermediary between Timer and TimerQueueTimer, releasing the TimerQueueTimer 
    // if the Timer is collected.
    // This is necessary because Timer itself cannot use its finalizer for this purpose.  If it did,
    // then users could control timer lifetimes using GC.SuppressFinalize/ReRegisterForFinalize.
    // You might ask, wouldn't that be a good thing?  Maybe (though it would be even better to offer this
    // via first-class APIs), but Timer has never offered this, and adding it now would be a breaking
    // change, because any code that happened to be suppressing finalization of Timer objects would now
    // unwittingly be changing the lifetime of those timers.
    //
    internal sealed class TimerHolder
    {
        internal TimerQueueTimer m_timer;

        public TimerHolder(TimerQueueTimer timer)
        {
            m_timer = timer;
        }

        ~TimerHolder()
        {
            //
            // If shutdown has started, another thread may be suspended while holding the timer lock.
            // So we can't safely close the timer.  
            //
            // Similarly, we should not close the timer during AD-unload's live-object finalization phase.
            // A rude abort may have prevented us from releasing the lock.
            //
            // Note that in either case, the Timer still won't fire, because ThreadPool threads won't be
            // allowed to run in this AppDomain.
            //
            if (Environment.HasShutdownStarted || AppDomain.CurrentDomain.IsFinalizingForUnload())
                return;

            m_timer.Close();
        }

        public void Close()
        {
            m_timer.Close();
            GC.SuppressFinalize(this);
        }

        public bool Close(WaitHandle notifyObject)
        {
            bool result = m_timer.Close(notifyObject);
            GC.SuppressFinalize(this);
            return result;
        }
    }


    public sealed class Timer : MarshalByRefObject, IDisposable
    {
        private const UInt32 MAX_SUPPORTED_TIMEOUT = (uint)0xfffffffe;

        private TimerHolder m_timer;

        public Timer(TimerCallback callback,
                     Object state,
                     int dueTime,
                     int period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (period < -1)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            TimerSetup(callback, state, (UInt32)dueTime, (UInt32)period);
        }

        public Timer(TimerCallback callback,
                     Object state,
                     TimeSpan dueTime,
                     TimeSpan period)
        {
            long dueTm = (long)dueTime.TotalMilliseconds;
            if (dueTm < -1)
                throw new ArgumentOutOfRangeException(nameof(dueTm), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (dueTm > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException(nameof(dueTm), SR.ArgumentOutOfRange_TimeoutTooLarge);

            long periodTm = (long)period.TotalMilliseconds;
            if (periodTm < -1)
                throw new ArgumentOutOfRangeException(nameof(periodTm), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (periodTm > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException(nameof(periodTm), SR.ArgumentOutOfRange_PeriodTooLarge);

            TimerSetup(callback, state, (UInt32)dueTm, (UInt32)periodTm);
        }

        [CLSCompliant(false)]
        public Timer(TimerCallback callback,
                     Object state,
                     UInt32 dueTime,
                     UInt32 period)
        {
            TimerSetup(callback, state, dueTime, period);
        }

        public Timer(TimerCallback callback,
                     Object state,
                     long dueTime,
                     long period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (period < -1)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (dueTime > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_TimeoutTooLarge);
            if (period > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_PeriodTooLarge);
            TimerSetup(callback, state, (UInt32)dueTime, (UInt32)period);
        }

        public Timer(TimerCallback callback)
        {
            int dueTime = -1;    // we want timer to be registered, but not activated.  Requires caller to call
            int period = -1;    // Change after a timer instance is created.  This is to avoid the potential
                                // for a timer to be fired before the returned value is assigned to the variable,
                                // potentially causing the callback to reference a bogus value (if passing the timer to the callback). 

            TimerSetup(callback, this, (UInt32)dueTime, (UInt32)period);
        }

        private void TimerSetup(TimerCallback callback,
                                Object state,
                                UInt32 dueTime,
                                UInt32 period)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(TimerCallback));

            m_timer = new TimerHolder(new TimerQueueTimer(callback, state, dueTime, period));
        }

        public bool Change(int dueTime, int period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (period < -1)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            return m_timer.m_timer.Change((UInt32)dueTime, (UInt32)period);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return Change((long)dueTime.TotalMilliseconds, (long)period.TotalMilliseconds);
        }

        [CLSCompliant(false)]
        public bool Change(UInt32 dueTime, UInt32 period)
        {
            return m_timer.m_timer.Change(dueTime, period);
        }

        public bool Change(long dueTime, long period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (period < -1)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (dueTime > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_TimeoutTooLarge);
            if (period > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_PeriodTooLarge);

            return m_timer.m_timer.Change((UInt32)dueTime, (UInt32)period);
        }

        public bool Dispose(WaitHandle notifyObject)
        {
            if (notifyObject == null)
                throw new ArgumentNullException(nameof(notifyObject));

            return m_timer.Close(notifyObject);
        }

        public void Dispose()
        {
            m_timer.Close();
        }
    }
}
