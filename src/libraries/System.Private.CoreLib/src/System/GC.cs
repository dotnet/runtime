// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System
{
    public static partial class GC
    {
#if !MONO
        public static void AddMemoryPressure(long bytesAllocated)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated);
            if (IntPtr.Size == 4)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesAllocated, int.MaxValue);
            }

            MemoryPressure.Add(bytesAllocated);
        }

        public static void RemoveMemoryPressure(long bytesAllocated)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated);
            if (IntPtr.Size == 4)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesAllocated, int.MaxValue);
            }

            MemoryPressure.Remove(bytesAllocated);
        }

        // Keep pressure-state initialization independent of other GC state.
        private static class MemoryPressure
        {
            private const uint PressureCount = 4;
            private const uint MaxGCMemoryPressureRatio = 10;

            private static readonly int[] s_gcCounts = new int[3];
            private static readonly long[] s_addPressure = new long[PressureCount];
            private static readonly long[] s_removePressure = new long[PressureCount];
            private static uint s_iteration;

            private static void CheckCollectionCount()
            {
                if (s_gcCounts[2] != CollectionCount(2))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        s_gcCounts[i] = CollectionCount(i);
                    }

                    s_iteration++;
                    uint p = s_iteration % PressureCount;
                    s_addPressure[p] = 0;
                    s_removePressure[p] = 0;
                }
            }

            private static long InterlockedAddMemoryPressure(ref long pAugend, long addend)
            {
                long oldMemValue;
                long newMemValue;

                do
                {
                    oldMemValue = pAugend;
                    newMemValue = oldMemValue + addend;

                    if (newMemValue < oldMemValue)
                    {
                        newMemValue = long.MaxValue;
                    }
                } while (Interlocked.CompareExchange(ref pAugend, newMemValue, oldMemValue) != oldMemValue);

                return newMemValue;
            }

            // Memory-pressure policy:
            // 1. Start with a budget of 4 MiB on 64-bit platforms, 3 MiB on 32-bit.
            // 2. Compare the budget with additions accumulated in the current interval.
            // 3. Once enough history is available, scale the budget using the add/remove
            //    ratio over the previous three completed accounting intervals, up to
            //    ten times the minimum budget.
            // 4. Raise the budget further if one third of the managed-heap estimate
            //    is larger.
            // 5. Advance the history when a gen2 GC is observed, regardless of its cause.
            internal static void Add(long bytesAllocated)
            {
                CheckCollectionCount();
                uint p = s_iteration % PressureCount;
                long newMemValue = InterlockedAddMemoryPressure(ref s_addPressure[p], bytesAllocated);
                if (NativeRuntimeEventSource.Log.IsEnabled() &&
                    NativeRuntimeEventSource.Log.IsEnabled(EventLevel.Verbose, NativeRuntimeEventSource.Keywords.GCKeyword))
                {
                    NativeRuntimeEventSource.Log.IncreaseMemoryPressure((ulong)bytesAllocated);
                }

                Debug.Assert(PressureCount == 4, "GC.AddMemoryPressure contains unrolled loops which depend on the PressureCount");

                long minBudget = IntPtr.Size == 8 ? 4 * 1024 * 1024 : 3 * 1024 * 1024;
                if (newMemValue >= minBudget)
                {
                    // Exclude the current interval from the historical totals.
                    long add = s_addPressure[0] + s_addPressure[1] + s_addPressure[2] + s_addPressure[3] - s_addPressure[p];
                    long rem = s_removePressure[0] + s_removePressure[1] + s_removePressure[2] + s_removePressure[3] - s_removePressure[p];
                    long budget = minBudget;

                    // Wait until enough history is available before adjusting the budget.
                    if (s_iteration >= PressureCount)
                    {
                        // Use the historical add/remove ratio to estimate how effectively
                        // collections reclaim the unmanaged memory represented by pressure.
                        if (add >= rem * MaxGCMemoryPressureRatio)
                        {
                            budget = minBudget * MaxGCMemoryPressureRatio;
                        }
                        else if (add > rem)
                        {
                            Debug.Assert(rem != 0);

                            // Calculate the add/remove ratio as fixed point (1024 represents 1).
                            budget = (add * 1024 / rem) * budget / 1024;
                        }
                    }

                    // If still over budget, account for the current managed-heap size.
                    if (newMemValue >= budget)
                    {
                        // Preserve signed estimates, which can be negative with an enlarged NoGC budget.
                        long heapOver3 = GetCurrentObjSize() / 3;
                        if (budget < heapOver3)
                        {
                            budget = heapOver3;
                        }

                        if (newMemValue >= budget)
                        {
                            // Avoid inducing collections too frequently: require more than five
                            // times the last gen2 GC's duration to elapse since that GC started.
                            if ((GetGCNow() - GetLastGCStartTime()) > (GetLastGCDuration() * 5))
                            {
                                Collect(2, GCCollectionMode.Default, blocking: false);
                                CheckCollectionCount();
                            }
                        }
                    }
                }
            }

            internal static void Remove(long bytesAllocated)
            {
                CheckCollectionCount();
                uint p = s_iteration % PressureCount;
                if (NativeRuntimeEventSource.Log.IsEnabled() &&
                    NativeRuntimeEventSource.Log.IsEnabled(EventLevel.Verbose, NativeRuntimeEventSource.Keywords.GCKeyword))
                {
                    NativeRuntimeEventSource.Log.DecreaseMemoryPressure((ulong)bytesAllocated);
                }
                InterlockedAddMemoryPressure(ref s_removePressure[p], bytesAllocated);
            }
        }
#endif

        /// <summary>
        /// Returns, in a specified time-out period, the status of a registered notification for determining whether a full,
        /// blocking garbage collection by the common language runtime is imminent.
        /// </summary>
        /// <param name="timeout">The timeout on waiting for a full GC approach</param>
        /// <returns>The status of a registered full GC notification</returns>
        public static GCNotificationStatus WaitForFullGCApproach(TimeSpan timeout)
            => WaitForFullGCApproach(WaitHandle.ToTimeoutMilliseconds(timeout));

        /// <summary>
        /// Returns the status of a registered notification about whether a blocking garbage collection
        /// has completed. May wait indefinitely for a full collection.
        /// </summary>
        /// <param name="timeout">The timeout on waiting for a full collection</param>
        /// <returns>The status of a registered full GC notification</returns>
        public static GCNotificationStatus WaitForFullGCComplete(TimeSpan timeout)
            => WaitForFullGCComplete(WaitHandle.ToTimeoutMilliseconds(timeout));
    }
}
