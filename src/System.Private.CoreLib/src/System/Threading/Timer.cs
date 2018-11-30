// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Threading
{
    public delegate void TimerCallback(object state);

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
    // Because of this, we want to choose a data structure with very fast insert and delete times, and we can live
    // with linear traversal times when firing timers.  However, we still want to minimize the number of timers
    // we need to traverse while doing the linear walk: in cases where we have lots of long-lived timers as well as
    // lots of short-lived timers, when the short-lived timers fire, they incur the cost of walking the long-lived ones.
    //
    // The data structure we've chosen is an unordered doubly-linked list of active timers.  This gives O(1) insertion
    // and removal, and O(N) traversal when finding expired timers.  We maintain two such lists: one for all of the
    // timers that'll next fire within a certain threshold, and one for the rest.
    //
    // Note that all instance methods of this class require that the caller hold a lock on the TimerQueue instance.
    // We partition the timers across multiple TimerQueues, each with its own lock and set of short/long lists,
    // in order to minimize contention when lots of threads are concurrently creating and destroying timers often.
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

        private static int TickCount
        {
            get
            {
#if !FEATURE_PAL
                // We need to keep our notion of time synchronized with the calls to SleepEx that drive
                // the underlying native timer.  In Win8, SleepEx does not count the time the machine spends
                // sleeping/hibernating.  Environment.TickCount (GetTickCount) *does* count that time,
                // so we will get out of sync with SleepEx if we use that method.
                //
                // So, on Win8, we use QueryUnbiasedInterruptTime instead; this does not count time spent
                // in sleep/hibernate mode.
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

        // We use a SafeHandle to ensure that the native timer is destroyed when the AppDomain is unloaded.
        private sealed class AppDomainTimerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
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
            // The VM's timer implementation does not work well for very long-duration timers.
            // See kb 950807.
            // So we'll limit our native timer duration to a "small" value.
            // This may cause us to attempt to fire timers early, but that's ok - 
            // we'll just see that none of our timers has actually reached its due time,
            // and schedule the native timer again.
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

        // The VM calls this when a native timer fires.
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

        // The two lists of timers that are part of this TimerQueue.  They conform to a single guarantee:
        // no timer in m_longTimers has an absolute next firing time <= m_currentAbsoluteThreshold.
        // That way, when FireNextTimers is invoked, we always process the short list, and we then only
        // process the long list if the current time is greater than m_currentAbsoluteThreshold (or
        // if the short list is now empty and we need to process the long list to know when to next
        // invoke FireNextTimers).
        private TimerQueueTimer m_shortTimers;
        private TimerQueueTimer m_longTimers;

        // The current threshold, an absolute time where any timers scheduled to go off at or
        // before this time must be queued to the short list.
        private int m_currentAbsoluteThreshold = ShortTimersThresholdMilliseconds;

        // Default threshold that separates which timers target m_shortTimers vs m_longTimers. The threshold
        // is chosen to balance the number of timers in the small list against the frequency with which
        // we need to scan the long list.  It's thus somewhat arbitrary and could be changed based on
        // observed workload demand. The larger the number, the more timers we'll likely need to enumerate
        // every time the appdomain timer fires, but also the more likely it is that when it does we won't
        // need to look at the long list because the current time will be <= m_currentAbsoluteThreshold.
        private const int ShortTimersThresholdMilliseconds = 333;

        // Time when Pause was called
        private volatile int m_pauseTicks = 0;

        // Fire any timers that have expired, and update the native timer to schedule the rest of them.
        // We're in a thread pool work item here, and if there are multiple timers to be fired, we want
        // to queue all but the first one.  The first may can then be invoked synchronously or queued,
        // a task left up to our caller, which might be firing timers from multiple queues.
        private void FireNextTimers()
        {
            // We fire the first timer on this thread; any other timers that need to be fired
            // are queued to the ThreadPool.
            TimerQueueTimer timerToFireOnThisThread = null;

            lock (this)
            {
                // Since we got here, that means our previous appdomain timer has fired.
                m_isAppDomainTimerScheduled = false;
                bool haveTimerToSchedule = false;
                uint nextAppDomainTimerDuration = uint.MaxValue;

                int nowTicks = TickCount;

                // Sweep through the "short" timers.  If the current tick count is greater than
                // the current threshold, also sweep through the "long" timers.  Finally, as part
                // of sweeping the long timers, move anything that'll fire within the next threshold
                // to the short list.  It's functionally ok if more timers end up in the short list
                // than is truly necessary (but not the opposite).
                TimerQueueTimer timer = m_shortTimers;
                for (int listNum = 0; listNum < 2; listNum++) // short == 0, long == 1
                {
                    while (timer != null)
                    {
                        Debug.Assert(timer.m_dueTime != Timeout.UnsignedInfinite, "A timer in the list must have a valid due time.");

                        // Save off the next timer to examine, in case our examination of this timer results
                        // in our deleting or moving it; we'll continue after with this saved next timer.
                        TimerQueueTimer next = timer.m_next;

                        uint elapsed = (uint)(nowTicks - timer.m_startTicks);
                        int remaining = (int)timer.m_dueTime - (int)elapsed;
                        if (remaining <= 0)
                        {
                            // Timer is ready to fire.

                            if (timer.m_period != Timeout.UnsignedInfinite)
                            {
                                // This is a repeating timer; schedule it to run again.

                                // Discount the extra amount of time that has elapsed since the previous firing time to
                                // prevent timer ticks from drifting.  If enough time has already elapsed for the timer to fire
                                // again, meaning the timer can't keep up with the short period, have it fire 1 ms from now to
                                // avoid spinning without a delay.
                                timer.m_startTicks = nowTicks;
                                uint elapsedForNextDueTime = elapsed - timer.m_dueTime;
                                timer.m_dueTime = (elapsedForNextDueTime < timer.m_period) ?
                                    timer.m_period - elapsedForNextDueTime :
                                    1;

                                // Update the appdomain timer if this becomes the next timer to fire.
                                if (timer.m_dueTime < nextAppDomainTimerDuration)
                                {
                                    haveTimerToSchedule = true;
                                    nextAppDomainTimerDuration = timer.m_dueTime;
                                }

                                // Validate that the repeating timer is still on the right list.  It's likely that
                                // it started in the long list and was moved to the short list at some point, so
                                // we now want to move it back to the long list if that's where it belongs. Note that
                                // if we're currently processing the short list and move it to the long list, we may
                                // end up revisiting it again if we also enumerate the long list, but we will have already
                                // updated the due time appropriately so that we won't fire it again (it's also possible
                                // but rare that we could be moving a timer from the long list to the short list here,
                                // if the initial due time was set to be long but the timer then had a short period).
                                bool targetShortList = (nowTicks + timer.m_dueTime) - m_currentAbsoluteThreshold <= 0;
                                if (timer.m_short != targetShortList)
                                {
                                    MoveTimerToCorrectList(timer, targetShortList);
                                }
                            }
                            else
                            {
                                // Not repeating; remove it from the queue
                                DeleteTimer(timer);
                            }

                            // If this is the first timer, we'll fire it on this thread (after processing
                            // all others). Otherwise, queue it to the ThreadPool.
                            if (timerToFireOnThisThread == null)
                            {
                                timerToFireOnThisThread = timer;
                            }
                            else
                            {
                                ThreadPool.UnsafeQueueUserWorkItemInternal(timer, preferLocal: false);
                            }
                        }
                        else
                        {
                            // This timer isn't ready to fire.  Update the next time the native timer fires if necessary,
                            // and move this timer to the short list if its remaining time is now at or under the threshold.

                            if (remaining < nextAppDomainTimerDuration)
                            {
                                haveTimerToSchedule = true;
                                nextAppDomainTimerDuration = (uint)remaining;
                            }

                            if (!timer.m_short && remaining <= ShortTimersThresholdMilliseconds)
                            {
                                MoveTimerToCorrectList(timer, shortList: true);
                            }
                        }

                        timer = next;
                    }

                    // Switch to process the long list if necessary.
                    if (listNum == 0)
                    {
                        // Determine how much time remains between now and the current threshold.  If time remains,
                        // we can skip processing the long list.  We use > rather than >= because, although we
                        // know that if remaining == 0 no timers in the long list will need to be fired, we
                        // don't know without looking at them when we'll need to call FireNextTimers again.  We
                        // could in that case just set the next appdomain firing to 1, but we may as well just iterate the
                        // long list now; otherwise, most timers created in the interim would end up in the long
                        // list and we'd likely end up paying for another invocation of FireNextTimers that could
                        // have been delayed longer (to whatever is the current minimum in the long list).
                        int remaining = m_currentAbsoluteThreshold - nowTicks;
                        if (remaining > 0)
                        {
                            if (m_shortTimers == null && m_longTimers != null)
                            {
                                // We don't have any short timers left and we haven't examined the long list,
                                // which means we likely don't have an accurate nextAppDomainTimerDuration.
                                // But we do know that nothing in the long list will be firing before or at m_currentAbsoluteThreshold,
                                // so we can just set nextAppDomainTimerDuration to the difference between then and now.
                                nextAppDomainTimerDuration = (uint)remaining + 1;
                                haveTimerToSchedule = true;
                            }
                            break;
                        }

                        // Switch to processing the long list.
                        timer = m_longTimers;

                        // Now that we're going to process the long list, update the current threshold.
                        m_currentAbsoluteThreshold = nowTicks + ShortTimersThresholdMilliseconds;
                    }
                }

                // If we still have scheduled timers, update the appdomain timer to ensure it fires
                // in time for the next one in line.
                if (haveTimerToSchedule)
                {
                    EnsureAppDomainTimerFiresBy(nextAppDomainTimerDuration);
                }
            }

            // Fire the user timer outside of the lock!
            timerToFireOnThisThread?.Fire();
        }

        #endregion

        #region Queue implementation

        public bool UpdateTimer(TimerQueueTimer timer, uint dueTime, uint period)
        {
            int nowTicks = TickCount;

            // The timer can be put onto the short list if it's next absolute firing time
            // is <= the current absolute threshold.
            int absoluteDueTime = (int)(nowTicks + dueTime);
            bool shouldBeShort = m_currentAbsoluteThreshold - absoluteDueTime >= 0;

            if (timer.m_dueTime == Timeout.UnsignedInfinite)
            {
                // If the timer wasn't previously scheduled, now add it to the right list.
                timer.m_short = shouldBeShort;
                LinkTimer(timer);
            }
            else if (timer.m_short != shouldBeShort)
            {
                // If the timer was previously scheduled, but this update should cause
                // it to move over the list threshold in either direction, do so.
                UnlinkTimer(timer);
                timer.m_short = shouldBeShort;
                LinkTimer(timer);
            }

            timer.m_dueTime = dueTime;
            timer.m_period = (period == 0) ? Timeout.UnsignedInfinite : period;
            timer.m_startTicks = nowTicks;
            return EnsureAppDomainTimerFiresBy(dueTime);
        }

        public void MoveTimerToCorrectList(TimerQueueTimer timer, bool shortList)
        {
            Debug.Assert(timer.m_dueTime != Timeout.UnsignedInfinite, "Expected timer to be on a list.");
            Debug.Assert(timer.m_short != shortList, "Unnecessary if timer is already on the right list.");

            // Unlink it from whatever list it's on, change its list association, then re-link it.
            UnlinkTimer(timer);
            timer.m_short = shortList;
            LinkTimer(timer);
        }

        private void LinkTimer(TimerQueueTimer timer)
        {
            // Use timer.m_short to decide to which list to add.
            ref TimerQueueTimer listHead = ref timer.m_short ? ref m_shortTimers : ref m_longTimers;
            timer.m_next = listHead;
            if (timer.m_next != null)
            {
                timer.m_next.m_prev = timer;
            }
            timer.m_prev = null;
            listHead = timer;
        }

        private void UnlinkTimer(TimerQueueTimer timer)
        {
            TimerQueueTimer t = timer.m_next;
            if (t != null)
            {
                t.m_prev = timer.m_prev;
            }

            if (m_shortTimers == timer)
            {
                Debug.Assert(timer.m_short);
                m_shortTimers = t;
            }
            else if (m_longTimers == timer)
            {
                Debug.Assert(!timer.m_short);
                m_longTimers = t;
            }

            t = timer.m_prev;
            if (t != null)
            {
                t.m_next = timer.m_next;
            }

            // At this point the timer is no longer in a list, but its next and prev
            // references may still point to other nodes.  UnlinkTimer should thus be
            // followed by something that overwrites those references, either with null
            // if deleting the timer or other nodes if adding it to another list.
        }

        public void DeleteTimer(TimerQueueTimer timer)
        {
            if (timer.m_dueTime != Timeout.UnsignedInfinite)
            {
                UnlinkTimer(timer);
                timer.m_prev = null;
                timer.m_next = null;
                timer.m_dueTime = Timeout.UnsignedInfinite;
                timer.m_period = Timeout.UnsignedInfinite;
                timer.m_startTicks = 0;
                timer.m_short = false;
            }
        }

        #endregion
    }

    // A timer in our TimerQueue.
    internal sealed class TimerQueueTimer : IThreadPoolWorkItem
    {
        // The associated timer queue.
        private readonly TimerQueue m_associatedTimerQueue;

        // All mutable fields of this class are protected by a lock on m_associatedTimerQueue.
        // The first six fields are maintained by TimerQueue.

        // Links to the next and prev timers in the list.
        internal TimerQueueTimer m_next;
        internal TimerQueueTimer m_prev;

        // true if on the short list; otherwise, false.
        internal bool m_short;

        // The time, according to TimerQueue.TickCount, when this timer's current interval started.
        internal int m_startTicks;

        // Timeout.UnsignedInfinite if we are not going to fire.  Otherwise, the offset from m_startTime when we will fire.
        internal uint m_dueTime;

        // Timeout.UnsignedInfinite if we are a single-shot timer.  Otherwise, the repeat interval.
        internal uint m_period;

        // Info about the user's callback
        private readonly TimerCallback m_timerCallback;
        private readonly object m_state;
        private readonly ExecutionContext m_executionContext;

        // When Timer.Dispose(WaitHandle) is used, we need to signal the wait handle only
        // after all pending callbacks are complete.  We set m_canceled to prevent any callbacks that
        // are already queued from running.  We track the number of callbacks currently executing in 
        // m_callbacksRunning.  We set m_notifyWhenNoCallbacksRunning only when m_callbacksRunning
        // reaches zero.  Same applies if Timer.DisposeAsync() is used, except with a Task<bool>
        // instead of with a provided WaitHandle.
        private int m_callbacksRunning;
        private volatile bool m_canceled;
        private volatile object m_notifyWhenNoCallbacksRunning; // may be either WaitHandle or Task<bool>


        internal TimerQueueTimer(TimerCallback timerCallback, object state, uint dueTime, uint period, bool flowExecutionContext)
        {
            m_timerCallback = timerCallback;
            m_state = state;
            m_dueTime = Timeout.UnsignedInfinite;
            m_period = Timeout.UnsignedInfinite;
            if (flowExecutionContext)
            {
                m_executionContext = ExecutionContext.Capture();
            }
            m_associatedTimerQueue = TimerQueue.Instances[RuntimeThread.GetCurrentProcessorId() % TimerQueue.Instances.Length];

            // After the following statement, the timer may fire.  No more manipulation of timer state outside of
            // the lock is permitted beyond this point!
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

                m_period = period;

                if (dueTime == Timeout.UnsignedInfinite)
                {
                    m_associatedTimerQueue.DeleteTimer(this);
                    success = true;
                }
                else
                {
                    // Don't emit this event during EventPipeController.  This avoids initializing FrameworkEventSource during start-up which is expensive relative to the rest of start-up.
                    if (!EventPipeController.Initializing && FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                        FrameworkEventSource.Log.ThreadTransferSendObj(this, 1, string.Empty, true, (int)dueTime, (int)period);

                    success = m_associatedTimerQueue.UpdateTimer(this, dueTime, period);
                }
            }

            return success;
        }


        public void Close()
        {
            lock (m_associatedTimerQueue)
            {
                if (!m_canceled)
                {
                    m_canceled = true;
                    m_associatedTimerQueue.DeleteTimer(this);
                }
            }
        }


        public bool Close(WaitHandle toSignal)
        {
            bool success;
            bool shouldSignal = false;

            lock (m_associatedTimerQueue)
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
                    shouldSignal = m_callbacksRunning == 0;
                    success = true;
                }
            }

            if (shouldSignal)
                SignalNoCallbacksRunning();

            return success;
        }

        public ValueTask CloseAsync()
        {
            lock (m_associatedTimerQueue)
            {
                object notifyWhenNoCallbacksRunning = m_notifyWhenNoCallbacksRunning;

                // Mark the timer as canceled if it's not already.
                if (m_canceled)
                {
                    if (notifyWhenNoCallbacksRunning is WaitHandle)
                    {
                        // A previous call to Close(WaitHandle) stored a WaitHandle.  We could try to deal with
                        // this case by using ThreadPool.RegisterWaitForSingleObject to create a Task that'll
                        // complete when the WaitHandle is set, but since arbitrary WaitHandle's can be supplied
                        // by the caller, it could be for an auto-reset event or similar where that caller's
                        // WaitOne on the WaitHandle could prevent this wrapper Task from completing.  We could also
                        // change the implementation to support storing multiple objects, but that's not pay-for-play,
                        // and the existing Close(WaitHandle) already discounts this as being invalid, instead just
                        // returning false if you use it multiple times. Since first calling Timer.Dispose(WaitHandle)
                        // and then calling Timer.DisposeAsync is not something anyone is likely to or should do, we
                        // simplify by just failing in that case.
                        return new ValueTask(Task.FromException(new InvalidOperationException(SR.InvalidOperation_TimerAlreadyClosed)));
                    }
                }
                else
                {
                    m_canceled = true;
                    m_associatedTimerQueue.DeleteTimer(this);
                }

                // We've deleted the timer, so if there are no callbacks queued or running,
                // we're done and return an already-completed value task.
                if (m_callbacksRunning == 0)
                {
                    return default;
                }

                Debug.Assert(
                    notifyWhenNoCallbacksRunning == null ||
                    notifyWhenNoCallbacksRunning is Task<bool>);

                // There are callbacks queued or running, so we need to store a Task<bool>
                // that'll be used to signal the caller when all callbacks complete. Do so as long as
                // there wasn't a previous CloseAsync call that did.
                if (notifyWhenNoCallbacksRunning == null)
                {
                    var t = new Task<bool>((object)null, TaskCreationOptions.RunContinuationsAsynchronously);
                    m_notifyWhenNoCallbacksRunning = t;
                    return new ValueTask(t);
                }

                // A previous CloseAsync call already hooked up a task.  Just return it.
                return new ValueTask((Task<bool>)notifyWhenNoCallbacksRunning);
            }
        }

        void IThreadPoolWorkItem.Execute() => Fire(isThreadPool: true);

        internal void Fire(bool isThreadPool = false)
        {
            bool canceled = false;

            lock (m_associatedTimerQueue)
            {
                canceled = m_canceled;
                if (!canceled)
                    m_callbacksRunning++;
            }

            if (canceled)
                return;

            CallCallback(isThreadPool);

            bool shouldSignal = false;
            lock (m_associatedTimerQueue)
            {
                m_callbacksRunning--;
                if (m_canceled && m_callbacksRunning == 0 && m_notifyWhenNoCallbacksRunning != null)
                    shouldSignal = true;
            }

            if (shouldSignal)
                SignalNoCallbacksRunning();
        }

        internal void SignalNoCallbacksRunning()
        {
            object toSignal = m_notifyWhenNoCallbacksRunning;
            Debug.Assert(toSignal is WaitHandle || toSignal is Task<bool>);

            if (toSignal is WaitHandle wh)
            {
                Interop.Kernel32.SetEvent(wh.SafeWaitHandle);
            }
            else
            {
                ((Task<bool>)toSignal).TrySetResult(true);
            }
        }

        internal void CallCallback(bool isThreadPool)
        {
            if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                FrameworkEventSource.Log.ThreadTransferReceiveObj(this, 1, string.Empty);

            // Call directly if EC flow is suppressed
            ExecutionContext context = m_executionContext;
            if (context == null)
            {
                m_timerCallback(m_state);
            }
            else
            {
                if (isThreadPool)
                {
                    ExecutionContext.RunFromThreadPoolDispatchLoop(Thread.CurrentThread, context, s_callCallbackInContext, this);
                }
                else
                {
                    ExecutionContext.RunInternal(context, s_callCallbackInContext, this);
                }
            }
        }

        private static readonly ContextCallback s_callCallbackInContext = state =>
        {
            TimerQueueTimer t = (TimerQueueTimer)state;
            t.m_timerCallback(t.m_state);
        };
    }

    // TimerHolder serves as an intermediary between Timer and TimerQueueTimer, releasing the TimerQueueTimer 
    // if the Timer is collected.
    // This is necessary because Timer itself cannot use its finalizer for this purpose.  If it did,
    // then users could control timer lifetimes using GC.SuppressFinalize/ReRegisterForFinalize.
    // You might ask, wouldn't that be a good thing?  Maybe (though it would be even better to offer this
    // via first-class APIs), but Timer has never offered this, and adding it now would be a breaking
    // change, because any code that happened to be suppressing finalization of Timer objects would now
    // unwittingly be changing the lifetime of those timers.
    internal sealed class TimerHolder
    {
        internal TimerQueueTimer m_timer;

        public TimerHolder(TimerQueueTimer timer)
        {
            m_timer = timer;
        }

        ~TimerHolder()
        {
            // If shutdown has started, another thread may be suspended while holding the timer lock.
            // So we can't safely close the timer.  
            //
            // Similarly, we should not close the timer during AD-unload's live-object finalization phase.
            // A rude abort may have prevented us from releasing the lock.
            //
            // Note that in either case, the Timer still won't fire, because ThreadPool threads won't be
            // allowed to run in this AppDomain.
            if (Environment.HasShutdownStarted)
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

        public ValueTask CloseAsync()
        {
            ValueTask result = m_timer.CloseAsync();
            GC.SuppressFinalize(this);
            return result;
        }
    }


    public sealed class Timer : MarshalByRefObject, IDisposable, IAsyncDisposable
    {
        private const uint MAX_SUPPORTED_TIMEOUT = (uint)0xfffffffe;

        private TimerHolder m_timer;

        public Timer(TimerCallback callback,
                     object state,
                     int dueTime,
                     int period) :
                     this(callback, state, dueTime, period, flowExecutionContext: true)
        {
        }

        internal Timer(TimerCallback callback,
                       object state,
                       int dueTime,
                       int period,
                       bool flowExecutionContext)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (period < -1)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            TimerSetup(callback, state, (uint)dueTime, (uint)period, flowExecutionContext);
        }

        public Timer(TimerCallback callback,
                     object state,
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

            TimerSetup(callback, state, (uint)dueTm, (uint)periodTm);
        }

        [CLSCompliant(false)]
        public Timer(TimerCallback callback,
                     object state,
                     uint dueTime,
                     uint period)
        {
            TimerSetup(callback, state, dueTime, period);
        }

        public Timer(TimerCallback callback,
                     object state,
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
            TimerSetup(callback, state, (uint)dueTime, (uint)period);
        }

        public Timer(TimerCallback callback)
        {
            int dueTime = -1;    // we want timer to be registered, but not activated.  Requires caller to call
            int period = -1;    // Change after a timer instance is created.  This is to avoid the potential
                                // for a timer to be fired before the returned value is assigned to the variable,
                                // potentially causing the callback to reference a bogus value (if passing the timer to the callback). 

            TimerSetup(callback, this, (uint)dueTime, (uint)period);
        }

        private void TimerSetup(TimerCallback callback,
                                object state,
                                uint dueTime,
                                uint period,
                                bool flowExecutionContext = true)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(TimerCallback));

            m_timer = new TimerHolder(new TimerQueueTimer(callback, state, dueTime, period, flowExecutionContext));
        }

        public bool Change(int dueTime, int period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (period < -1)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            return m_timer.m_timer.Change((uint)dueTime, (uint)period);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return Change((long)dueTime.TotalMilliseconds, (long)period.TotalMilliseconds);
        }

        [CLSCompliant(false)]
        public bool Change(uint dueTime, uint period)
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

            return m_timer.m_timer.Change((uint)dueTime, (uint)period);
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

        public ValueTask DisposeAsync()
        {
            return m_timer.CloseAsync();
        }
    }
}
