// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace System.Threading
{
    public delegate void TimerCallback(object? state);

    // TimerQueue maintains a list of active timers.  We use a single native timer to schedule all managed timers
    // in the process.
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
    internal partial class TimerQueue
    {
        #region Shared TimerQueue instances

        public static TimerQueue[] Instances { get; } = CreateTimerQueues();

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

        #region interface to native timer

        private bool _isTimerScheduled;
        private int _currentTimerStartTicks;
        private uint _currentTimerDuration;

        private bool EnsureTimerFiresBy(uint requestedDuration)
        {
            // The VM's timer implementation does not work well for very long-duration timers.
            // See kb 950807.
            // So we'll limit our native timer duration to a "small" value.
            // This may cause us to attempt to fire timers early, but that's ok - 
            // we'll just see that none of our timers has actually reached its due time,
            // and schedule the native timer again.
            const uint maxPossibleDuration = 0x0fffffff;
            uint actualDuration = Math.Min(requestedDuration, maxPossibleDuration);

            if (_isTimerScheduled)
            {
                uint elapsed = (uint)(TickCount - _currentTimerStartTicks);
                if (elapsed >= _currentTimerDuration)
                    return true; //the timer's about to fire

                uint remainingDuration = _currentTimerDuration - elapsed;
                if (actualDuration >= remainingDuration)
                    return true; //the timer will fire earlier than this request
            }

            // If Pause is underway then do not schedule the timers
            // A later update during resume will re-schedule
            if (_pauseTicks != 0)
            {
                Debug.Assert(!_isTimerScheduled);
                return true;
            }

            if (SetTimer(actualDuration))
            {
                _isTimerScheduled = true;
                _currentTimerStartTicks = TickCount;
                _currentTimerDuration = actualDuration;
                return true;
            }

            return false;
        }

        #endregion

        #region Firing timers

        // The two lists of timers that are part of this TimerQueue.  They conform to a single guarantee:
        // no timer in _longTimers has an absolute next firing time <= _currentAbsoluteThreshold.
        // That way, when FireNextTimers is invoked, we always process the short list, and we then only
        // process the long list if the current time is greater than _currentAbsoluteThreshold (or
        // if the short list is now empty and we need to process the long list to know when to next
        // invoke FireNextTimers).
        private TimerQueueTimer? _shortTimers;
        private TimerQueueTimer? _longTimers;

        // The current threshold, an absolute time where any timers scheduled to go off at or
        // before this time must be queued to the short list.
        private int _currentAbsoluteThreshold = ShortTimersThresholdMilliseconds;

        // Default threshold that separates which timers target _shortTimers vs _longTimers. The threshold
        // is chosen to balance the number of timers in the small list against the frequency with which
        // we need to scan the long list.  It's thus somewhat arbitrary and could be changed based on
        // observed workload demand. The larger the number, the more timers we'll likely need to enumerate
        // every time the timer fires, but also the more likely it is that when it does we won't
        // need to look at the long list because the current time will be <= _currentAbsoluteThreshold.
        private const int ShortTimersThresholdMilliseconds = 333;

        // Time when Pause was called
        private volatile int _pauseTicks = 0;

        // Fire any timers that have expired, and update the native timer to schedule the rest of them.
        // We're in a thread pool work item here, and if there are multiple timers to be fired, we want
        // to queue all but the first one.  The first may can then be invoked synchronously or queued,
        // a task left up to our caller, which might be firing timers from multiple queues.
        private void FireNextTimers()
        {
            // We fire the first timer on this thread; any other timers that need to be fired
            // are queued to the ThreadPool.
            TimerQueueTimer? timerToFireOnThisThread = null;

            lock (this)
            {
                // Since we got here, that means our previous timer has fired.
                _isTimerScheduled = false;
                bool haveTimerToSchedule = false;
                uint nextTimerDuration = uint.MaxValue;

                int nowTicks = TickCount;

                // Sweep through the "short" timers.  If the current tick count is greater than
                // the current threshold, also sweep through the "long" timers.  Finally, as part
                // of sweeping the long timers, move anything that'll fire within the next threshold
                // to the short list.  It's functionally ok if more timers end up in the short list
                // than is truly necessary (but not the opposite).
                TimerQueueTimer? timer = _shortTimers;
                for (int listNum = 0; listNum < 2; listNum++) // short == 0, long == 1
                {
                    while (timer != null)
                    {
                        Debug.Assert(timer._dueTime != Timeout.UnsignedInfinite, "A timer in the list must have a valid due time.");

                        // Save off the next timer to examine, in case our examination of this timer results
                        // in our deleting or moving it; we'll continue after with this saved next timer.
                        TimerQueueTimer? next = timer._next;

                        uint elapsed = (uint)(nowTicks - timer._startTicks);
                        int remaining = (int)timer._dueTime - (int)elapsed;
                        if (remaining <= 0)
                        {
                            // Timer is ready to fire.

                            if (timer._period != Timeout.UnsignedInfinite)
                            {
                                // This is a repeating timer; schedule it to run again.

                                // Discount the extra amount of time that has elapsed since the previous firing time to
                                // prevent timer ticks from drifting.  If enough time has already elapsed for the timer to fire
                                // again, meaning the timer can't keep up with the short period, have it fire 1 ms from now to
                                // avoid spinning without a delay.
                                timer._startTicks = nowTicks;
                                uint elapsedForNextDueTime = elapsed - timer._dueTime;
                                timer._dueTime = (elapsedForNextDueTime < timer._period) ?
                                    timer._period - elapsedForNextDueTime :
                                    1;

                                // Update the timer if this becomes the next timer to fire.
                                if (timer._dueTime < nextTimerDuration)
                                {
                                    haveTimerToSchedule = true;
                                    nextTimerDuration = timer._dueTime;
                                }

                                // Validate that the repeating timer is still on the right list.  It's likely that
                                // it started in the long list and was moved to the short list at some point, so
                                // we now want to move it back to the long list if that's where it belongs. Note that
                                // if we're currently processing the short list and move it to the long list, we may
                                // end up revisiting it again if we also enumerate the long list, but we will have already
                                // updated the due time appropriately so that we won't fire it again (it's also possible
                                // but rare that we could be moving a timer from the long list to the short list here,
                                // if the initial due time was set to be long but the timer then had a short period).
                                bool targetShortList = (nowTicks + timer._dueTime) - _currentAbsoluteThreshold <= 0;
                                if (timer._short != targetShortList)
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

                            if (remaining < nextTimerDuration)
                            {
                                haveTimerToSchedule = true;
                                nextTimerDuration = (uint)remaining;
                            }

                            if (!timer._short && remaining <= ShortTimersThresholdMilliseconds)
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
                        // could in that case just set the next firing to 1, but we may as well just iterate the
                        // long list now; otherwise, most timers created in the interim would end up in the long
                        // list and we'd likely end up paying for another invocation of FireNextTimers that could
                        // have been delayed longer (to whatever is the current minimum in the long list).
                        int remaining = _currentAbsoluteThreshold - nowTicks;
                        if (remaining > 0)
                        {
                            if (_shortTimers == null && _longTimers != null)
                            {
                                // We don't have any short timers left and we haven't examined the long list,
                                // which means we likely don't have an accurate nextTimerDuration.
                                // But we do know that nothing in the long list will be firing before or at _currentAbsoluteThreshold,
                                // so we can just set nextTimerDuration to the difference between then and now.
                                nextTimerDuration = (uint)remaining + 1;
                                haveTimerToSchedule = true;
                            }
                            break;
                        }

                        // Switch to processing the long list.
                        timer = _longTimers;

                        // Now that we're going to process the long list, update the current threshold.
                        _currentAbsoluteThreshold = nowTicks + ShortTimersThresholdMilliseconds;
                    }
                }

                // If we still have scheduled timers, update the timer to ensure it fires
                // in time for the next one in line.
                if (haveTimerToSchedule)
                {
                    EnsureTimerFiresBy(nextTimerDuration);
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
            bool shouldBeShort = _currentAbsoluteThreshold - absoluteDueTime >= 0;

            if (timer._dueTime == Timeout.UnsignedInfinite)
            {
                // If the timer wasn't previously scheduled, now add it to the right list.
                timer._short = shouldBeShort;
                LinkTimer(timer);
            }
            else if (timer._short != shouldBeShort)
            {
                // If the timer was previously scheduled, but this update should cause
                // it to move over the list threshold in either direction, do so.
                UnlinkTimer(timer);
                timer._short = shouldBeShort;
                LinkTimer(timer);
            }

            timer._dueTime = dueTime;
            timer._period = (period == 0) ? Timeout.UnsignedInfinite : period;
            timer._startTicks = nowTicks;
            return EnsureTimerFiresBy(dueTime);
        }

        public void MoveTimerToCorrectList(TimerQueueTimer timer, bool shortList)
        {
            Debug.Assert(timer._dueTime != Timeout.UnsignedInfinite, "Expected timer to be on a list.");
            Debug.Assert(timer._short != shortList, "Unnecessary if timer is already on the right list.");

            // Unlink it from whatever list it's on, change its list association, then re-link it.
            UnlinkTimer(timer);
            timer._short = shortList;
            LinkTimer(timer);
        }

        private void LinkTimer(TimerQueueTimer timer)
        {
            // Use timer._short to decide to which list to add.
            ref TimerQueueTimer? listHead = ref timer._short ? ref _shortTimers : ref _longTimers;
            timer._next = listHead;
            if (timer._next != null)
            {
                timer._next._prev = timer;
            }
            timer._prev = null;
            listHead = timer;
        }

        private void UnlinkTimer(TimerQueueTimer timer)
        {
            TimerQueueTimer? t = timer._next;
            if (t != null)
            {
                t._prev = timer._prev;
            }

            if (_shortTimers == timer)
            {
                Debug.Assert(timer._short);
                _shortTimers = t;
            }
            else if (_longTimers == timer)
            {
                Debug.Assert(!timer._short);
                _longTimers = t;
            }

            t = timer._prev;
            if (t != null)
            {
                t._next = timer._next;
            }

            // At this point the timer is no longer in a list, but its next and prev
            // references may still point to other nodes.  UnlinkTimer should thus be
            // followed by something that overwrites those references, either with null
            // if deleting the timer or other nodes if adding it to another list.
        }

        public void DeleteTimer(TimerQueueTimer timer)
        {
            if (timer._dueTime != Timeout.UnsignedInfinite)
            {
                UnlinkTimer(timer);
                timer._prev = null;
                timer._next = null;
                timer._dueTime = Timeout.UnsignedInfinite;
                timer._period = Timeout.UnsignedInfinite;
                timer._startTicks = 0;
                timer._short = false;
            }
        }

        #endregion
    }

    // A timer in our TimerQueue.
    internal sealed partial class TimerQueueTimer : IThreadPoolWorkItem
    {
        // The associated timer queue.
        private readonly TimerQueue _associatedTimerQueue;

        // All mutable fields of this class are protected by a lock on _associatedTimerQueue.
        // The first six fields are maintained by TimerQueue.

        // Links to the next and prev timers in the list.
        internal TimerQueueTimer? _next;
        internal TimerQueueTimer? _prev;

        // true if on the short list; otherwise, false.
        internal bool _short;

        // The time, according to TimerQueue.TickCount, when this timer's current interval started.
        internal int _startTicks;

        // Timeout.UnsignedInfinite if we are not going to fire.  Otherwise, the offset from _startTime when we will fire.
        internal uint _dueTime;

        // Timeout.UnsignedInfinite if we are a single-shot timer.  Otherwise, the repeat interval.
        internal uint _period;

        // Info about the user's callback
        private readonly TimerCallback _timerCallback;
        private readonly object? _state;
        private readonly ExecutionContext? _executionContext;

        // When Timer.Dispose(WaitHandle) is used, we need to signal the wait handle only
        // after all pending callbacks are complete.  We set _canceled to prevent any callbacks that
        // are already queued from running.  We track the number of callbacks currently executing in 
        // _callbacksRunning.  We set _notifyWhenNoCallbacksRunning only when _callbacksRunning
        // reaches zero.  Same applies if Timer.DisposeAsync() is used, except with a Task<bool>
        // instead of with a provided WaitHandle.
        private int _callbacksRunning;
        private volatile bool _canceled;
        private volatile object? _notifyWhenNoCallbacksRunning; // may be either WaitHandle or Task<bool>


        internal TimerQueueTimer(TimerCallback timerCallback, object? state, uint dueTime, uint period, bool flowExecutionContext)
        {
            _timerCallback = timerCallback;
            _state = state;
            _dueTime = Timeout.UnsignedInfinite;
            _period = Timeout.UnsignedInfinite;
            if (flowExecutionContext)
            {
                _executionContext = ExecutionContext.Capture();
            }
            _associatedTimerQueue = TimerQueue.Instances[Thread.GetCurrentProcessorId() % TimerQueue.Instances.Length];

            // After the following statement, the timer may fire.  No more manipulation of timer state outside of
            // the lock is permitted beyond this point!
            if (dueTime != Timeout.UnsignedInfinite)
                Change(dueTime, period);
        }

        internal bool Change(uint dueTime, uint period)
        {
            bool success;

            lock (_associatedTimerQueue)
            {
                if (_canceled)
                    throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);

                _period = period;

                if (dueTime == Timeout.UnsignedInfinite)
                {
                    _associatedTimerQueue.DeleteTimer(this);
                    success = true;
                }
                else
                {
                    if (FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                        FrameworkEventSource.Log.ThreadTransferSendObj(this, 1, string.Empty, true, (int)dueTime, (int)period);
                    success = _associatedTimerQueue.UpdateTimer(this, dueTime, period);
                }
            }

            return success;
        }


        public void Close()
        {
            lock (_associatedTimerQueue)
            {
                if (!_canceled)
                {
                    _canceled = true;
                    _associatedTimerQueue.DeleteTimer(this);
                }
            }
        }


        public bool Close(WaitHandle toSignal)
        {
            bool success;
            bool shouldSignal = false;

            lock (_associatedTimerQueue)
            {
                if (_canceled)
                {
                    success = false;
                }
                else
                {
                    _canceled = true;
                    _notifyWhenNoCallbacksRunning = toSignal;
                    _associatedTimerQueue.DeleteTimer(this);
                    shouldSignal = _callbacksRunning == 0;
                    success = true;
                }
            }

            if (shouldSignal)
                SignalNoCallbacksRunning();

            return success;
        }

        public ValueTask CloseAsync()
        {
            lock (_associatedTimerQueue)
            {
                object? notifyWhenNoCallbacksRunning = _notifyWhenNoCallbacksRunning;

                // Mark the timer as canceled if it's not already.
                if (_canceled)
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
                    _canceled = true;
                    _associatedTimerQueue.DeleteTimer(this);
                }

                // We've deleted the timer, so if there are no callbacks queued or running,
                // we're done and return an already-completed value task.
                if (_callbacksRunning == 0)
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
                    var t = new Task<bool>((object?)null, TaskCreationOptions.RunContinuationsAsynchronously);
                    _notifyWhenNoCallbacksRunning = t;
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

            lock (_associatedTimerQueue)
            {
                canceled = _canceled;
                if (!canceled)
                    _callbacksRunning++;
            }

            if (canceled)
                return;

            CallCallback(isThreadPool);

            bool shouldSignal = false;
            lock (_associatedTimerQueue)
            {
                _callbacksRunning--;
                if (_canceled && _callbacksRunning == 0 && _notifyWhenNoCallbacksRunning != null)
                    shouldSignal = true;
            }

            if (shouldSignal)
                SignalNoCallbacksRunning();
        }

        internal void SignalNoCallbacksRunning()
        {
            object? toSignal = _notifyWhenNoCallbacksRunning;
            Debug.Assert(toSignal is WaitHandle || toSignal is Task<bool>);

            if (toSignal is WaitHandle wh)
            {
                EventWaitHandle.Set(wh.SafeWaitHandle!); // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/2384
            }
            else
            {
                ((Task<bool>)toSignal).TrySetResult(true);
            }
        }

        internal void CallCallback(bool isThreadPool)
        {
            if (FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                FrameworkEventSource.Log.ThreadTransferReceiveObj(this, 1, string.Empty);

            // Call directly if EC flow is suppressed
            ExecutionContext? context = _executionContext;
            if (context == null)
            {
                _timerCallback(_state);
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
            Debug.Assert(state is TimerQueueTimer);
            var t = (TimerQueueTimer)state;
            t._timerCallback(t._state);
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
        internal readonly TimerQueueTimer _timer;

        public TimerHolder(TimerQueueTimer timer)
        {
            _timer = timer;
        }

        ~TimerHolder()
        {
            _timer.Close();
        }

        public void Close()
        {
            _timer.Close();
            GC.SuppressFinalize(this);
        }

        public bool Close(WaitHandle notifyObject)
        {
            bool result = _timer.Close(notifyObject);
            GC.SuppressFinalize(this);
            return result;
        }

        public ValueTask CloseAsync()
        {
            ValueTask result = _timer.CloseAsync();
            GC.SuppressFinalize(this);
            return result;
        }
    }


    public sealed class Timer : MarshalByRefObject, IDisposable, IAsyncDisposable
    {
        private const uint MAX_SUPPORTED_TIMEOUT = (uint)0xfffffffe;

        private TimerHolder _timer = null!; // initialized in helper called by ctors

        public Timer(TimerCallback callback,
                     object? state,
                     int dueTime,
                     int period) :
                     this(callback, state, dueTime, period, flowExecutionContext: true)
        {
        }

        internal Timer(TimerCallback callback,
                       object? state,
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
                     object? state,
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
                     object? state,
                     uint dueTime,
                     uint period)
        {
            TimerSetup(callback, state, dueTime, period);
        }

        public Timer(TimerCallback callback,
                     object? state,
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
            int dueTime = -1;   // We want timer to be registered, but not activated.  Requires caller to call
            int period = -1;    // Change after a timer instance is created.  This is to avoid the potential
                                // for a timer to be fired before the returned value is assigned to the variable,
                                // potentially causing the callback to reference a bogus value (if passing the timer to the callback). 

            TimerSetup(callback, this, (uint)dueTime, (uint)period);
        }

        private void TimerSetup(TimerCallback callback,
                                object? state,
                                uint dueTime,
                                uint period,
                                bool flowExecutionContext = true)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(TimerCallback));

            _timer = new TimerHolder(new TimerQueueTimer(callback, state, dueTime, period, flowExecutionContext));
        }

        public bool Change(int dueTime, int period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException(nameof(dueTime), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (period < -1)
                throw new ArgumentOutOfRangeException(nameof(period), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            return _timer._timer.Change((uint)dueTime, (uint)period);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return Change((long)dueTime.TotalMilliseconds, (long)period.TotalMilliseconds);
        }

        [CLSCompliant(false)]
        public bool Change(uint dueTime, uint period)
        {
            return _timer._timer.Change(dueTime, period);
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

            return _timer._timer.Change((uint)dueTime, (uint)period);
        }

        public bool Dispose(WaitHandle notifyObject)
        {
            if (notifyObject == null)
                throw new ArgumentNullException(nameof(notifyObject));

            return _timer.Close(notifyObject);
        }

        public void Dispose()
        {
            _timer.Close();
        }

        public ValueTask DisposeAsync()
        {
            return _timer.CloseAsync();
        }
    }
}
