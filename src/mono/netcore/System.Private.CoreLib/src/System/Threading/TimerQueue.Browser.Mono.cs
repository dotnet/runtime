// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading
{
    //
    // WebAssembly-specific implementation of Timer
    // Based on TimerQueue.Portable.cs
    // Not thread safe
    //
    internal partial class TimerQueue
    {
        private static List<TimerQueue>? s_scheduledTimers;
        private static List<TimerQueue>? s_scheduledTimersToFire;

        private bool _isScheduled;
        private long _scheduledDueTimeMs;

        private TimerQueue(int id)
        {
        }

        [DynamicDependency("TimeoutCallback")]
        // The id argument is unused in netcore
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetTimeout(int timeout, int id);

        // Called by mini-wasm.c:mono_set_timeout_exec
        private static void TimeoutCallback()
        {
            int shortestWaitDurationMs = PumpTimerQueue();

            if (shortestWaitDurationMs != int.MaxValue)
            {
                SetTimeout((int)shortestWaitDurationMs, 0);
            }
        }

        private bool SetTimer(uint actualDuration)
        {
            Debug.Assert((int)actualDuration >= 0);
            long dueTimeMs = TickCount64 + (int)actualDuration;
            if (!_isScheduled)
            {
                s_scheduledTimers ??= new List<TimerQueue>(Instances.Length);
                s_scheduledTimersToFire ??= new List<TimerQueue>(Instances.Length);
                s_scheduledTimers.Add(this);
                _isScheduled = true;
            }
            _scheduledDueTimeMs = dueTimeMs;
            SetTimeout((int)actualDuration, 0);

            return true;
        }

        private static int PumpTimerQueue() // NOTE: this method is called via reflection by test code
        {
            if (s_scheduledTimersToFire == null)
            {
                return int.MaxValue;
            }

            List<TimerQueue> timersToFire = s_scheduledTimersToFire!;
            List<TimerQueue> timers;
            timers = s_scheduledTimers!;
            long currentTimeMs = TickCount64;
            int shortestWaitDurationMs = int.MaxValue;
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

            if (timersToFire.Count > 0)
            {
                foreach (TimerQueue timerToFire in timersToFire)
                {
                    timerToFire.FireNextTimers();
                }
                timersToFire.Clear();
            }

            return shortestWaitDurationMs;
        }
    }
}
