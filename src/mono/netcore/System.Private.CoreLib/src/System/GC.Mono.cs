// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    public enum GCCollectionMode
    {
        Default = 0,
        Forced = 1,
        Optimized = 2
    }

    public enum GCNotificationStatus
    {
        Succeeded = 0,
        Failed = 1,
        Canceled = 2,
        Timeout = 3,
        NotApplicable = 4
    }

    public static partial class GC
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetCollectionCount(int generation);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetMaxGeneration();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InternalCollect(int generation);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void RecordPressure(long bytesAllocated);

        // TODO: Move following to ConditionalWeakTable
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void register_ephemeron_array(Ephemeron[] array);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object get_ephemeron_tombstone();

        internal static readonly object EPHEMERON_TOMBSTONE = get_ephemeron_tombstone();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long GetAllocatedBytesForCurrentThread();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long GetTotalAllocatedBytes(bool precise = false);

        public static void AddMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated <= 0)
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated), SR.ArgumentOutOfRange_NeedPosNum);
            if (IntPtr.Size == 4 && bytesAllocated > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated), SR.ArgumentOutOfRange_MustBeNonNegInt32);
            RecordPressure(bytesAllocated);
        }

        public static void RemoveMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated <= 0)
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated), SR.ArgumentOutOfRange_NeedPosNum);
            if (IntPtr.Size == 4 && bytesAllocated > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated), SR.ArgumentOutOfRange_MustBeNonNegInt32);
            RecordPressure(-bytesAllocated);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern int GetGeneration(object obj);

        public static void Collect(int generation)
        {
            Collect(generation, GCCollectionMode.Default);
        }

        public static void Collect()
        {
            InternalCollect(MaxGeneration);
        }

        public static void Collect(int generation, GCCollectionMode mode) => Collect(generation, mode, true);

        public static void Collect(int generation, GCCollectionMode mode, bool blocking) => Collect(generation, mode, blocking, false);

        public static void Collect(int generation, GCCollectionMode mode, bool blocking, bool compacting)
        {
            if (generation < 0)
                throw new ArgumentOutOfRangeException(nameof(generation), "generation", SR.ArgumentOutOfRange_GenericPositive);
            if ((mode < GCCollectionMode.Default) || (mode > GCCollectionMode.Optimized))
                throw new ArgumentOutOfRangeException(nameof(mode), SR.ArgumentOutOfRange_Enum);

            InternalCollect(generation);
        }

        public static int CollectionCount(int generation)
        {
            if (generation < 0)
                throw new ArgumentOutOfRangeException(nameof(generation), SR.ArgumentOutOfRange_GenericPositive);
            return GetCollectionCount(generation);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
        public static void KeepAlive(object? obj)
        {
        }

        public static int GetGeneration(WeakReference wo)
        {
            object? obj = wo.Target;
            if (obj == null)
                throw new ArgumentException(null, nameof(wo));
            return GetGeneration(obj);
        }

        public static int MaxGeneration
        {
            get
            {
                return GetMaxGeneration();
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void WaitForPendingFinalizers();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _SuppressFinalize(object o);

        public static void SuppressFinalize(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            _SuppressFinalize(obj);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _ReRegisterForFinalize(object o);

        public static void ReRegisterForFinalize(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            _ReRegisterForFinalize(obj);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern long GetTotalMemory(bool forceFullCollection);

        private static bool _RegisterForFullGCNotification(int maxGenerationPercentage, int largeObjectHeapPercentage)
        {
            throw new NotImplementedException();
        }

        private static bool _CancelFullGCNotification()
        {
            throw new NotImplementedException();
        }

        private static GCNotificationStatus _WaitForFullGCApproach(int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        private static GCNotificationStatus _WaitForFullGCComplete(int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold)
        {
            if ((maxGenerationThreshold <= 0) || (maxGenerationThreshold >= 100))
                throw new ArgumentOutOfRangeException(nameof(maxGenerationThreshold),
                                                       SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, 99));
            if ((largeObjectHeapThreshold <= 0) || (largeObjectHeapThreshold >= 100))
                throw new ArgumentOutOfRangeException(nameof(largeObjectHeapThreshold),
                                                       SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, 99));

            if (!_RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold))
                throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
        }

        public static void CancelFullGCNotification()
        {
            if (!_CancelFullGCNotification())
                throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
        }

        public static GCNotificationStatus WaitForFullGCApproach()
        {
            return (GCNotificationStatus)_WaitForFullGCApproach(-1);
        }

        public static GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            return _WaitForFullGCApproach(millisecondsTimeout);
        }

        public static GCNotificationStatus WaitForFullGCComplete()
        {
            return _WaitForFullGCComplete(-1);
        }

        public static GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return _WaitForFullGCComplete(millisecondsTimeout);
        }

        private static bool StartNoGCRegion(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
        {
            throw new NotImplementedException();
        }

        public static bool TryStartNoGCRegion(long totalSize) => StartNoGCRegion(totalSize, false, 0, false);

        public static bool TryStartNoGCRegion(long totalSize, long lohSize) => StartNoGCRegion(totalSize, true, lohSize, false);

        public static bool TryStartNoGCRegion(long totalSize, bool disallowFullBlockingGC) => StartNoGCRegion(totalSize, false, 0, disallowFullBlockingGC);

        public static bool TryStartNoGCRegion(long totalSize, long lohSize, bool disallowFullBlockingGC) => StartNoGCRegion(totalSize, true, lohSize, disallowFullBlockingGC);

        public static void EndNoGCRegion()
        {
            throw new NotImplementedException();
        }

        internal static ulong GetSegmentSize()
        {
            // coreclr default
            return 1024 * 1024 * 16;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _GetGCMemoryInfo(out long highMemoryLoadThresholdBytes,
                                        out long memoryLoadBytes,
                                        out long totalAvailableMemoryBytes,
                                        out long heapSizeBytes,
                                        out long fragmentedBytes);

        public static GCMemoryInfo GetGCMemoryInfo()
        {
            _GetGCMemoryInfo(out long highMemoryLoadThresholdBytes,
                             out long memoryLoadBytes,
                             out long totalAvailableMemoryBytes,
                             out long heapSizeBytes,
                             out long fragmentedBytes);

            return new GCMemoryInfo(highMemoryLoadThresholdBytes, memoryLoadBytes, totalAvailableMemoryBytes, heapSizeBytes, fragmentedBytes);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Array AllocPinnedArray(Type t, int length);

        public static T[] AllocateUninitializedArray<T>(int length, bool pinned = false)
        {
            // Mono only does explicit zeroning if the array is to big for the nursery, but less than 1 Mb - 4 kb.
            // If it is bigger than that, we grab memoroy directly from the OS which comes pre-zeroed.
            // Experimentation shows that if we just skip the zeroing in this case, we do not save a measurable
            // amount of time. So we just allocate the normal way here.
            // Revist if we change LOS implementation.
            return AllocateArray<T>(length, pinned);
        }

        public static T[] AllocateArray<T>(int length, bool pinned = false)
        {
            if (pinned) {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
                return Unsafe.As<T[]>(AllocPinnedArray(typeof(T[]), length));
            }

            return new T[length];
        }
    }
}
