// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    public static partial class GC
    {
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

#if !MONO
        // Support for AddMemoryPressure and RemoveMemoryPressure below.
        private const uint PressureCount = 4;
#if TARGET_64BIT
        private const uint MinGCMemoryPressureBudget = 4 * 1024 * 1024;
#else
        private const uint MinGCMemoryPressureBudget = 3 * 1024 * 1024;
#endif

        private const uint MaxGCMemoryPressureRatio = 10;

        private static int[] s_gcCounts = new int[] { 0, 0, 0 };

        private static long[] s_addPressure = new long[] { 0, 0, 0, 0 };
        private static long[] s_removePressure = new long[] { 0, 0, 0, 0 };
        private static uint s_iteration;

        /// <summary>
        /// Resets the pressure accounting after a gen2 GC has occurred.
        /// </summary>
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

                // check for overflow
                if (newMemValue < oldMemValue)
                {
                    newMemValue = long.MaxValue;
                }
            } while (Interlocked.CompareExchange(ref pAugend, newMemValue, oldMemValue) != oldMemValue);

            return newMemValue;
        }

        /// <summary>
        /// New AddMemoryPressure implementation (used by RCW and the CLRServicesImpl class)
        /// 1. Less sensitive than the original implementation (start budget 3 MB)
        /// 2. Focuses more on newly added memory pressure
        /// 3. Budget adjusted by effectiveness of last 3 triggered GC (add / remove ratio, max 10x)
        /// 4. Budget maxed with 30% of current managed GC size
        /// 5. If Gen2 GC is happening naturally, ignore past pressure
        ///
        /// Here's a brief description of the ideal algorithm for Add/Remove memory pressure:
        /// Do a GC when (HeapStart is less than X * MemPressureGrowth) where
        /// - HeapStart is GC Heap size after doing the last GC
        /// - MemPressureGrowth is the net of Add and Remove since the last GC
        /// - X is proportional to our guess of the ummanaged memory death rate per GC interval,
        /// and would be calculated based on historic data using standard exponential approximation:
        /// Xnew = UMDeath/UMTotal * 0.5 + Xprev
        /// </summary>
        /// <param name="bytesAllocated"></param>
        public static void AddMemoryPressure(long bytesAllocated)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated);
#if !TARGET_64BIT
            ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesAllocated, int.MaxValue);
#endif

            CheckCollectionCount();
            uint p = s_iteration % PressureCount;
            long newMemValue = InterlockedAddMemoryPressure(ref s_addPressure[p], bytesAllocated);

            Debug.Assert(PressureCount == 4, "GC.AddMemoryPressure contains unrolled loops which depend on the PressureCount");

            SendEtwAddMemoryPressureEvent((ulong)bytesAllocated);

            if (newMemValue >= MinGCMemoryPressureBudget)
            {
                long add = s_addPressure[0] + s_addPressure[1] + s_addPressure[2] + s_addPressure[3] - s_addPressure[p];
                long rem = s_removePressure[0] + s_removePressure[1] + s_removePressure[2] + s_removePressure[3] - s_removePressure[p];

                long budget = MinGCMemoryPressureBudget;

                if (s_iteration >= PressureCount)  // wait until we have enough data points
                {
                    // Adjust according to effectiveness of GC
                    // Scale budget according to past m_addPressure / m_remPressure ratio
                    if (add >= rem * MaxGCMemoryPressureRatio)
                    {
                        budget = MinGCMemoryPressureBudget * MaxGCMemoryPressureRatio;
                    }
                    else if (add > rem)
                    {
                        Debug.Assert(rem != 0);

                        // Avoid overflow by calculating addPressure / remPressure as fixed point (1 = 1024)
                        budget = (add * 1024 / rem) * budget / 1024;
                    }
                }

                // If still over budget, check current managed heap size
                if (newMemValue >= budget)
                {
                    long heapOver3 = _GetCurrentObjSize() / 3;

                    if (budget < heapOver3)  //Max
                    {
                        budget = heapOver3;
                    }

                    if (newMemValue >= budget)
                    {
                        // last check - if we would exceed 20% of GC "duty cycle", do not trigger GC at this time
                        if ((_GetNow() - _GetLastGCStartTime(2)) > (_GetLastGCDuration(2) * 5))
                        {
                            _Collect(2, (int)InternalGCCollectionMode.NonBlocking, lowMemoryPressure: false);
                            CheckCollectionCount();
                        }
                    }
                }
            }
        }

        public static void RemoveMemoryPressure(long bytesAllocated)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated);
#if !TARGET_64BIT
            ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesAllocated, int.MaxValue);
#endif

            CheckCollectionCount();
            uint p = s_iteration % PressureCount;
            SendEtwRemoveMemoryPressureEvent((ulong)bytesAllocated);
            InterlockedAddMemoryPressure(ref s_removePressure[p], bytesAllocated);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long _GetCurrentObjSize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long _GetNow();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long _GetLastGCStartTime(int generation);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long _GetLastGCDuration(int generation);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SendEtwAddMemoryPressureEvent(ulong bytesAllocated);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SendEtwRemoveMemoryPressureEvent(ulong bytesAllocated);
#endif // !MONO
    }
}
