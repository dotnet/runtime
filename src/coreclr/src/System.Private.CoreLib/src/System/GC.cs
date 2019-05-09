// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using Internal.Runtime.CompilerServices;

namespace System
{
    public enum GCCollectionMode
    {
        Default = 0,
        Forced = 1,
        Optimized = 2
    }

    // !!!!!!!!!!!!!!!!!!!!!!!
    // make sure you change the def in vm\gc.h 
    // if you change this!
    internal enum InternalGCCollectionMode
    {
        NonBlocking = 0x00000001,
        Blocking = 0x00000002,
        Optimized = 0x00000004,
        Compacting = 0x00000008,
    }

    // !!!!!!!!!!!!!!!!!!!!!!!
    // make sure you change the def in vm\gc.h 
    // if you change this!
    public enum GCNotificationStatus
    {
        Succeeded = 0,
        Failed = 1,
        Canceled = 2,
        Timeout = 3,
        NotApplicable = 4
    }

    public static class GC
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void GetMemoryInfo(out uint highMemLoadThreshold,
                                                  out ulong totalPhysicalMem,
                                                  out uint lastRecordedMemLoad,
                                                  // The next two are size_t
                                                  out UIntPtr lastRecordedHeapSize,
                                                  out UIntPtr lastRecordedFragmentation);

