// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// A lightweight spin-waiter intended to be used as the first-level wait for a condition before the user forces the thread
    /// into a wait state, and where the condition to be checked in each iteration is relatively cheap, like just an interlocked
    /// operation.
    ///
    /// Used by the wait subsystem on Unix, so this class cannot have any dependencies on the wait subsystem.
    /// </summary>
    internal struct LowLevelSpinWaiter
    {
        private int _spinningThreadCount;

        public bool SpinWaitForCondition(Func<object, bool> condition, object state, int spinCount, int sleep0Threshold)
        {
            Debug.Assert(condition != null);

            int processorCount = Environment.ProcessorCount;
            int spinningThreadCount = Interlocked.Increment(ref _spinningThreadCount);
            try
            {
                // Limit the maximum spinning thread count to the processor count to prevent unnecessary context switching
                // caused by an excessive number of threads spin waiting, perhaps even slowing down the thread holding the
                // resource being waited upon
                if (spinningThreadCount <= processorCount)
                {
                    // For uniprocessor systems, start at the yield threshold since the pause instructions used for waiting
                    // prior to that threshold would not help other threads make progress
                    for (int spinIndex = processorCount > 1 ? 0 : sleep0Threshold; spinIndex < spinCount; ++spinIndex)
                    {
                        // The caller should check the condition in a fast path before calling this method, so wait first
                        Wait(spinIndex, sleep0Threshold, processorCount);

                        if (condition(state))
                        {
                            return true;
                        }
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _spinningThreadCount);
            }

            return false;
        }

        public static void Wait(int spinIndex, int sleep0Threshold, int processorCount)
        {
            Debug.Assert(spinIndex >= 0);
            Debug.Assert(sleep0Threshold >= 0);

            // Wait
            //
            // (spinIndex - Sleep0Threshold) % 2 != 0: The purpose of this check is to interleave Thread.Yield/Sleep(0) with
            // Thread.SpinWait. Otherwise, the following issues occur:
            //   - When there are no threads to switch to, Yield and Sleep(0) become no-op and it turns the spin loop into a
            //     busy-spin that may quickly reach the max spin count and cause the thread to enter a wait state. Completing the
            //     spin loop too early can cause excessive context switcing from the wait.
            //   - If there are multiple threads doing Yield and Sleep(0) (typically from the same spin loop due to contention),
            //     they may switch between one another, delaying work that can make progress.
            if (processorCount > 1 && (spinIndex < sleep0Threshold || (spinIndex - sleep0Threshold) % 2 != 0))
            {
                // Cap the maximum spin count to a value such that many thousands of CPU cycles would not be wasted doing
                // the equivalent of YieldProcessor(), as that that point SwitchToThread/Sleep(0) are more likely to be able to
                // allow other useful work to run. Long YieldProcessor() loops can help to reduce contention, but Sleep(1) is
                // usually better for that.
                int n = Thread.OptimalMaxSpinWaitsPerSpinIteration;
                if (spinIndex <= 30 && (1 << spinIndex) < n)
                {
                    n = 1 << spinIndex;
                }
                Thread.SpinWait(n);
                return;
            }

            // Thread.Sleep is interruptible. The current operation may not allow thread interrupt. Use the uninterruptible
            // version of Sleep(0). Not doing Thread.Yield, it does not seem to have any benefit over Sleep(0).
            Thread.UninterruptibleSleep0();

            // Don't want to Sleep(1) in this spin wait:
            //   - Don't want to spin for that long, since a proper wait will follow when the spin wait fails
            //   - Sleep(1) would put the thread into a wait state, and a proper wait will follow when the spin wait fails
            //     anyway (the intended use for this class), so it's preferable to put the thread into the proper wait state
        }
    }
}
