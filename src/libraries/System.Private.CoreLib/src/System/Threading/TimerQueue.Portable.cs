// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading
{
    //
    // Unix-specific implementation of Timer
    //
    internal sealed partial class TimerQueue : IThreadPoolWorkItem
    {
        private static List<TimerQueue>? s_scheduledTimers;
        private static List<TimerQueue>? s_scheduledTimersToFire;

        /// <summary>
        /// This event is used by the timer thread to wait for timer expiration. It is also
        /// used to notify the timer thread that a new timer has been set.
        /// </summary>
        private static readonly AutoResetEvent s_timerEvent = new AutoResetEvent(false);

        private bool _isScheduled;
        private long _scheduledDueTimeMs;

        private TimerQueue(int id)
        {
        }

        private static List<TimerQueue> InitializeScheduledTimerManager_Locked()
        {
            Debug.Assert(s_scheduledTimers == null);

            var timers = new List<TimerQueue>(Instances.Length);
            s_scheduledTimersToFire ??= new List<TimerQueue>(Instances.Length);

            // The timer thread must start in the default execution context without transferring the context, so
            // using UnsafeStart() instead of Start()
            Thread timerThread = new Thread(TimerThread)
            {
                Name = ".NET Timers",
                IsBackground = true
            };
            timerThread.UnsafeStart();

            // Do this after creating the thread in case thread creation fails so that it will try again next time
            s_scheduledTimers = timers;
            return timers;
        }

        private bool SetTimer(uint actualDuration)
        {
            Debug.Assert((int)actualDuration >= 0);
            long dueTimeMs = TickCount64 + (int)actualDuration;
            AutoResetEvent timerEvent = s_timerEvent;
            lock (timerEvent)
            {
                if (!_isScheduled)
                {
                    List<TimerQueue>? timers = s_scheduledTimers;
                    if (timers == null)
                    {
                        timers = InitializeScheduledTimerManager_Locked();
                    }

                    timers.Add(this);
                    _isScheduled = true;
                }

                _scheduledDueTimeMs = dueTimeMs;
            }

            timerEvent.Set();
            return true;
        }

        /// <summary>
        /// This method is executed on a dedicated a timer thread. Its purpose is
        /// to handle timer requests and notify the TimerQueue when a timer expires.
        /// </summary>
        private static void TimerThread()
        {
            AutoResetEvent timerEvent = s_timerEvent;
            List<TimerQueue> timersToFire = s_scheduledTimersToFire!;
            List<TimerQueue> timers;
            lock (timerEvent)
            {
                timers = s_scheduledTimers!;
            }

            int shortestWaitDurationMs = Timeout.Infinite;
            while (true)
            {
                timerEvent.WaitOne(shortestWaitDurationMs);

                long currentTimeMs = TickCount64;
                shortestWaitDurationMs = int.MaxValue;
                lock (timerEvent)
                {
                    for (int i = timers.Count - 1; i >= 0; --i)
                    {
                        TimerQueue timer = timers[i];
                        long waitDurationMs = timer._scheduledDueTimeMs - currentTimeMs;
                        if (waitDurationMs <= 0)
                        {
                            timer._isScheduled = false;
                            timersToFire.Add(timer);

                            int lastIndex = timers.Count - 1;
                            if (i != lastIndex)
                            {
                                timers[i] = timers[lastIndex];
                            }
                            timers.RemoveAt(lastIndex);
                            continue;
                        }

                        if (waitDurationMs < shortestWaitDurationMs)
                        {
                            shortestWaitDurationMs = (int)waitDurationMs;
                        }
                    }
                }

                if (timersToFire.Count > 0)
                {
                    foreach (TimerQueue timerToFire in timersToFire)
                    {
                        ThreadPool.UnsafeQueueTimeSensitiveWorkItem(timerToFire);
                    }
                    timersToFire.Clear();
                }

                if (shortestWaitDurationMs == int.MaxValue)
                {
                    shortestWaitDurationMs = Timeout.Infinite;
                }
            }
        }

        void IThreadPoolWorkItem.Execute() => FireNextTimers();
    }
}
