// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

namespace System.Threading 
{
    using System;
    using System.Security;
    using System.Security.Permissions;
    using Microsoft.Win32;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.Tracing;
    using Microsoft.Win32.SafeHandles;


        
    [System.Runtime.InteropServices.ComVisible(true)]
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
    // Note that all instance methods of this class require that the caller hold a lock on TimerQueue.Instance.
    //
    class TimerQueue
    {
        #region singleton pattern implementation

        // The one-and-only TimerQueue for the AppDomain.
        static TimerQueue s_queue = new TimerQueue();

        public static TimerQueue Instance
        {
            get { return s_queue; }
        }

        private TimerQueue()
        {
            // empty private constructor to ensure we remain a singleton.
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
            [SecuritySafeCritical]
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
        [SecurityCritical]
        class AppDomainTimerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public AppDomainTimerSafeHandle()
                : base(true)
            {
            }

            [SecurityCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            protected override bool ReleaseHandle()
            {
                return DeleteAppDomainTimer(handle);
            }
        }

        [SecurityCritical]
        AppDomainTimerSafeHandle m_appDomainTimer;

        bool m_isAppDomainTimerScheduled;
        int m_currentAppDomainTimerStartTicks;
        uint m_currentAppDomainTimerDuration;

        [SecuritySafeCritical]
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
            if(m_pauseTicks != 0)
            {
                Contract.Assert(!m_isAppDomainTimerScheduled);
                Contract.Assert(m_appDomainTimer == null);
                return true;
            }
 
            if (m_appDomainTimer == null || m_appDomainTimer.IsInvalid)
            {
                Contract.Assert(!m_isAppDomainTimerScheduled);

                m_appDomainTimer = CreateAppDomainTimer(actualDuration);
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
        // The VM calls this when the native timer fires.
        //
        [SecuritySafeCritical]
        internal static void AppDomainTimerCallback()
        {
            Instance.FireNextTimers();
        }

        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static extern AppDomainTimerSafeHandle CreateAppDomainTimer(uint dueTime);

        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static extern bool ChangeAppDomainTimer(AppDomainTimerSafeHandle handle, uint dueTime);

        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        static extern bool DeleteAppDomainTimer(IntPtr handle);

        #endregion

        #region Firing timers

        //
        // The list of timers
        //
        TimerQueueTimer m_timers;


        volatile int m_pauseTicks = 0; // Time when Pause was called

        [SecurityCritical]
        internal void Pause()
        {
            lock(this)
            {
                // Delete the native timer so that no timers are fired in the Pause zone
                if(m_appDomainTimer != null && !m_appDomainTimer.IsInvalid)
                {
                    m_appDomainTimer.Dispose();
                    m_appDomainTimer = null;
                    m_isAppDomainTimerScheduled = false;
                    m_pauseTicks = TickCount;
                }
            }
        }

        [SecurityCritical]
        internal void Resume()
        {
            //
            // Update timers to adjust their due-time to accomodate Pause/Resume
            //
            lock (this)
            {
                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    int pauseTicks = m_pauseTicks;
                    m_pauseTicks = 0; // Set this to 0 so that now timers can be scheduled

                    int resumedTicks = TickCount;
                    int pauseDuration = resumedTicks - pauseTicks;

                    bool haveTimerToSchedule = false;
                    uint nextAppDomainTimerDuration = uint.MaxValue;      
            
                    TimerQueueTimer timer = m_timers;
                    while (timer != null)
                    {
                        Contract.Assert(timer.m_dueTime != Timeout.UnsignedInfinite);
                        Contract.Assert(resumedTicks >= timer.m_startTicks);

                        uint elapsed; // How much of the timer dueTime has already elapsed

                        // Timers started before the paused event has to be sufficiently delayed to accomodate 
                        // for the Pause time. However, timers started after the Paused event shouldnt be adjusted. 
                        // E.g. ones created by the app in its Activated event should fire when it was designated.
                        // The Resumed event which is where this routine is executing is after this Activated and hence 
                        // shouldn't delay this timer

                        if(timer.m_startTicks <= pauseTicks)
                            elapsed = (uint)(pauseTicks - timer.m_startTicks);
                        else
                            elapsed = (uint)(resumedTicks - timer.m_startTicks);

                        // Handling the corner cases where a Timer was already due by the time Resume is happening,
                        // We shouldn't delay those timers. 
                        // Example is a timer started in App's Activated event with a very small duration
                        timer.m_dueTime = (timer.m_dueTime > elapsed) ? timer.m_dueTime - elapsed : 0;;
                        timer.m_startTicks = resumedTicks; // re-baseline

                        if (timer.m_dueTime < nextAppDomainTimerDuration)
                        {
                            haveTimerToSchedule = true;
                            nextAppDomainTimerDuration = timer.m_dueTime;
                        }

                        timer = timer.m_next;
                    }
                    
                    if (haveTimerToSchedule)
                    {
                        EnsureAppDomainTimerFiresBy(nextAppDomainTimerDuration);
                    }
                }
            }
        }


        //
        // Fire any timers that have expired, and update the native timer to schedule the rest of them.
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
                        Contract.Assert(timer.m_dueTime != Timeout.UnsignedInfinite);

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
                                timer.m_dueTime = timer.m_period;

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

        [SecuritySafeCritical]
        private static void QueueTimerCompletion(TimerQueueTimer timer)
        {
            WaitCallback callback = s_fireQueuedTimerCompletion;
            if (callback == null)
                s_fireQueuedTimerCompletion = callback = new WaitCallback(FireQueuedTimerCompletion);

            // Can use "unsafe" variant because we take care of capturing and restoring
            // the ExecutionContext.
            ThreadPool.UnsafeQueueUserWorkItem(callback, timer);
        }

        private static WaitCallback s_fireQueuedTimerCompletion;

        private static void FireQueuedTimerCompletion(object state)
        {
            ((TimerQueueTimer)state).Fire();
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
    sealed class TimerQueueTimer
    {
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
        readonly TimerCallback m_timerCallback;
        readonly Object m_state;
        readonly ExecutionContext m_executionContext;


        //
        // When Timer.Dispose(WaitHandle) is used, we need to signal the wait handle only
        // after all pending callbacks are complete.  We set m_canceled to prevent any callbacks that
        // are already queued from running.  We track the number of callbacks currently executing in 
        // m_callbacksRunning.  We set m_notifyWhenNoCallbacksRunning only when m_callbacksRunning
        // reaches zero.
        //
        int m_callbacksRunning;
        volatile bool m_canceled;
        volatile WaitHandle m_notifyWhenNoCallbacksRunning;


        [SecurityCritical]
        internal TimerQueueTimer(TimerCallback timerCallback, object state, uint dueTime, uint period, ref StackCrawlMark stackMark)
        {
            m_timerCallback = timerCallback;
            m_state = state;
            m_dueTime = Timeout.UnsignedInfinite;
            m_period = Timeout.UnsignedInfinite;

            if (!ExecutionContext.IsFlowSuppressed())
            {
                m_executionContext = ExecutionContext.Capture(
                    ref stackMark,
                    ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
            }

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

            lock (TimerQueue.Instance)
            {
                if (m_canceled)
                    throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));

                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    m_period = period;

                    if (dueTime == Timeout.UnsignedInfinite)
                    {
                        TimerQueue.Instance.DeleteTimer(this);
                        success = true;
                    }
                    else
                    {
                        if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                            FrameworkEventSource.Log.ThreadTransferSendObj(this, 1, string.Empty, true);

                        success = TimerQueue.Instance.UpdateTimer(this, dueTime, period);
                    }
                }
            }

            return success;
        }


        public void Close()
        {
            lock (TimerQueue.Instance)
            {
                // prevent ThreadAbort while updating state
                try { }
                finally
                {
                    if (!m_canceled)
                    {
                        m_canceled = true;
                        TimerQueue.Instance.DeleteTimer(this);
                    }
                }
            }
        }


        public bool Close(WaitHandle toSignal)
        {
            bool success;
            bool shouldSignal = false;

            lock (TimerQueue.Instance)
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
                        TimerQueue.Instance.DeleteTimer(this);

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

            lock (TimerQueue.Instance)
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
            lock (TimerQueue.Instance)
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

        [SecuritySafeCritical]
        internal void SignalNoCallbacksRunning()
        {
            Win32Native.SetEvent(m_notifyWhenNoCallbacksRunning.SafeWaitHandle);
        }

        [SecuritySafeCritical]
        internal void CallCallback()
        {
            if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                FrameworkEventSource.Log.ThreadTransferReceiveObj(this, 1, string.Empty);

            // call directly if EC flow is suppressed
            if (m_executionContext == null)
            {
                m_timerCallback(m_state);
            }
            else
            {
                using (ExecutionContext executionContext = 
                    m_executionContext.IsPreAllocatedDefault ? m_executionContext : m_executionContext.CreateCopy())
                {
                    ContextCallback callback = s_callCallbackInContext;
                    if (callback == null)
                        s_callCallbackInContext = callback = new ContextCallback(CallCallbackInContext);
                    
                    ExecutionContext.Run(
                        executionContext,
                        callback,
                        this,  // state
                        true); // ignoreSyncCtx
                }
            }
        }

        [SecurityCritical]
        private static ContextCallback s_callCallbackInContext;

        [SecurityCritical]
        private static void CallCallbackInContext(object state)
        {
            TimerQueueTimer t = (TimerQueueTimer)state;
            t.m_timerCallback(t.m_state);
        }
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
    sealed class TimerHolder
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


    [HostProtection(Synchronization=true, ExternalThreading=true)]
    [System.Runtime.InteropServices.ComVisible(true)]
#if FEATURE_REMOTING
    public sealed class Timer : MarshalByRefObject, IDisposable
#else // FEATURE_REMOTING
    public sealed class Timer : IDisposable
#endif // FEATURE_REMOTING
    {
        private const UInt32 MAX_SUPPORTED_TIMEOUT = (uint)0xfffffffe;

        private TimerHolder m_timer;

        [SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public Timer(TimerCallback callback, 
                     Object        state,  
                     int           dueTime,
                     int           period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (period < -1 )
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            TimerSetup(callback,state,(UInt32)dueTime,(UInt32)period,ref stackMark);
        }

        [SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public Timer(TimerCallback callback, 
                     Object        state,  
                     TimeSpan      dueTime,
                     TimeSpan      period)
        {                
            long dueTm = (long)dueTime.TotalMilliseconds;
            if (dueTm < -1)
                throw new ArgumentOutOfRangeException("dueTm",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (dueTm > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("dueTm",Environment.GetResourceString("ArgumentOutOfRange_TimeoutTooLarge"));

            long periodTm = (long)period.TotalMilliseconds;
            if (periodTm < -1)
                throw new ArgumentOutOfRangeException("periodTm",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (periodTm > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("periodTm",Environment.GetResourceString("ArgumentOutOfRange_PeriodTooLarge"));

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback,state,(UInt32)dueTm,(UInt32)periodTm,ref stackMark);
        }

        [CLSCompliant(false)]
        [SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public Timer(TimerCallback callback, 
                     Object        state,  
                     UInt32        dueTime,
                     UInt32        period)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback,state,dueTime,period,ref stackMark);
        }

        [SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable                                        
        public Timer(TimerCallback callback, 
                     Object        state,  
                     long          dueTime,
                     long          period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException("dueTime",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (period < -1)
                throw new ArgumentOutOfRangeException("period",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (dueTime > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("dueTime",Environment.GetResourceString("ArgumentOutOfRange_TimeoutTooLarge"));
            if (period > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("period",Environment.GetResourceString("ArgumentOutOfRange_PeriodTooLarge"));
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback,state,(UInt32) dueTime, (UInt32) period,ref stackMark);
        }

        [SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public Timer(TimerCallback callback)
        {
            int dueTime = -1;    // we want timer to be registered, but not activated.  Requires caller to call
            int period = -1;    // Change after a timer instance is created.  This is to avoid the potential
                                // for a timer to be fired before the returned value is assigned to the variable,
                                // potentially causing the callback to reference a bogus value (if passing the timer to the callback). 
            
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback, this, (UInt32)dueTime, (UInt32)period, ref stackMark);
        }

        [SecurityCritical]
        private void TimerSetup(TimerCallback callback,
                                Object state, 
                                UInt32 dueTime,
                                UInt32 period,
                                ref StackCrawlMark stackMark)
        {
            if (callback == null)
                throw new ArgumentNullException("TimerCallback");
            Contract.EndContractBlock();

            m_timer = new TimerHolder(new TimerQueueTimer(callback, state, dueTime, period, ref stackMark));
        }

        [SecurityCritical]
        internal static void Pause()
        {
            TimerQueue.Instance.Pause();
        }

        [SecurityCritical]
        internal static void Resume()
        {
            TimerQueue.Instance.Resume();
        }
     
        public bool Change(int dueTime, int period)
        {
            if (dueTime < -1 )
                throw new ArgumentOutOfRangeException("dueTime",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (period < -1)
                throw new ArgumentOutOfRangeException("period",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            Contract.EndContractBlock();

            return m_timer.m_timer.Change((UInt32)dueTime, (UInt32)period);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return Change((long) dueTime.TotalMilliseconds, (long) period.TotalMilliseconds);
        }

        [CLSCompliant(false)]
        public bool Change(UInt32 dueTime, UInt32 period)
        {
            return m_timer.m_timer.Change(dueTime, period);
        }

        public bool Change(long dueTime, long period)
        {
            if (dueTime < -1 )
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (period < -1)
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (dueTime > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_TimeoutTooLarge"));
            if (period > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_PeriodTooLarge"));
            Contract.EndContractBlock();

            return m_timer.m_timer.Change((UInt32)dueTime, (UInt32)period);
        }
    
        public bool Dispose(WaitHandle notifyObject)
        {
            if (notifyObject==null)
                throw new ArgumentNullException("notifyObject");
            Contract.EndContractBlock();

            return m_timer.Close(notifyObject);
        }
         
        public void Dispose()
        {
            m_timer.Close();
        }

        internal void KeepRootedWhileScheduled()
        {
            GC.SuppressFinalize(m_timer);
        }
    }
}