        public static GCMemoryInfo GetGCMemoryInfo()
        {
            GetMemoryInfo(out uint highMemLoadThreshold,
                          out ulong totalPhysicalMem,
                          out uint lastRecordedMemLoad,
                          out UIntPtr lastRecordedHeapSize,
                          out UIntPtr lastRecordedFragmentation);

            return new GCMemoryInfo((long)((double)highMemLoadThreshold / 100 * totalPhysicalMem),
                                    (long)((double)lastRecordedMemLoad / 100 * totalPhysicalMem),
                                    (long)totalPhysicalMem,
                                    (long)(ulong)lastRecordedHeapSize,
                                    (long)(ulong)lastRecordedFragmentation);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int _StartNoGCRegion(long totalSize, bool lohSizeKnown, long lohSize, bool disallowFullBlockingGC);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int _EndNoGCRegion();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Array AllocateNewArray(IntPtr typeHandle, int length, bool zeroingOptional);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetGenerationWR(IntPtr handle);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern long GetTotalMemory();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void _Collect(int generation, int mode);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetMaxGeneration();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int _CollectionCount(int generation, int getSpecialGCCount);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern ulong GetSegmentSize();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void _AddMemoryPressure(ulong bytesAllocated);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void _RemoveMemoryPressure(ulong bytesAllocated);

        public static void AddMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesAllocated),
                        SR.ArgumentOutOfRange_NeedPosNum);
            }

            if ((4 == IntPtr.Size) && (bytesAllocated > int.MaxValue))
            {
                throw new ArgumentOutOfRangeException("pressure",
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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
            //-1 says to GC all generations.
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
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // disable optimizations
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
        public static int MaxGeneration
        {
            get { return GetMaxGeneration(); }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void _WaitForPendingFinalizers();

        public static void WaitForPendingFinalizers()
        {
            // QCalls can not be exposed from mscorlib directly, need to wrap it.
            _WaitForPendingFinalizers();
        }

        // Indicates that the system should not call the Finalize() method on
        // an object that would normally require this call.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _SuppressFinalize(object o);

        public static void SuppressFinalize(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            _SuppressFinalize(obj);
        }

        // Indicates that the system should call the Finalize() method on an object
        // for which SuppressFinalize has already been called. The other situation 
        // where calling ReRegisterForFinalize is useful is inside a finalizer that 
        // needs to resurrect itself or an object that it references.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void _ReRegisterForFinalize(object o);

        public static void ReRegisterForFinalize(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
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

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr _RegisterFrozenSegment(IntPtr sectionAddress, int sectionSize);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr _UnregisterFrozenSegment(IntPtr segmentHandle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern long _GetAllocatedBytesForCurrentThread();

        public static long GetAllocatedBytesForCurrentThread()
        {
            return _GetAllocatedBytesForCurrentThread();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool _RegisterForFullGCNotification(int maxGenerationPercentage, int largeObjectHeapPercentage);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool _CancelFullGCNotification();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int _WaitForFullGCApproach(int millisecondsTimeout);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
                throw new ArgumentOutOfRangeException(nameof(totalSize), "totalSize can't be zero or negative");
            }

            if (hasLohSize)
            {
                if (lohSize <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(lohSize), "lohSize can't be zero or negative");
                }

                if (lohSize > totalSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(lohSize), "lohSize can't be greater than totalSize");
                }
            }

            StartNoGCRegionStatus status = (StartNoGCRegionStatus)_StartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC);
            switch (status)
            {
                case StartNoGCRegionStatus.NotEnoughMemory:
                    return false;
                case StartNoGCRegionStatus.AlreadyInProgress:
                    throw new InvalidOperationException("The NoGCRegion mode was already in progress");
                case StartNoGCRegionStatus.AmountTooLarge:
                    throw new ArgumentOutOfRangeException(nameof(totalSize),
                        "totalSize is too large. For more information about setting the maximum size, see \"Latency Modes\" in http://go.microsoft.com/fwlink/?LinkId=522706");
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

        private static EndNoGCRegionStatus EndNoGCRegionWorker()
        {
            EndNoGCRegionStatus status = (EndNoGCRegionStatus)_EndNoGCRegion();
            if (status == EndNoGCRegionStatus.NotInProgress)
                throw new InvalidOperationException("NoGCRegion mode must be set");
            else if (status == EndNoGCRegionStatus.GCInduced)
                throw new InvalidOperationException("Garbage collection was induced in NoGCRegion mode");
            else if (status == EndNoGCRegionStatus.AllocationExceeded)
                throw new InvalidOperationException("Allocated memory exceeds specified memory for NoGCRegion mode");

            return EndNoGCRegionStatus.Succeeded;
        }

        public static void EndNoGCRegion()
        {
            EndNoGCRegionWorker();
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

        private static float GetMemoryLoad()
        {
            GetMemoryInfo(out uint _,
                          out ulong _,
                          out uint lastRecordedMemLoad,
                          out UIntPtr _,
                          out UIntPtr _);

            return (float)lastRecordedMemLoad / 100;
        }

        private static bool InvokeMemoryLoadChangeNotifications()
        {
            float currentMemoryLoad = GetMemoryLoad();

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
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            lock (s_notifications)
            {
                s_notifications.Add (new MemoryLoadChangeNotification(lowMemoryPercent, highMemoryPercent, notification));

                if (s_notifications.Count == 1)
                {
                    Gen2GcCallback.Register(InvokeMemoryLoadChangeNotifications);
                }
            }
        }

        internal static void UnregisterMemoryLoadChangeNotification(Action notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

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

        // Skips zero-initialization of the array if possible. If T contains object references, 
        // the array is always zero-initialized.
        internal static T[] AllocateUninitializedArray<T>(int length)
        {
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, 0, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
#if DEBUG
            // in DEBUG arrays of any length can be created uninitialized
#else
            // otherwise small arrays are allocated using `new[]` as that is generally faster.
            //
            // The threshold was derived from various simulations. 
            // As it turned out the threshold depends on overal pattern of all allocations and is typically in 200-300 byte range.
            // The gradient around the number is shallow (there is no perf cliff) and the exact value of the threshold does not matter a lot.
            // So it is 256 bytes including array header.
            if (Unsafe.SizeOf<T>() * length < 256 - 3 * IntPtr.Size)
            {
                return new T[length];
            }
#endif
            return (T[])AllocateNewArray(typeof(T[]).TypeHandle.Value, length, zeroingOptional: true);
        }
    }
}
