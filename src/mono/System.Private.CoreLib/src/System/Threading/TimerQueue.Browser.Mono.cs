// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
        private static long s_shortestDueTimeMs = long.MaxValue;

        // this means that it's in the s_scheduledTimers collection, not that it's the one which would run on the next TimeoutCallback
        private bool _isScheduled;
        private long _scheduledDueTimeMs;

        private TimerQueue(int _)
        {
        }

        // This replaces the current pending setTimeout with shorter one
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void MainThreadScheduleTimer(void* callback, int shortestDueTimeMs);

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        // this callback will arrive on the main thread, called from mono_wasm_execute_timer
        private static void TimerHandler ()
        {
            try
            {
                // always only have one scheduled at a time
                s_shortestDueTimeMs = long.MaxValue;

                long currentTimeMs = TickCount64;
                ReplaceNextTimer(PumpTimerQueue(currentTimeMs), currentTimeMs);
            }
            catch (Exception e)
            {
                Environment.FailFast("TimerQueue.TimerHandler failed", e);
            }
        }

        // this is called with shortest of timers scheduled on the particular TimerQueue
        private bool SetTimer(uint actualDuration)
        {
            Debug.Assert((int)actualDuration >= 0);
            long currentTimeMs = TickCount64;
            if (!_isScheduled)
            {
                s_scheduledTimers ??= new List<TimerQueue>(Instances.Length);
                s_scheduledTimersToFire ??= new List<TimerQueue>(Instances.Length);
                s_scheduledTimers.Add(this);
                _isScheduled = true;
            }

            _scheduledDueTimeMs = currentTimeMs + (int)actualDuration;

            ReplaceNextTimer(ShortestDueTime(), currentTimeMs);

            return true;
        }

        // shortest time of all TimerQueues
        private static unsafe void ReplaceNextTimer(long shortestDueTimeMs, long currentTimeMs)
        {
            if (shortestDueTimeMs == long.MaxValue)
            {
                return;
            }

            // this also covers s_shortestDueTimeMs = long.MaxValue when none is scheduled
            if (s_shortestDueTimeMs > shortestDueTimeMs)
            {
                s_shortestDueTimeMs = shortestDueTimeMs;
                int shortestWait = Math.Max((int)(shortestDueTimeMs - currentTimeMs), 0);
                // this would cancel the previous schedule and create shorter one, it is expensive callback
                MainThreadScheduleTimer((void*)(delegate* unmanaged[Cdecl]<void>)&TimerHandler, shortestWait);
            }
        }

        private static long ShortestDueTime()
        {
            if (s_scheduledTimers == null)
            {
                return long.MaxValue;
            }

            long shortestDueTimeMs = long.MaxValue;
            var timers = s_scheduledTimers!;
            for (int i = timers.Count - 1; i >= 0; --i)
            {
                TimerQueue timer = timers[i];
                if (timer._scheduledDueTimeMs < shortestDueTimeMs)
                {
                    shortestDueTimeMs = timer._scheduledDueTimeMs;
                }
            }

            return shortestDueTimeMs;
        }

        private static long PumpTimerQueue(long currentTimeMs)
        {
            if (s_scheduledTimersToFire == null)
            {
                return ShortestDueTime();
            }

            List<TimerQueue> timersToFire = s_scheduledTimersToFire!;
            List<TimerQueue> timers;
            timers = s_scheduledTimers!;
            long shortestDueTimeMs = long.MaxValue;
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

                if (timer._scheduledDueTimeMs < shortestDueTimeMs)
                {
                    shortestDueTimeMs = timer._scheduledDueTimeMs;
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

            return shortestDueTimeMs;
        }
    }
}
