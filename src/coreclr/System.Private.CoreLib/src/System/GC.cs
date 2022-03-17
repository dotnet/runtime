// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: Exposes features of the Garbage Collector through
** the class libraries.  This is a class which cannot be
** instantiated.
**
**
===========================================================*/

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public enum GCCollectionMode
    {
        Default = 0,
        Forced = 1,
        Optimized = 2
    }

    // !!!!!!!!!!!!!!!!!!!!!!!
    // make sure you change the def in gc\gcinterface.h
    // if you change this!
    internal enum InternalGCCollectionMode
    {
        NonBlocking = 0x00000001,
        Blocking = 0x00000002,
        Optimized = 0x00000004,
        Compacting = 0x00000008,
    }

    // !!!!!!!!!!!!!!!!!!!!!!!
    // make sure you change the def in gc\gcinterface.h
    // if you change this!
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
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetMemoryInfo(GCMemoryInfoData data, int kind);

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
            GetMemoryInfo(data, (int)kind);
            return new GCMemoryInfo(data);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_StartNoGCRegion")]
        internal static partial int _StartNoGCRegion(long totalSize, [MarshalAs(UnmanagedType.Bool)] bool lohSizeKnown, long lohSize, [MarshalAs(UnmanagedType.Bool)] bool disallowFullBlockingGC);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_EndNoGCRegion")]
        internal static partial int _EndNoGCRegion();

        // keep in sync with GC_ALLOC_FLAGS in gcinterface.h
        internal enum GC_ALLOC_FLAGS
        {
            GC_ALLOC_NO_FLAGS = 0,
            GC_ALLOC_ZEROING_OPTIONAL = 16,
            GC_ALLOC_PINNED_OBJECT_HEAP = 64,
        };

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern Array AllocateNewArray(IntPtr typeHandle, int length, GC_ALLOC_FLAGS flags);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetGenerationWR(IntPtr handle);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_GetTotalMemory")]
        private static partial long GetTotalMemory();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_Collect")]
        private static partial void _Collect(int generation, int mode);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetMaxGeneration();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int _CollectionCount(int generation, int getSpecialGCCount);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern ulong GetSegmentSize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetLastGCPercentTimeInGC();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern ulong GetGenerationSize(int gen);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_AddMemoryPressure")]
        private static partial void _AddMemoryPressure(ulong bytesAllocated);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_RemoveMemoryPressure")]
        private static partial void _RemoveMemoryPressure(ulong bytesAllocated);

        public static void AddMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated),
                        SR.ArgumentOutOfRange_NeedPosNum);
            }

            if ((4 == IntPtr.Size) && (bytesAllocated > int.MaxValue))
            {
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated),
                    SR.ArgumentOutOfRange_MustBeNonNegInt32);
            }

            _AddMemoryPressure((ulong)bytesAllocated);
        }

        public static void RemoveMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated),
                    SR.ArgumentOutOfRange_NeedPosNum);
            }

            if ((4 == IntPtr.Size) && (bytesAllocated > int.MaxValue))
            {
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated),
                    SR.ArgumentOutOfRange_MustBeNonNegInt32);
            }

            _RemoveMemoryPressure((ulong)bytesAllocated);
        }


        // Returns the generation that obj is currently in.
        //
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int GetGeneration(object obj);


        // Forces a collection of all generations from 0 through Generation.
        //
        public static void Collect(int generation)
        {
            Collect(generation, GCCollectionMode.Default);
        }

        // Garbage Collect all generations.
        //
        public static void Collect()
        {
            // -1 says to GC all generations.
            _Collect(-1, (int)InternalGCCollectionMode.Blocking);
        }

        public static void Collect(int generation, GCCollectionMode mode)
        {
            Collect(generation, mode, true);
        }

        public static void Collect(int generation, GCCollectionMode mode, bool blocking)
        {
            Collect(generation, mode, blocking, false);
        }

        public static void Collect(int generation, GCCollectionMode mode, bool blocking, bool compacting)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(generation), SR.ArgumentOutOfRange_GenericPositive);
            }

            if ((mode < GCCollectionMode.Default) || (mode > GCCollectionMode.Optimized))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), SR.ArgumentOutOfRange_Enum);
            }


            int iInternalModes = 0;

            if (mode == GCCollectionMode.Optimized)
            {
                iInternalModes |= (int)InternalGCCollectionMode.Optimized;
            }

            if (compacting)
                iInternalModes |= (int)InternalGCCollectionMode.Compacting;

            if (blocking)
            {
                iInternalModes |= (int)InternalGCCollectionMode.Blocking;
            }
            else if (!compacting)
            {
                iInternalModes |= (int)InternalGCCollectionMode.NonBlocking;
            }

            _Collect(generation, iInternalModes);
        }

        public static int CollectionCount(int generation)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(generation), SR.ArgumentOutOfRange_GenericPositive);
            }
            return _CollectionCount(generation, 0);
        }

        // This method DOES NOT DO ANYTHING in and of itself.  It's used to
        // prevent a finalizable object from losing any outstanding references
        // a touch too early.  The JIT is very aggressive about keeping an
        // object's lifetime to as small a window as possible, to the point
        // where a 'this' pointer isn't considered live in an instance method
        // unless you read a value from the instance.  So for finalizable
        // objects that store a handle or pointer and provide a finalizer that
        // cleans them up, this can cause subtle race conditions with the finalizer
        // thread.  This isn't just about handles - it can happen with just
        // about any finalizable resource.
        //
        // Users should insert a call to this method right after the last line
        // of their code where their code still needs the object to be kept alive.
        // The object which reference is passed into this method will not
        // be eligible for collection until the call to this method happens.
        // Once the call to this method has happened the object may immediately
        // become eligible for collection. Here is an example:
        //
        // "...all you really need is one object with a Finalize method, and a
        // second object with a Close/Dispose/Done method.  Such as the following
        // contrived example:
        //
        // class Foo {
        //    Stream stream = ...;
        //    protected void Finalize() { stream.Close(); }
        //    void Problem() { stream.MethodThatSpansGCs(); }
        //    static void Main() { new Foo().Problem(); }
        // }
        //
        //
        // In this code, Foo will be finalized in the middle of
        // stream.MethodThatSpansGCs, thus closing a stream still in use."
        //
        // If we insert a call to GC.KeepAlive(this) at the end of Problem(), then
        // Foo doesn't get finalized and the stream stays open.
        [MethodImpl(MethodImplOptions.NoInlining)] // disable optimizations
        [Intrinsic]
        public static void KeepAlive(object? obj)
        {
        }

        // Returns the generation in which wo currently resides.
        //
        public static int GetGeneration(WeakReference wo)
        {
            int result = GetGenerationWR(wo.m_handle);
            KeepAlive(wo);
            return result;
        }

        // Returns the maximum GC generation.  Currently assumes only 1 heap.
        //
        public static int MaxGeneration => GetMaxGeneration();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_WaitForPendingFinalizers")]
        private static partial void _WaitForPendingFinalizers();

        public static void WaitForPendingFinalizers()
        {
            // QCalls can not be exposed directly, need to wrap it.
            _WaitForPendingFinalizers();
        }

        // Indicates that the system should not call the Finalize() method on
        // an object that would normally require this call.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _SuppressFinalize(object o);

        public static void SuppressFinalize(object obj!!)
        {
            _SuppressFinalize(obj);
        }

        // Indicates that the system should call the Finalize() method on an object
        // for which SuppressFinalize has already been called. The other situation
        // where calling ReRegisterForFinalize is useful is inside a finalizer that
        // needs to resurrect itself or an object that it references.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void _ReRegisterForFinalize(object o);

        public static void ReRegisterForFinalize(object obj!!)
        {
            _ReRegisterForFinalize(obj);
        }

        // Returns the total number of bytes currently in use by live objects in
        // the GC heap.  This does not return the total size of the GC heap, but
        // only the live objects in the GC heap.
        //
        public static long GetTotalMemory(bool forceFullCollection)
        {
            long size = GetTotalMemory();
            if (!forceFullCollection)
                return size;
            // If we force a full collection, we will run the finalizers on all
            // existing objects and do a collection until the value stabilizes.
            // The value is "stable" when either the value is within 5% of the
            // previous call to GetTotalMemory, or if we have been sitting
            // here for more than x times (we don't want to loop forever here).
            int reps = 20;  // Number of iterations
            long newSize = size;
            float diff;
            do
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
                size = newSize;
                newSize = GetTotalMemory();
                diff = ((float)(newSize - size)) / size;
            } while (reps-- > 0 && !(-.05 < diff && diff < .05));
            return newSize;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_RegisterFrozenSegment")]
        private static partial IntPtr _RegisterFrozenSegment(IntPtr sectionAddress, nint sectionSize);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCInterface_UnregisterFrozenSegment")]
        private static partial void _UnregisterFrozenSegment(IntPtr segmentHandle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern long GetAllocatedBytesForCurrentThread();


        /// <summary>
        /// Get a count of the bytes allocated over the lifetime of the process.
        /// <param name="precise">If true, gather a precise number, otherwise gather a fairly count. Gathering a precise value triggers at a significant performance penalty.</param>
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern long GetTotalAllocatedBytes(bool precise = false);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool _RegisterForFullGCNotification(int maxGenerationPercentage, int largeObjectHeapPercentage);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool _CancelFullGCNotification();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int _WaitForFullGCApproach(int millisecondsTimeout);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int _WaitForFullGCComplete(int millisecondsTimeout);

        public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold)
        {
            if ((maxGenerationThreshold <= 0) || (maxGenerationThreshold >= 100))
            {
                throw new ArgumentOutOfRangeException(nameof(maxGenerationThreshold),
                                                      SR.Format(
                                                          SR.ArgumentOutOfRange_Bounds_Lower_Upper,
                                                          1,
                                                          99));
            }

            if ((largeObjectHeapThreshold <= 0) || (largeObjectHeapThreshold >= 100))
            {
                throw new ArgumentOutOfRangeException(nameof(largeObjectHeapThreshold),
                                                      SR.Format(
                                                          SR.ArgumentOutOfRange_Bounds_Lower_Upper,
                                                          1,
                                                          99));
            }

            if (!_RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold))
            {
                throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
            }
        }

        public static void CancelFullGCNotification()
        {
            if (!_CancelFullGCNotification())
            {
                throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
            }
        }

        public static GCNotificationStatus WaitForFullGCApproach()
        {
            return (GCNotificationStatus)_WaitForFullGCApproach(-1);
        }

        public static GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);

            return (GCNotificationStatus)_WaitForFullGCApproach(millisecondsTimeout);
        }

        public static GCNotificationStatus WaitForFullGCComplete()
        {
            return (GCNotificationStatus)_WaitForFullGCComplete(-1);
        }

        public static GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return (GCNotificationStatus)_WaitForFullGCComplete(millisecondsTimeout);
        }

        private enum StartNoGCRegionStatus
        {
            Succeeded = 0,
            NotEnoughMemory = 1,
            AmountTooLarge = 2,
            AlreadyInProgress = 3
        }

        private enum EndNoGCRegionStatus
        {
            Succeeded = 0,
            NotInProgress = 1,
            GCInduced = 2,
            AllocationExceeded = 3
        }

        private static bool StartNoGCRegionWorker(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
        {
            if (totalSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalSize), SR.ArgumentOutOfRange_MustBePositive);
            }

            if (hasLohSize)
            {
                if (lohSize <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(lohSize), SR.ArgumentOutOfRange_MustBePositive);
                }

                if (lohSize > totalSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(lohSize), SR.ArgumentOutOfRange_NoGCLohSizeGreaterTotalSize);
                }
            }

            StartNoGCRegionStatus status = (StartNoGCRegionStatus)_StartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC);
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

        public static bool TryStartNoGCRegion(long totalSize)
        {
            return StartNoGCRegionWorker(totalSize, false, 0, false);
        }

        public static bool TryStartNoGCRegion(long totalSize, long lohSize)
        {
            return StartNoGCRegionWorker(totalSize, true, lohSize, false);
        }

        public static bool TryStartNoGCRegion(long totalSize, bool disallowFullBlockingGC)
        {
            return StartNoGCRegionWorker(totalSize, false, 0, disallowFullBlockingGC);
        }

        public static bool TryStartNoGCRegion(long totalSize, long lohSize, bool disallowFullBlockingGC)
        {
            return StartNoGCRegionWorker(totalSize, true, lohSize, disallowFullBlockingGC);
        }

        public static void EndNoGCRegion()
        {
            EndNoGCRegionStatus status = (EndNoGCRegionStatus)_EndNoGCRegion();
            if (status == EndNoGCRegionStatus.NotInProgress)
                throw new InvalidOperationException(SR.InvalidOperationException_NoGCRegionNotInProgress);
            else if (status == EndNoGCRegionStatus.GCInduced)
                throw new InvalidOperationException(SR.InvalidOperationException_NoGCRegionInduced);
            else if (status == EndNoGCRegionStatus.AllocationExceeded)
                throw new InvalidOperationException(SR.InvalidOperationException_NoGCRegionAllocationExceeded);
        }

        private readonly struct MemoryLoadChangeNotification
        {
            public float LowMemoryPercent { get; }
            public float HighMemoryPercent { get; }
            public Action Notification { get; }

            public MemoryLoadChangeNotification(float lowMemoryPercent, float highMemoryPercent, Action notification)
            {
                LowMemoryPercent = lowMemoryPercent;
                HighMemoryPercent = highMemoryPercent;
                Notification = notification;
            }
        }

        private static readonly List<MemoryLoadChangeNotification> s_notifications = new List<MemoryLoadChangeNotification>();
        private static float s_previousMemoryLoad = float.MaxValue;

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern uint GetMemoryLoad();

        private static bool InvokeMemoryLoadChangeNotifications()
        {
            float currentMemoryLoad = (float)GetMemoryLoad();

            lock (s_notifications)
            {
                if (s_previousMemoryLoad == float.MaxValue)
                {
                    s_previousMemoryLoad = currentMemoryLoad;
                    return true;
                }

                // We need to take a snapshot of s_notifications.Count, so that in the case that s_notifications[i].Notification() registers new notifications,
                // we neither get rid of them nor iterate over them
                int count = s_notifications.Count;

                // If there is no existing notifications, we won't be iterating over any and we won't be adding any new one. Also, there wasn't any added since
                // we last invoked this method so it's safe to assume we can reset s_previousMemoryLoad.
                if (count == 0)
                {
                    s_previousMemoryLoad = float.MaxValue;
                    return false;
                }

                int last = 0;
                for (int i = 0; i < count; ++i)
                {
                    // If s_notifications[i] changes from within s_previousMemoryLoad bound to outside s_previousMemoryLoad, we trigger the notification
                    if (s_notifications[i].LowMemoryPercent <= s_previousMemoryLoad && s_previousMemoryLoad <= s_notifications[i].HighMemoryPercent
                         && !(s_notifications[i].LowMemoryPercent <= currentMemoryLoad && currentMemoryLoad <= s_notifications[i].HighMemoryPercent))
                    {
                        s_notifications[i].Notification();
                        // it will then be overwritten or removed
                    }
                    else
                    {
                        s_notifications[last++] = s_notifications[i];
                    }
                }

                if (last < count)
                {
                    s_notifications.RemoveRange(last, count - last);
                }

                return true;
            }
        }

        /// <summary>
        /// Register a notification to occur *AFTER* a GC occurs in which the memory load changes from within the bound specified
        /// to outside of the bound specified. This notification will occur once. If repeated notifications are required, the notification
        /// must be reregistered. The notification will occur on a thread which should not be blocked. Complex processing in the notification should defer work to the threadpool.
        /// </summary>
        /// <param name="lowMemoryPercent">percent of HighMemoryLoadThreshold to use as lower bound. Must be a number >= 0 or an ArgumentOutOfRangeException will be thrown.</param>
        /// <param name="highMemoryPercent">percent of HighMemoryLoadThreshold use to use as lower bound. Must be a number > lowMemory or an ArgumentOutOfRangeException will be thrown. </param>
        /// <param name="notification">delegate to invoke when operation occurs</param>s
        internal static void RegisterMemoryLoadChangeNotification(float lowMemoryPercent, float highMemoryPercent, Action notification)
        {
            if (highMemoryPercent < 0 || highMemoryPercent > 1.0 || highMemoryPercent <= lowMemoryPercent)
            {
                throw new ArgumentOutOfRangeException(nameof(highMemoryPercent));
            }
            if (lowMemoryPercent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lowMemoryPercent));
            }
            ArgumentNullException.ThrowIfNull(notification);

            lock (s_notifications)
            {
                s_notifications.Add(new MemoryLoadChangeNotification(lowMemoryPercent, highMemoryPercent, notification));

                if (s_notifications.Count == 1)
                {
                    Gen2GcCallback.Register(InvokeMemoryLoadChangeNotifications);
                }
            }
        }

        internal static void UnregisterMemoryLoadChangeNotification(Action notification!!)
        {
            lock (s_notifications)
            {
                for (int i = 0; i < s_notifications.Count; ++i)
                {
                    if (s_notifications[i].Notification == notification)
                    {
                        s_notifications.RemoveAt(i);
                        break;
                    }
                }

                // We only register the callback from the runtime in InvokeMemoryLoadChangeNotifications, so to avoid race conditions between
                // UnregisterMemoryLoadChangeNotification and InvokeMemoryLoadChangeNotifications in native.
            }
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
        public static T[] AllocateUninitializedArray<T>(int length, bool pinned = false) // T[] rather than T?[] to match `new T[length]` behavior
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
                if (length < 2048 / Unsafe.SizeOf<T>())
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

            // remove the local function when https://github.com/dotnet/runtime/issues/5973 is implemented
            static T[] AllocateNewUninitializedArray(int length, bool pinned)
            {
                GC_ALLOC_FLAGS flags = GC_ALLOC_FLAGS.GC_ALLOC_ZEROING_OPTIONAL;
                if (pinned)
                    flags |= GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP;

                return Unsafe.As<T[]>(AllocateNewArray(typeof(T[]).TypeHandle.Value, length, flags));
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
        public static T[] AllocateArray<T>(int length, bool pinned = false) // T[] rather than T?[] to match `new T[length]` behavior
        {
            GC_ALLOC_FLAGS flags = GC_ALLOC_FLAGS.GC_ALLOC_NO_FLAGS;

            if (pinned)
            {
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                    ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));

                flags = GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP;
            }

            return Unsafe.As<T[]>(AllocateNewArray(typeof(T[]).TypeHandle.Value, length, flags));
        }
    }
}
