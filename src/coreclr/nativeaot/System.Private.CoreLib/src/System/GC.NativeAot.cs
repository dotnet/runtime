// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Exposes features of the Garbage Collector to managed code.
//

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime.CompilerServices;
using Internal.Runtime;

namespace System
{
    // !!!!!!!!!!!!!!!!!!!!!!!
    // Make sure you change the def in rtu\gc.h if you change this!
    public enum GCCollectionMode
    {
        Default = 0,
        Forced = 1,
        Optimized = 2,
        Aggressive = 3,
    }

    public enum GCNotificationStatus
    {
        Succeeded     = 0,
        Failed        = 1,
        Canceled      = 2,
        Timeout       = 3,
        NotApplicable = 4
    }

    internal enum InternalGCCollectionMode
    {
        NonBlocking = 0x00000001,
        Blocking = 0x00000002,
        Optimized = 0x00000004,
        Compacting = 0x00000008,
        Aggressive = 0x00000010
    }

    internal enum StartNoGCRegionStatus
    {
        Succeeded = 0,
        NotEnoughMemory = 1,
        AmountTooLarge = 2,
        AlreadyInProgress = 3
    }

    internal enum EndNoGCRegionStatus
    {
        Succeeded = 0,
        NotInProgress = 1,
        GCInduced = 2,
        AllocationExceeded = 3
    }

    internal enum RefreshMemoryStatus
    {
        Succeeded = 0,
        HardLimitTooLow = 1,
        HardLimitInvalid = 2,
    }

    internal enum EnableNoGCRegionCallbackStatus
    {
        Success,
        NotStarted,
        InsufficientBudget,
        AlreadyRegistered,
    }

    public static partial class GC
    {
        public static int GetGeneration(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            return RuntimeImports.RhGetGeneration(obj);
        }

        internal static int GetGenerationSize(int gen)
        {
            return RuntimeImports.RhGetGenerationSize(gen);
        }

        internal static int GetLastGCPercentTimeInGC()
        {
            return RuntimeImports.RhGetLastGCPercentTimeInGC();
        }

        /// <summary>
        /// Returns the current generation number of the target
        /// of a specified <see cref="System.WeakReference"/>.
        /// </summary>
        /// <param name="wo">The WeakReference whose target is the object
        /// whose generation will be returned</param>
        /// <returns>The generation of the target of the WeakReference</returns>
        /// <exception cref="ArgumentNullException">The target of the weak reference
        /// has already been garbage collected.</exception>
        public static int GetGeneration(WeakReference wo)
        {
            // note - this throws an NRE if given a null weak reference. This isn't
            // documented, but it's the behavior of Desktop and CoreCLR.
            object? obj = RuntimeImports.RhHandleGet(wo.WeakHandle);
            KeepAlive(wo);

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(wo));
            }

