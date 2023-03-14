// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct Win32ThreadPoolNativeOverlapped
    {
        // Per-thread cache of the args object, so we don't have to allocate a new one each time.
        [ThreadStatic]
        private static ExecutionContextCallbackArgs t_executionContextCallbackArgs;

        private static ContextCallback s_executionContextCallback;
        private static OverlappedData[] s_dataArray;
        private static int s_dataCount;   // Current number of valid entries in _dataArray
        private static IntPtr s_freeList; // Lock-free linked stack of free ThreadPoolNativeOverlapped instances.

        private NativeOverlapped _overlapped; // must be first, so we can cast to and from NativeOverlapped.
        private IntPtr _nextFree; // if this instance if free, points to the next free instance.
        private int _dataIndex; // Index in _dataArray of this instance's OverlappedData.

        internal OverlappedData Data
        {
            get { return s_dataArray[_dataIndex]; }
        }

        internal static unsafe Win32ThreadPoolNativeOverlapped* Allocate(IOCompletionCallback callback, object state, object pinData, PreAllocatedOverlapped preAllocated, bool flowExecutionControl) =>
            AllocateCore(callback, state, pinData, preAllocated, flowExecutionControl);

        internal static unsafe void Free(Win32ThreadPoolNativeOverlapped* overlapped) => FreeCore(overlapped);

        internal static unsafe NativeOverlapped* ToNativeOverlapped(Win32ThreadPoolNativeOverlapped* overlapped) => ToNativeOverlappedCore(overlapped);

        internal static unsafe Win32ThreadPoolNativeOverlapped* FromNativeOverlapped(NativeOverlapped* overlapped) => FromNativeOverlappedCore(overlapped);

        internal static unsafe void CompleteWithCallback(uint errorCode, uint bytesWritten, Win32ThreadPoolNativeOverlapped* overlapped) => CompleteWithCallbackCore(errorCode, bytesWritten, overlapped);

        internal bool IsUserObject(byte[]? buffer) => IsUserObjectCore(buffer);
    }
}
