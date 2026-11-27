// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct Win32ThreadPoolNativeOverlapped
    {
        // Per-thread cache of the args object, so we don't have to allocate a new one each time.
        [ThreadStatic]
        private static ExecutionContextCallbackArgs? t_executionContextCallbackArgs;

        private static ContextCallback? s_executionContextCallback;

        private NativeOverlapped _overlapped; // must be first, so we can cast to and from NativeOverlapped.
        private GCHandle _dataHandle;

        internal OverlappedData Data
        {
            get { return (OverlappedData)_dataHandle.Target!; }
        }

        internal static unsafe Win32ThreadPoolNativeOverlapped* Allocate(IOCompletionCallback callback, object? state, object? pinData, PreAllocatedOverlapped? preAllocated, bool flowExecutionControl)
        {
            Win32ThreadPoolNativeOverlapped* overlapped =
                (Win32ThreadPoolNativeOverlapped*)NativeMemory.AllocZeroed((nuint)sizeof(Win32ThreadPoolNativeOverlapped));
            try
            {
                overlapped->_dataHandle = GCHandle.Alloc(new OverlappedData());
                overlapped->SetData(callback, state, pinData, preAllocated, flowExecutionControl);
            }
            catch
            {
                Free(overlapped);
                throw;
            }
            return overlapped;
        }

        private void SetData(IOCompletionCallback callback, object? state, object? pinData, PreAllocatedOverlapped? preAllocated, bool flowExecutionContext)
        {
            Debug.Assert(callback != null);

            OverlappedData data = Data;

            data._callback = callback;
            data._state = state;
            data._executionContext = flowExecutionContext ? ExecutionContext.Capture() : null;
            data._preAllocated = preAllocated;

            //
            // pinData can be any blittable type to be pinned, *or* an instance of object[] each element of which refers to
            // an instance of a blittable type to be pinned.
            //
            if (pinData != null)
            {
                if (pinData is object[] objArray && objArray.GetType() == typeof(object[]))
                {
                    data._pinnedData = new GCHandle[objArray.Length];
                    for (int i = 0; i < objArray.Length; i++)
                        data._pinnedData[i] = GCHandle.Alloc(objArray[i], GCHandleType.Pinned);
                }
                else
                {
                    data._pinnedData = new GCHandle[1];
                    data._pinnedData[0] = GCHandle.Alloc(pinData, GCHandleType.Pinned);
                }
            }
        }

        internal static unsafe void Free(Win32ThreadPoolNativeOverlapped* overlapped)
        {
            if (overlapped->_dataHandle.IsAllocated)
            {
                OverlappedData data = overlapped->Data;
                Debug.Assert(data._boundHandle is null);

                if (data._pinnedData is GCHandle[] pinnedData)
                {
                    for (int i = 0; i < pinnedData.Length; i++)
                    {
                        if (pinnedData[i].IsAllocated)
                            pinnedData[i].Free();
                    }
                }

                overlapped->_dataHandle.Free();
            }

            NativeMemory.Free(overlapped);
        }

        internal static unsafe NativeOverlapped* ToNativeOverlapped(Win32ThreadPoolNativeOverlapped* overlapped)
        {
            return (NativeOverlapped*)overlapped;
        }

        internal static unsafe Win32ThreadPoolNativeOverlapped* FromNativeOverlapped(NativeOverlapped* overlapped)
        {
            return (Win32ThreadPoolNativeOverlapped*)overlapped;
        }

        internal static unsafe void CompleteWithCallback(uint errorCode, uint bytesWritten, Win32ThreadPoolNativeOverlapped* overlapped)
        {
            OverlappedData data = overlapped->Data;

            Debug.Assert(!data._completed);
            data._completed = true;

            if (data._executionContext == null)
            {
                Debug.Assert(data._callback != null, "Does CompleteWithCallback called after Reset?");
                data._callback(errorCode, bytesWritten, ToNativeOverlapped(overlapped));
                return;
            }

            ContextCallback? callback = s_executionContextCallback;
            if (callback == null)
                s_executionContextCallback = callback = OnExecutionContextCallback;

            // Get an args object from the per-thread cache.
            ExecutionContextCallbackArgs? args = t_executionContextCallbackArgs;
            args ??= new ExecutionContextCallbackArgs();

            t_executionContextCallbackArgs = null;

            args._errorCode = errorCode;
            args._bytesWritten = bytesWritten;
            args._overlapped = overlapped;
            args._data = data;

            ExecutionContext.Run(data._executionContext, callback, args);
        }

        private static unsafe void OnExecutionContextCallback(object? state)
        {
            Debug.Assert(state != null);
            ExecutionContextCallbackArgs args = (ExecutionContextCallbackArgs)state;

            uint errorCode = args._errorCode;
            uint bytesWritten = args._bytesWritten;
            Win32ThreadPoolNativeOverlapped* overlapped = args._overlapped;
            OverlappedData data = args._data!;

            // Put the args object back in the per-thread cache, now that we're done with it.
            args._data = null;
            t_executionContextCallbackArgs = args;

            Debug.Assert(data._callback != null, "Does OnExecutionContextCallback called after Reset?");
            data._callback(errorCode, bytesWritten, ToNativeOverlapped(overlapped));
        }

        internal bool IsUserObject(byte[]? buffer) => ReferenceEquals(Data._pinnedData, buffer);
    }
}