            return RuntimeImports.RhGetGeneration(obj);
        }

        // Forces a collection of all generations from 0 through Generation.
        public static void Collect(int generation)
        {
            Collect(generation, GCCollectionMode.Default);
        }

        // Garbage collect all generations.
        public static void Collect()
        {
            //-1 says to GC all generations.
            RuntimeImports.RhCollect(-1, InternalGCCollectionMode.Blocking);
        }

        public static void Collect(int generation, GCCollectionMode mode)
        {
            Collect(generation, mode, true);
        }

        public static void Collect(int generation, GCCollectionMode mode, bool blocking)
        {
            bool aggressive = generation == MaxGeneration && mode == GCCollectionMode.Aggressive;
            Collect(generation, mode, blocking, compacting: aggressive);
        }

        public static void Collect(int generation, GCCollectionMode mode, bool blocking, bool compacting)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(generation);

            if ((mode < GCCollectionMode.Default) || (mode > GCCollectionMode.Aggressive))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), SR.ArgumentOutOfRange_Enum);
            }

            int iInternalModes = 0;

            if (mode == GCCollectionMode.Optimized)
            {
                iInternalModes |= (int)InternalGCCollectionMode.Optimized;
            }
            else if (mode == GCCollectionMode.Aggressive)
            {
                iInternalModes |= (int)InternalGCCollectionMode.Aggressive;
                if (generation != MaxGeneration)
                {
                    throw new ArgumentException(SR.Argument_AggressiveGCRequiresMaxGeneration, nameof(generation));
                }
                if (!blocking)
                {
                    throw new ArgumentException(SR.Argument_AggressiveGCRequiresBlocking, nameof(blocking));
                }
                if (!compacting)
                {
                    throw new ArgumentException(SR.Argument_AggressiveGCRequiresCompacting, nameof(compacting));
                }
            }

            if (compacting)
            {
                iInternalModes |= (int)InternalGCCollectionMode.Compacting;
            }

            if (blocking)
            {
                iInternalModes |= (int)InternalGCCollectionMode.Blocking;
            }
            else if (!compacting)
            {
                iInternalModes |= (int)InternalGCCollectionMode.NonBlocking;
            }

            RuntimeImports.RhCollect(generation, (InternalGCCollectionMode)iInternalModes);
        }

        /// <summary>
        /// Specifies that a garbage collection notification should be raised when conditions are favorable
        /// for a full garbage collection and when the collection has been completed.
        /// </summary>
        /// <param name="maxGenerationThreshold">A number between 1 and 99 that specifies when the notification
        /// should be raised based on the objects allocated in Gen 2.</param>
        /// <param name="largeObjectHeapThreshold">A number between 1 and 99 that specifies when the notification
        /// should be raised based on the objects allocated in the large object heap.</param>
        /// <exception cref="ArgumentOutOfRangeException">If either of the two arguments are not between 1 and 99</exception>
        /// <exception cref="InvalidOperationException">If Concurrent GC is enabled</exception>"
        public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold)
        {
            if (maxGenerationThreshold < 1 || maxGenerationThreshold > 99)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxGenerationThreshold),
                    SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, 99));
            }

            if (largeObjectHeapThreshold < 1 || largeObjectHeapThreshold > 99)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(largeObjectHeapThreshold),
                    SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, 99));
            }

            // This is not documented on MSDN, but CoreCLR throws when the GC's
            // RegisterForFullGCNotification returns false
            if (!RuntimeImports.RhRegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold))
            {
                throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
            }
        }

        /// <summary>
        /// Returns the status of a registered notification about whether a blocking garbage collection
        /// is imminent. May wait indefinitely for a full collection.
        /// </summary>
        /// <returns>The status of a registered full GC notification</returns>
        public static GCNotificationStatus WaitForFullGCApproach()
        {
            return (GCNotificationStatus)RuntimeImports.RhWaitForFullGCApproach(-1);
        }

        /// <summary>
        /// Returns the status of a registered notification about whether a blocking garbage collection
        /// is imminent. May wait up to a given timeout for a full collection.
        /// </summary>
        /// <param name="millisecondsTimeout">The timeout on waiting for a full collection</param>
        /// <returns>The status of a registered full GC notification</returns>
        public static GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            return (GCNotificationStatus)RuntimeImports.RhWaitForFullGCApproach(millisecondsTimeout);
        }

        /// <summary>
        /// Returns the status of a registered notification about whether a blocking garbage collection
        /// has completed. May wait indefinitely for a full collection.
        /// </summary>
        /// <returns>The status of a registered full GC notification</returns>
        public static GCNotificationStatus WaitForFullGCComplete()
        {
            return (GCNotificationStatus)RuntimeImports.RhWaitForFullGCComplete(-1);
        }

        /// <summary>
        /// Returns the status of a registered notification about whether a blocking garbage collection
        /// has completed. May wait up to a specified timeout for a full collection.
        /// </summary>
        /// <param name="millisecondsTimeout">The timeout on waiting for a full collection</param>
        /// <returns></returns>
        public static GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            return (GCNotificationStatus)RuntimeImports.RhWaitForFullGCComplete(millisecondsTimeout);
        }

        /// <summary>
        /// Cancels an outstanding full GC notification.
        /// </summary>
        /// <exception cref="InvalidOperationException">Raised if called
        /// with concurrent GC enabled</exception>
        public static void CancelFullGCNotification()
        {
            if (!RuntimeImports.RhCancelFullGCNotification())
            {
                throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
            }
        }

        /// <summary>
        /// Attempts to disallow garbage collection during execution of a critical path.
        /// </summary>
        /// <param name="totalSize">Disallows garbage collection if a specified amount of
        /// of memory is available.</param>
        /// <returns>True if the disallowing of garbage collection was successful, False otherwise</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the amount of memory requested
        /// is too large for the GC to accommodate</exception>
        /// <exception cref="InvalidOperationException">If the GC is already in a NoGCRegion</exception>
        public static bool TryStartNoGCRegion(long totalSize)
        {
            return StartNoGCRegionWorker(totalSize, false, 0, false);
        }

        private unsafe struct NoGCRegionCallbackFinalizerWorkItem
        {
            // FinalizerWorkItem
            public NoGCRegionCallbackFinalizerWorkItem* next;
            public delegate* unmanaged<NoGCRegionCallbackFinalizerWorkItem*, void> callback;

            public bool scheduled;
            public bool abandoned;

            public GCHandle action;
        }

        public static unsafe void RegisterNoGCRegionCallback(long totalSize, Action callback)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSize);
            ArgumentNullException.ThrowIfNull(callback);

            NoGCRegionCallbackFinalizerWorkItem* pWorkItem = null;
            try
            {
                pWorkItem = (NoGCRegionCallbackFinalizerWorkItem*)NativeMemory.AllocZeroed((nuint)sizeof(NoGCRegionCallbackFinalizerWorkItem));
                pWorkItem->action = GCHandle.Alloc(callback);
                pWorkItem->callback = &Callback;

                EnableNoGCRegionCallbackStatus status = (EnableNoGCRegionCallbackStatus)RuntimeImports.RhEnableNoGCRegionCallback(pWorkItem, totalSize);
                if (status != EnableNoGCRegionCallbackStatus.Success)
                {
                    switch (status)
                    {
                        case EnableNoGCRegionCallbackStatus.NotStarted:
                            throw new InvalidOperationException(SR.Format(SR.InvalidOperationException_NoGCRegionNotInProgress));
                        case EnableNoGCRegionCallbackStatus.InsufficientBudget:
                            throw new InvalidOperationException(SR.Format(SR.InvalidOperationException_NoGCRegionAllocationExceeded));
                        case EnableNoGCRegionCallbackStatus.AlreadyRegistered:
                            throw new InvalidOperationException(SR.InvalidOperationException_NoGCRegionCallbackAlreadyRegistered);
                    }
                    Debug.Assert(false);
                }
                pWorkItem = null; // Ownership transferred
            }
            finally
            {
                if (pWorkItem != null)
                    Free(pWorkItem);
            }

            [UnmanagedCallersOnly]
            static void Callback(NoGCRegionCallbackFinalizerWorkItem* pWorkItem)
            {
                Debug.Assert(pWorkItem->scheduled);
                if (!pWorkItem->abandoned)
                    ((Action)(pWorkItem->action.Target!))();
                Free(pWorkItem);
            }

            static void Free(NoGCRegionCallbackFinalizerWorkItem* pWorkItem)
            {
                if (pWorkItem->action.IsAllocated)
                    pWorkItem->action.Free();
                NativeMemory.Free(pWorkItem);
            }
        }

        /// <summary>
        /// Attempts to disallow garbage collection during execution of a critical path.
        /// </summary>
        /// <param name="totalSize">Disallows garbage collection if a specified amount of
        /// of memory is available.</param>
        /// <param name="lohSize">Disallows garbagte collection if a specified amount of
        /// large object heap space is available.</param>
        /// <returns>True if the disallowing of garbage collection was successful, False otherwise</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the amount of memory requested
        /// is too large for the GC to accommodate</exception>
        /// <exception cref="InvalidOperationException">If the GC is already in a NoGCRegion</exception>
        public static bool TryStartNoGCRegion(long totalSize, long lohSize)
        {
            return StartNoGCRegionWorker(totalSize, true, lohSize, false);
        }

        /// <summary>
        /// Attempts to disallow garbage collection during execution of a critical path.
        /// </summary>
        /// <param name="totalSize">Disallows garbage collection if a specified amount of
        /// of memory is available.</param>
        /// <param name="disallowFullBlockingGC">Controls whether or not a full blocking GC
        /// is performed if the requested amount of memory is not available</param>
        /// <returns>True if the disallowing of garbage collection was successful, False otherwise</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the amount of memory requested
        /// is too large for the GC to accommodate</exception>
        /// <exception cref="InvalidOperationException">If the GC is already in a NoGCRegion</exception>
        public static bool TryStartNoGCRegion(long totalSize, bool disallowFullBlockingGC)
        {
            return StartNoGCRegionWorker(totalSize, false, 0, disallowFullBlockingGC);
        }

        /// <summary>
        /// Attempts to disallow garbage collection during execution of a critical path.
        /// </summary>
        /// <param name="totalSize">Disallows garbage collection if a specified amount of
        /// of memory is available.</param>
        /// <param name="lohSize">Disallows garbagte collection if a specified amount of
        /// large object heap space is available.</param>
        /// <param name="disallowFullBlockingGC">Controls whether or not a full blocking GC
        /// is performed if the requested amount of memory is not available</param>
        /// <returns>True if the disallowing of garbage collection was successful, False otherwise</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the amount of memory requested
        /// is too large for the GC to accommodate</exception>
        /// <exception cref="InvalidOperationException">If the GC is already in a NoGCRegion</exception>
        public static bool TryStartNoGCRegion(long totalSize, long lohSize, bool disallowFullBlockingGC)
        {
            return StartNoGCRegionWorker(totalSize, true, lohSize, disallowFullBlockingGC);
        }

        private static bool StartNoGCRegionWorker(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSize);
            if (hasLohSize)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lohSize);
                if (lohSize > totalSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(lohSize), SR.ArgumentOutOfRange_NoGCLohSizeGreaterTotalSize);
                }
            }

            StartNoGCRegionStatus status =
                (StartNoGCRegionStatus)RuntimeImports.RhStartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC);
            switch (status)
            {
                case StartNoGCRegionStatus.NotEnoughMemory:
                    return false;
                case StartNoGCRegionStatus.AlreadyInProgress:
                    throw new InvalidOperationException(SR.InvalidOperationException_AlreadyInNoGCRegion);
                case StartNoGCRegionStatus.AmountTooLarge:
                    throw new ArgumentOutOfRangeException(nameof(totalSize), SR.ArgumentOutOfRangeException_NoGCRegionSizeTooLarge);
            }

            Debug.Assert(status == StartNoGCRegionStatus.Succeeded);
            return true;
        }

        /// <summary>
        /// Exits the current no GC region.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the GC is not in a no GC region</exception>
        /// <exception cref="InvalidOperationException">If the no GC region was exited due to an induced GC</exception>
        /// <exception cref="InvalidOperationException">If the no GC region was exited due to memory allocations
        /// exceeding the amount given to <see cref="TryStartNoGCRegion(long)"/></exception>
        public static void EndNoGCRegion()
        {
            EndNoGCRegionStatus status = (EndNoGCRegionStatus)RuntimeImports.RhEndNoGCRegion();
            if (status == EndNoGCRegionStatus.NotInProgress)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperationException_NoGCRegionNotInProgress);
            }
            else if (status == EndNoGCRegionStatus.GCInduced)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperationException_NoGCRegionInduced);
            }
            else if (status == EndNoGCRegionStatus.AllocationExceeded)
            {
                throw new InvalidOperationException(
                    SR.InvalidOperationException_NoGCRegionAllocationExceeded);
            }
        }

        // Block until the next finalization pass is complete.
        public static void WaitForPendingFinalizers()
        {
            RuntimeImports.RhWaitForPendingFinalizers(Thread.ReentrantWaitsEnabled);
        }

        public static void SuppressFinalize(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            RuntimeImports.RhSuppressFinalize(obj);
        }

        public static void ReRegisterForFinalize(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            RuntimeImports.RhReRegisterForFinalize(obj);
        }

        [Intrinsic]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void KeepAlive(object? obj)
        {
        }

        // Returns the maximum GC generation.  Currently assumes only 1 heap.
        //
        public static int MaxGeneration
        {
            get { return RuntimeImports.RhGetMaxGcGeneration(); }
        }

        public static int CollectionCount(int generation)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(generation);

            return RuntimeImports.RhGetGcCollectionCount(generation, false);
        }

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
                    long heapOver3 = RuntimeImports.RhGetCurrentObjSize() / 3;

                    if (budget < heapOver3)  //Max
                    {
                        budget = heapOver3;
                    }

                    if (newMemValue >= budget)
                    {
                        // last check - if we would exceed 20% of GC "duty cycle", do not trigger GC at this time
                        if ((RuntimeImports.RhGetGCNow() - RuntimeImports.RhGetLastGCStartTime(2)) > (RuntimeImports.RhGetLastGCDuration(2) * 5))
                        {
                            RuntimeImports.RhCollect(2, InternalGCCollectionMode.NonBlocking);
                            CheckCollectionCount();
                        }
                    }
                }
            }
        }

        internal struct GCConfigurationContext
        {
            internal Dictionary<string, object> Configurations;
        }

        [UnmanagedCallersOnly]
        private static unsafe void ConfigCallback(void* configurationContext, void* name, void* publicKey, RuntimeImports.GCConfigurationType type, long data)
        {
            // If the public key is null, it means that the corresponding configuration isn't publicly available
            // and therefore, we shouldn't add it to the configuration dictionary to return to the user.
            if (publicKey == null)
            {
                return;
            }

            Debug.Assert(name != null);
            Debug.Assert(configurationContext != null);

            ref GCConfigurationContext context = ref Unsafe.As<byte, GCConfigurationContext>(ref *(byte*)configurationContext);
            Debug.Assert(context.Configurations != null);
            Dictionary<string, object> configurationDictionary = context.Configurations!;

            string nameAsString = Marshal.PtrToStringUTF8((IntPtr)name)!;
            switch (type)
            {
                case RuntimeImports.GCConfigurationType.Int64:
                    configurationDictionary[nameAsString] = data;
                    break;

                case RuntimeImports.GCConfigurationType.StringUtf8:
                    {
                        string? dataAsString = Marshal.PtrToStringUTF8((nint)data);
                        configurationDictionary[nameAsString] = dataAsString ?? string.Empty;
                        break;
                    }

                case RuntimeImports.GCConfigurationType.Boolean:
                    configurationDictionary![nameAsString] = data != 0;
                    break;
            }
        }

        /// <summary>
        /// Gets the Configurations used by the Garbage Collector. The value of these configurations used don't necessarily have to be the same as the ones that are passed by the user.
        /// For example for the "GCHeapCount" configuration, if the user supplies a value higher than the number of CPUs, the configuration that will be used is that of the number of CPUs.
        /// </summary>
        /// <returns> A Read Only Dictionary with configuration names and values of the configuration as the keys and values of the dictionary, respectively.</returns>
        public static unsafe IReadOnlyDictionary<string, object> GetConfigurationVariables()
        {
            GCConfigurationContext context = new GCConfigurationContext
            {
                Configurations = new Dictionary<string, object>()
            };

            RuntimeImports.RhEnumerateConfigurationValues(Unsafe.AsPointer(ref context), &ConfigCallback);
            return context.Configurations!;
        }

        public static void RemoveMemoryPressure(long bytesAllocated)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bytesAllocated);
