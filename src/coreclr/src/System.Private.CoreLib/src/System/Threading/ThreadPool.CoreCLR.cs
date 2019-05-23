// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Class for creating and managing a threadpool
**
**
=============================================================================*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System.Threading
{
    //
    // This type is necessary because VS 2010's debugger looks for a method named _ThreadPoolWaitCallbacck.PerformWaitCallback
    // on the stack to determine if a thread is a ThreadPool thread or not.  We have a better way to do this for .NET 4.5, but
    // still need to maintain compatibility with VS 2010.  When compat with VS 2010 is no longer an issue, this type may be
    // removed.
    //
    internal static class _ThreadPoolWaitCallback
    {
        internal static bool PerformWaitCallback() => ThreadPoolWorkQueue.Dispatch();
    }

    internal sealed class RegisteredWaitHandleSafe : CriticalFinalizerObject
    {
        private static IntPtr InvalidHandle => new IntPtr(-1);
        private IntPtr registeredWaitHandle = InvalidHandle;
        private WaitHandle? m_internalWaitObject;
        private bool bReleaseNeeded = false;
        private volatile int m_lock = 0;

        internal IntPtr GetHandle() => registeredWaitHandle;

        internal void SetHandle(IntPtr handle)
        {
            registeredWaitHandle = handle;
        }

        internal void SetWaitObject(WaitHandle waitObject)
        {
            // needed for DangerousAddRef
            RuntimeHelpers.PrepareConstrainedRegions();

            m_internalWaitObject = waitObject;
            if (waitObject != null)
            {
                m_internalWaitObject.SafeWaitHandle!.DangerousAddRef(ref bReleaseNeeded); // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/2384
            }
        }

        internal bool Unregister(
             WaitHandle? waitObject          // object to be notified when all callbacks to delegates have completed
             )
        {
            bool result = false;
            // needed for DangerousRelease
            RuntimeHelpers.PrepareConstrainedRegions();

            // lock(this) cannot be used reliably in Cer since thin lock could be
            // promoted to syncblock and that is not a guaranteed operation
            bool bLockTaken = false;
            do
            {
                if (Interlocked.CompareExchange(ref m_lock, 1, 0) == 0)
                {
                    bLockTaken = true;
                    try
                    {
                        if (ValidHandle())
                        {
                            result = UnregisterWaitNative(GetHandle(), waitObject == null ? null : waitObject.SafeWaitHandle);
                            if (result == true)
                            {
                                if (bReleaseNeeded)
                                {
                                    Debug.Assert(m_internalWaitObject != null, "Must be non-null for bReleaseNeeded to be true");
                                    m_internalWaitObject.SafeWaitHandle!.DangerousRelease(); // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/2384
                                    bReleaseNeeded = false;
                                }
                                // if result not true don't release/suppress here so finalizer can make another attempt
                                SetHandle(InvalidHandle);
                                m_internalWaitObject = null;
                                GC.SuppressFinalize(this);
                            }
                        }
                    }
                    finally
                    {
                        m_lock = 0;
                    }
                }
                Thread.SpinWait(1);     // yield to processor
            }
            while (!bLockTaken);

            return result;
        }

        private bool ValidHandle() =>
            registeredWaitHandle != InvalidHandle && registeredWaitHandle != IntPtr.Zero;

        ~RegisteredWaitHandleSafe()
        {
            // if the app has already unregistered the wait, there is nothing to cleanup
            // we can detect this by checking the handle. Normally, there is no race condition here
            // so no need to protect reading of handle. However, if this object gets 
            // resurrected and then someone does an unregister, it would introduce a race condition
            //
            // PrepareConstrainedRegions call not needed since finalizer already in Cer
            //
            // lock(this) cannot be used reliably even in Cer since thin lock could be
            // promoted to syncblock and that is not a guaranteed operation
            //
            // Note that we will not "spin" to get this lock.  We make only a single attempt;
            // if we can't get the lock, it means some other thread is in the middle of a call
            // to Unregister, which will do the work of the finalizer anyway.
            //
            // Further, it's actually critical that we *not* wait for the lock here, because
            // the other thread that's in the middle of Unregister may be suspended for shutdown.
            // Then, during the live-object finalization phase of shutdown, this thread would
            // end up spinning forever, as the other thread would never release the lock.
            // This will result in a "leak" of sorts (since the handle will not be cleaned up)
            // but the process is exiting anyway.
            //
            // During AD-unload, we don't finalize live objects until all threads have been 
            // aborted out of the AD.  Since these locked regions are CERs, we won't abort them 
            // while the lock is held.  So there should be no leak on AD-unload.
            //
            if (Interlocked.CompareExchange(ref m_lock, 1, 0) == 0)
            {
                try
                {
                    if (ValidHandle())
                    {
                        WaitHandleCleanupNative(registeredWaitHandle);
                        if (bReleaseNeeded)
                        {
                            Debug.Assert(m_internalWaitObject != null, "Must be non-null for bReleaseNeeded to be true");
                            m_internalWaitObject.SafeWaitHandle!.DangerousRelease(); // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/2384
                            bReleaseNeeded = false;
                        }
                        SetHandle(InvalidHandle);
                        m_internalWaitObject = null;
                    }
                }
                finally
                {
                    m_lock = 0;
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void WaitHandleCleanupNative(IntPtr handle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool UnregisterWaitNative(IntPtr handle, SafeHandle? waitObject);
    }

    public sealed class RegisteredWaitHandle : MarshalByRefObject
    {
        private readonly RegisteredWaitHandleSafe internalRegisteredWait;

        internal RegisteredWaitHandle()
        {
            internalRegisteredWait = new RegisteredWaitHandleSafe();
        }

        internal void SetHandle(IntPtr handle)
        {
            internalRegisteredWait.SetHandle(handle);
        }

        internal void SetWaitObject(WaitHandle waitObject)
        {
            internalRegisteredWait.SetWaitObject(waitObject);
        }

        public bool Unregister(
             WaitHandle? waitObject          // object to be notified when all callbacks to delegates have completed
             )
        {
            return internalRegisteredWait.Unregister(waitObject);
        }
    }

    public static partial class ThreadPool
    {
        // Time in ms for which ThreadPoolWorkQueue.Dispatch keeps executing work items before returning to the OS
        private const uint DispatchQuantum = 30;

        internal static bool KeepDispatching(int startTickCount)
        {
            // Note: this function may incorrectly return false due to TickCount overflow
            // if work item execution took around a multiple of 2^32 milliseconds (~49.7 days),
            // which is improbable.
            return ((uint)(Environment.TickCount - startTickCount) < DispatchQuantum);
        }

        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            return
                workerThreads >= 0 &&
                completionPortThreads >= 0 &&
                SetMaxThreadsNative(workerThreads, completionPortThreads);
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            GetMaxThreadsNative(out workerThreads, out completionPortThreads);
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            return
                workerThreads >= 0 &&
                completionPortThreads >= 0 &&
                SetMinThreadsNative(workerThreads, completionPortThreads);
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            GetMinThreadsNative(out workerThreads, out completionPortThreads);
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            GetAvailableThreadsNative(out workerThreads, out completionPortThreads);
        }

        /// <summary>
        /// Gets the number of thread pool threads that currently exist.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of threads, the count includes all types.
        /// </remarks>
        public static extern int ThreadCount
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Gets the number of work items that have been processed so far.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types.
        /// </remarks>
        public static long CompletedWorkItemCount => GetCompletedWorkItemCount();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern long GetCompletedWorkItemCount();

        private static extern long PendingUnmanagedWorkItemCount
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        private static RegisteredWaitHandle RegisterWaitForSingleObject(  // throws RegisterWaitException
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,   // NOTE: we do not allow other options that allow the callback to be queued as an APC
             bool compressStack
             )
        {
            RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle();

            if (callBack != null)
            {
                _ThreadPoolWaitOrTimerCallback callBackHelper = new _ThreadPoolWaitOrTimerCallback(callBack, state, compressStack);
                state = (object)callBackHelper;
                // call SetWaitObject before native call so that waitObject won't be closed before threadpoolmgr registration
                // this could occur if callback were to fire before SetWaitObject does its addref
                registeredWaitHandle.SetWaitObject(waitObject);
                IntPtr nativeRegisteredWaitHandle = RegisterWaitForSingleObjectNative(waitObject,
                                                                               state,
                                                                               millisecondsTimeOutInterval,
                                                                               executeOnlyOnce,
                                                                               registeredWaitHandle);
                registeredWaitHandle.SetHandle(nativeRegisteredWaitHandle);
            }
            else
            {
                throw new ArgumentNullException(nameof(WaitOrTimerCallback));
            }
            return registeredWaitHandle;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern Interop.BOOL RequestWorkerThread();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe bool PostQueuedCompletionStatus(NativeOverlapped* overlapped);

        [CLSCompliant(false)]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped) =>
            PostQueuedCompletionStatus(overlapped);

        // The thread pool maintains a per-appdomain managed work queue.
        // New thread pool entries are added in the managed queue.
        // The VM is responsible for the actual growing/shrinking of 
        // threads. 
        private static void EnsureInitialized()
        {
            if (!ThreadPoolGlobals.threadPoolInitialized)
            {
                EnsureVMInitializedCore(); // separate out to help with inlining
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnsureVMInitializedCore()
        {
            InitializeVMTp(ref ThreadPoolGlobals.enableWorkerTracking);
            ThreadPoolGlobals.threadPoolInitialized = true;
        }

        // Native methods: 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool SetMinThreadsNative(int workerThreads, int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool SetMaxThreadsNative(int workerThreads, int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetMinThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetMaxThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetAvailableThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool NotifyWorkItemComplete();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ReportThreadStatus(bool isWorking);

        internal static void NotifyWorkItemProgress()
        {
            EnsureInitialized();
            NotifyWorkItemProgressNative();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void NotifyWorkItemProgressNative();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void InitializeVMTp(ref bool enableWorkerTracking);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr RegisterWaitForSingleObjectNative(
             WaitHandle waitHandle,
             object state,
             uint timeOutInterval,
             bool executeOnlyOnce,
             RegisteredWaitHandle registeredWaitHandle
             );


        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated.  Please use ThreadPool.BindHandle(SafeHandle) instead.", false)]
        public static bool BindHandle(IntPtr osHandle)
        {
            return BindIOCompletionCallbackNative(osHandle);
        }

        public static bool BindHandle(SafeHandle osHandle)
        {
            if (osHandle == null)
                throw new ArgumentNullException(nameof(osHandle));

            bool ret = false;
            bool mustReleaseSafeHandle = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                osHandle.DangerousAddRef(ref mustReleaseSafeHandle);
                ret = BindIOCompletionCallbackNative(osHandle.DangerousGetHandle());
            }
            finally
            {
                if (mustReleaseSafeHandle)
                    osHandle.DangerousRelease();
            }
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool BindIOCompletionCallbackNative(IntPtr fileHandle);
    }
}
