// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// A LIFO semaphore.
    /// Waits on this semaphore are uninterruptible.
    /// </summary>
    internal sealed partial class LowLevelLifoSemaphore : LowLevelLifoSemaphoreBase, IDisposable
    {
        private const int SpinSleep0Threshold = 10;

        public LowLevelLifoSemaphore(int initialSignalCount, int maximumSignalCount, int spinCount, Action onWait)
            : base (initialSignalCount, maximumSignalCount, spinCount, onWait)
        {
            Create(maximumSignalCount);
        }

        public bool Wait(int timeoutMs, bool spinWait)
        {
            Debug.Assert(timeoutMs >= -1);

            int spinCount = spinWait ? _spinCount : 0;

            // Try to acquire the semaphore or
            // a) register as a spinner if spinCount > 0 and timeoutMs > 0
            // b) register as a waiter if there's already too many spinners or spinCount == 0 and timeoutMs > 0
            // c) bail out if timeoutMs == 0 and return false
            Counts counts = _separated._counts;
            while (true)
            {
                Debug.Assert(counts.SignalCount <= _maximumSignalCount);
                Counts newCounts = counts;
                if (counts.SignalCount != 0)
                {
                    newCounts.DecrementSignalCount();
                }
                else if (timeoutMs != 0)
                {
                    if (spinCount > 0 && newCounts.SpinnerCount < byte.MaxValue)
                    {
                        newCounts.IncrementSpinnerCount();
                    }
                    else
                    {
                        // Maximum number of spinners reached, register as a waiter instead
                        newCounts.IncrementWaiterCount();
                    }
                }

                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    if (counts.SignalCount != 0)
                    {
                        return true;
                    }
                    if (newCounts.WaiterCount != counts.WaiterCount)
                    {
                        return WaitForSignal(timeoutMs);
                    }
                    if (timeoutMs == 0)
                    {
                        return false;
                    }
                    break;
                }

                counts = countsBeforeUpdate;
            }

#if CORECLR && TARGET_UNIX
            // The PAL's wait subsystem is slower, spin more to compensate for the more expensive wait
            spinCount *= 2;
#endif
            int processorCount = Environment.ProcessorCount;
            int spinIndex = processorCount > 1 ? 0 : SpinSleep0Threshold;
            while (spinIndex < spinCount)
            {
                LowLevelSpinWaiter.Wait(spinIndex, SpinSleep0Threshold, processorCount);
                spinIndex++;

                // Try to acquire the semaphore and unregister as a spinner
                counts = _separated._counts;
                while (counts.SignalCount > 0)
                {
                    Counts newCounts = counts;
                    newCounts.DecrementSignalCount();
                    newCounts.DecrementSpinnerCount();

                    Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        return true;
                    }

                    counts = countsBeforeUpdate;
                }
            }

            // Unregister as spinner, and acquire the semaphore or register as a waiter
            counts = _separated._counts;
            while (true)
            {
                Counts newCounts = counts;
                newCounts.DecrementSpinnerCount();
                if (counts.SignalCount != 0)
                {
                    newCounts.DecrementSignalCount();
                }
                else
                {
                    newCounts.IncrementWaiterCount();
                }

                Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    return counts.SignalCount != 0 || WaitForSignal(timeoutMs);
                }

                counts = countsBeforeUpdate;
            }
        }

        private bool WaitForSignal(int timeoutMs)
        {
            Debug.Assert(timeoutMs > 0 || timeoutMs == -1);

            _onWait();

            while (true)
            {
                int startWaitTicks = timeoutMs != -1 ? Environment.TickCount : 0;
                if (timeoutMs == 0 || !WaitCore(timeoutMs))
                {
                    // Unregister the waiter. The wait subsystem used above guarantees that a thread that wakes due to a timeout does
                    // not observe a signal to the object being waited upon.
                    _separated._counts.InterlockedDecrementWaiterCount();
                    return false;
                }
                int endWaitTicks = timeoutMs != -1 ? Environment.TickCount : 0;

                // Unregister the waiter if this thread will not be waiting anymore, and try to acquire the semaphore
                Counts counts = _separated._counts;
                while (true)
                {
                    Debug.Assert(counts.WaiterCount != 0);
                    Counts newCounts = counts;
                    if (counts.SignalCount != 0)
                    {
                        newCounts.DecrementSignalCount();
                        newCounts.DecrementWaiterCount();
                    }

                    // This waiter has woken up and this needs to be reflected in the count of waiters signaled to wake
                    if (counts.CountOfWaitersSignaledToWake != 0)
                    {
                        newCounts.DecrementCountOfWaitersSignaledToWake();
                    }

                    Counts countsBeforeUpdate = _separated._counts.InterlockedCompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        if (counts.SignalCount != 0)
                        {
                            return true;
                        }
                        break;
                    }

                    counts = countsBeforeUpdate;
                    if (timeoutMs != -1) {
                        int waitMs = endWaitTicks - startWaitTicks;
                        if (waitMs >= 0 && waitMs < timeoutMs)
                            timeoutMs -= waitMs;
                        else
                            timeoutMs = 0;
                    }
                }
            }
        }
    }
}