#if !TARGET_64BIT
            ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesAllocated, int.MaxValue);
#endif

            CheckCollectionCount();
            uint p = s_iteration % PressureCount;
            InterlockedAddMemoryPressure(ref s_removePressure[p], bytesAllocated);
        }

        public static long GetTotalMemory(bool forceFullCollection)
        {
            long size = RuntimeImports.RhGetGcTotalMemory();

            if (forceFullCollection)
            {
                // If we force a full collection, we will run the finalizers on all
                // existing objects and do a collection until the value stabilizes.
                // The value is "stable" when either the value is within 5% of the
                // previous call to GetTotalMemory, or if we have been sitting
                // here for more than x times (we don't want to loop forever here).
                int reps = 20;  // Number of iterations

                long diff;

                do
                {
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    long newSize = RuntimeImports.RhGetGcTotalMemory();
                    diff = (newSize - size) * 100 / size;
                    size = newSize;
                }
                while (reps-- > 0 && !(-5 < diff && diff < 5));
            }

            return size;
        }

        private static IntPtr _RegisterFrozenSegment(IntPtr sectionAddress, IntPtr sectionSize)
        {
            return RuntimeImports.RhpRegisterFrozenSegment(sectionAddress, sectionSize);
        }

        private static void _UnregisterFrozenSegment(IntPtr segmentHandle)
        {
            RuntimeImports.RhpUnregisterFrozenSegment(segmentHandle);
        }

        public static long GetAllocatedBytesForCurrentThread()
        {
            return RuntimeImports.RhGetAllocatedBytesForCurrentThread();
        }

        public static long GetTotalAllocatedBytes(bool precise = false)
        {
            return precise ? RuntimeImports.RhGetTotalAllocatedBytesPrecise() : RuntimeImports.RhGetTotalAllocatedBytes();
        }

        /// <summary>Gets garbage collection memory information.</summary>
        /// <returns>An object that contains information about the garbage collector's memory usage.</returns>
        public static GCMemoryInfo GetGCMemoryInfo() => GetGCMemoryInfo(GCKind.Any);

        /// <summary>Gets garbage collection memory information.</summary>
        /// <param name="kind">The kind of collection for which to retrieve memory information.</param>
        /// <returns>An object that contains information about the garbage collector's memory usage.</returns>
        public static GCMemoryInfo GetGCMemoryInfo(GCKind kind)
        {
            if ((kind < GCKind.Any) || (kind > GCKind.Background))
            {
                throw new ArgumentOutOfRangeException(nameof(kind),
                                      SR.Format(
                                          SR.ArgumentOutOfRange_Bounds_Lower_Upper,
                                          GCKind.Any,
                                          GCKind.Background));
            }

            var data = new GCMemoryInfoData();
            RuntimeImports.RhGetMemoryInfo(ref data.GetRawData(), kind);
            return new GCMemoryInfo(data);
        }

        internal static ulong GetSegmentSize()
        {
            return RuntimeImports.RhGetGCSegmentSize();
        }

        /// <summary>
        /// Allocate an array while skipping zero-initialization if possible.
        /// </summary>
        /// <typeparam name="T">Specifies the type of the array element.</typeparam>
        /// <param name="length">Specifies the length of the array.</param>
        /// <param name="pinned">Specifies whether the allocated array must be pinned.</param>
        /// <remarks>
        /// If pinned is set to true, <typeparamref name="T"/> must not be a reference type or a type that contains object references.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // forced to ensure no perf drop for small memory buffers (hot path)
        public static unsafe T[] AllocateUninitializedArray<T>(int length, bool pinned = false)
        {
            if (!pinned)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    return new T[length];
                }

                // for debug builds we always want to call AllocateNewArray to detect AllocateNewArray bugs
