// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading
{
    public partial interface IThreadPoolWorkItem
    {
        void Execute();
    }
#if !FEATURE_WASM_THREADS
    [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
    public sealed partial class RegisteredWaitHandle : System.MarshalByRefObject
    {
        internal RegisteredWaitHandle() { }
        public bool Unregister(System.Threading.WaitHandle? waitObject) { throw null; }
    }
    public static partial class ThreadPool
    {
        public static long CompletedWorkItemCount { get { throw null; } }
        public static long PendingWorkItemCount { get { throw null; } }
        public static int ThreadCount { get { throw null; } }
        [System.ObsoleteAttribute("ThreadPool.BindHandle(IntPtr) has been deprecated. Use ThreadPool.BindHandle(SafeHandle) instead.")]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool BindHandle(System.IntPtr osHandle) { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool BindHandle(System.Runtime.InteropServices.SafeHandle osHandle) { throw null; }
        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads) { throw null; }
        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads) { throw null; }
        public static void GetMinThreads(out int workerThreads, out int completionPortThreads) { throw null; }
        public static bool QueueUserWorkItem(System.Threading.WaitCallback callBack) { throw null; }
        public static bool QueueUserWorkItem(System.Threading.WaitCallback callBack, object? state) { throw null; }
        public static bool QueueUserWorkItem<TState>(System.Action<TState> callBack, TState state, bool preferLocal) { throw null; }
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object? state, int millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object? state, long millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object? state, System.TimeSpan timeout, bool executeOnlyOnce) { throw null; }
        [System.CLSCompliantAttribute(false)]
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object? state, uint millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
        public static bool SetMaxThreads(int workerThreads, int completionPortThreads) { throw null; }
        public static bool SetMinThreads(int workerThreads, int completionPortThreads) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public unsafe static bool UnsafeQueueNativeOverlapped(System.Threading.NativeOverlapped* overlapped) { throw null; }
        public static bool UnsafeQueueUserWorkItem(System.Threading.IThreadPoolWorkItem callBack, bool preferLocal) { throw null; }
        public static bool UnsafeQueueUserWorkItem(System.Threading.WaitCallback callBack, object? state) { throw null; }
        public static bool UnsafeQueueUserWorkItem<TState>(System.Action<TState> callBack, TState state, bool preferLocal) { throw null; }
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public static System.Threading.RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object? state, int millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public static System.Threading.RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object? state, long millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public static System.Threading.RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object? state, System.TimeSpan timeout, bool executeOnlyOnce) { throw null; }
        [System.CLSCompliantAttribute(false)]
#if !FEATURE_WASM_THREADS
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
#endif
        public static System.Threading.RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object? state, uint millisecondsTimeOutInterval, bool executeOnlyOnce) { throw null; }
    }
    public delegate void WaitCallback(object? state);
    public delegate void WaitOrTimerCallback(object? state, bool timedOut);
}
