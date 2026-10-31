// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System
{
    public static partial class GC
    {
#if !MONO
        private const uint PressureCount = 4;
        private const uint MaxGCMemoryPressureRatio = 10;

        private static int[] s_gcCounts = new int[] { 0, 0, 0 };
        private static long[] s_addPressure = new long[] { 0, 0, 0, 0 };
        private static long[] s_removePressure = new long[] { 0, 0, 0, 0 };
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

        // Scale the budget using the effectiveness of the preceding three gen2 GC intervals
        // and the current managed heap size, limiting induced collections to a 20% duty cycle.
        public static void AddMemoryPressure(long bytesAllocated)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated);
            if (IntPtr.Size == 4)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesAllocated, int.MaxValue);
            }

            CheckCollectionCount();
            uint p = s_iteration % PressureCount;
            long newMemValue = InterlockedAddMemoryPressure(ref s_addPressure[p], bytesAllocated);
#if CORECLR
            _AddMemoryPressure((ulong)bytesAllocated);
#endif

            Debug.Assert(PressureCount == 4, "GC.AddMemoryPressure contains unrolled loops which depend on the PressureCount");

            long minBudget = IntPtr.Size == 8 ? 4 * 1024 * 1024 : 3 * 1024 * 1024;
            if (newMemValue >= minBudget)
            {
                long add = s_addPressure[0] + s_addPressure[1] + s_addPressure[2] + s_addPressure[3] - s_addPressure[p];
                long rem = s_removePressure[0] + s_removePressure[1] + s_removePressure[2] + s_removePressure[3] - s_removePressure[p];
                long budget = minBudget;

                if (s_iteration >= PressureCount)
                {
                    if (add >= rem * MaxGCMemoryPressureRatio)
                    {
                        budget = minBudget * MaxGCMemoryPressureRatio;
                    }
                    else if (add > rem)
                    {
                        Debug.Assert(rem != 0);

                        // Calculate add / remove as fixed point (1 = 1024).
                        budget = (add * 1024 / rem) * budget / 1024;
                    }
                }

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
                        if ((GetGCNow() - GetLastGCStartTime()) > (GetLastGCDuration() * 5))
                        {
                            Collect(2, GCCollectionMode.Default, blocking: false);
                            CheckCollectionCount();
                        }
                    }
                }
            }
        }

        public static void RemoveMemoryPressure(long bytesAllocated)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated);
            if (IntPtr.Size == 4)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesAllocated, int.MaxValue);
            }

            CheckCollectionCount();
            uint p = s_iteration % PressureCount;
#if CORECLR
            _RemoveMemoryPressure((ulong)bytesAllocated);
#endif
            InterlockedAddMemoryPressure(ref s_removePressure[p], bytesAllocated);
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