#if !DEBUG
                // small arrays are allocated using `new[]` as that is generally faster.
#pragma warning disable 8500 // sizeof of managed types
                if (length < 2048 / sizeof(T))
#pragma warning restore 8500
                {
                    return new T[length];
                }
#endif
            }
            else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
            }

            // kept outside of the small arrays hot path to have inlining without big size growth
            return AllocateNewUninitializedArray(length, pinned);

            static T[] AllocateNewUninitializedArray(int length, bool pinned)
            {
                GC_ALLOC_FLAGS flags = GC_ALLOC_FLAGS.GC_ALLOC_ZEROING_OPTIONAL;
                if (pinned)
                    flags |= GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP;

                if (length < 0)
                    throw new OverflowException();

                T[]? array = null;
                RuntimeImports.RhAllocateNewArray(EETypePtr.EETypePtrOf<T[]>().RawValue, (uint)length, (uint)flags, Unsafe.AsPointer(ref array));
                if (array == null)
                    throw new OutOfMemoryException();

                return array;
            }
        }

        /// <summary>
        /// Allocate an array.
        /// </summary>
        /// <typeparam name="T">Specifies the type of the array element.</typeparam>
        /// <param name="length">Specifies the length of the array.</param>
        /// <param name="pinned">Specifies whether the allocated array must be pinned.</param>
        /// <remarks>
        /// If pinned is set to true, <typeparamref name="T"/> must not be a reference type or a type that contains object references.
        /// </remarks>
        public static unsafe T[] AllocateArray<T>(int length, bool pinned = false)
        {
            GC_ALLOC_FLAGS flags = GC_ALLOC_FLAGS.GC_ALLOC_NO_FLAGS;

            if (pinned)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));

                flags = GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP;
            }

            if (length < 0)
                throw new OverflowException();

            T[]? array = null;
            RuntimeImports.RhAllocateNewArray(EETypePtr.EETypePtrOf<T[]>().RawValue, (uint)length, (uint)flags, Unsafe.AsPointer(ref array));
            if (array == null)
                throw new OutOfMemoryException();

            return array;
        }

        public static TimeSpan GetTotalPauseDuration()
        {
            return new TimeSpan(RuntimeImports.RhGetTotalPauseDuration());
        }

        [System.Runtime.Versioning.RequiresPreviewFeaturesAttribute("RefreshMemoryLimit is in preview.")]
        public static void RefreshMemoryLimit()
        {
            ulong heapHardLimit = (AppContext.GetData("GCHeapHardLimit") as ulong?) ?? ulong.MaxValue;
            ulong heapHardLimitPercent = (AppContext.GetData("GCHeapHardLimitPercent") as ulong?) ?? ulong.MaxValue;
            ulong heapHardLimitSOH = (AppContext.GetData("GCHeapHardLimitSOH") as ulong?) ?? ulong.MaxValue;
            ulong heapHardLimitLOH = (AppContext.GetData("GCHeapHardLimitLOH") as ulong?) ?? ulong.MaxValue;
            ulong heapHardLimitPOH = (AppContext.GetData("GCHeapHardLimitPOH") as ulong?) ?? ulong.MaxValue;
            ulong heapHardLimitSOHPercent = (AppContext.GetData("GCHeapHardLimitSOHPercent") as ulong?) ?? ulong.MaxValue;
            ulong heapHardLimitLOHPercent = (AppContext.GetData("GCHeapHardLimitLOHPercent") as ulong?) ?? ulong.MaxValue;
            ulong heapHardLimitPOHPercent = (AppContext.GetData("GCHeapHardLimitPOHPercent") as ulong?) ?? ulong.MaxValue;
            RuntimeImports.GCHeapHardLimitInfo heapHardLimitInfo = new RuntimeImports.GCHeapHardLimitInfo
            {
                HeapHardLimit = heapHardLimit,
                HeapHardLimitPercent = heapHardLimitPercent,
                HeapHardLimitSOH = heapHardLimitSOH,
                HeapHardLimitLOH = heapHardLimitLOH,
                HeapHardLimitPOH = heapHardLimitPOH,
                HeapHardLimitSOHPercent = heapHardLimitSOHPercent,
                HeapHardLimitLOHPercent = heapHardLimitLOHPercent,
                HeapHardLimitPOHPercent = heapHardLimitPOHPercent,
            };
            RefreshMemoryStatus status = (RefreshMemoryStatus)RuntimeImports.RhRefreshMemoryLimit(heapHardLimitInfo);
            switch (status)
            {
                case RefreshMemoryStatus.HardLimitTooLow:
                    throw new InvalidOperationException(SR.InvalidOperationException_HardLimitTooLow);
                case RefreshMemoryStatus.HardLimitInvalid:
                    throw new InvalidOperationException(SR.InvalidOperationException_HardLimitInvalid);
            }
            Debug.Assert(status == RefreshMemoryStatus.Succeeded);
        }
    }
}
