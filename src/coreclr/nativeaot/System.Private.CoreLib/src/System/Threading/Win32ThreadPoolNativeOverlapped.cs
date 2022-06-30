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

        internal static unsafe Win32ThreadPoolNativeOverlapped* Allocate(IOCompletionCallback callback, object state, object pinData, PreAllocatedOverlapped preAllocated, bool flowExecutionControl)
        {
            Win32ThreadPoolNativeOverlapped* overlapped = AllocateNew();
            try
            {
                overlapped->SetData(callback, state, pinData, preAllocated, flowExecutionControl);
            }
            catch
            {
                Free(overlapped);
                throw;
            }
            return overlapped;
        }

        private static unsafe Win32ThreadPoolNativeOverlapped* AllocateNew()
        {
            IntPtr freePtr;
            Win32ThreadPoolNativeOverlapped* overlapped;
            OverlappedData data;

            // Find a free Overlapped
            while ((freePtr = Volatile.Read(ref s_freeList)) != IntPtr.Zero)
            {
                overlapped = (Win32ThreadPoolNativeOverlapped*)freePtr;

                if (Interlocked.CompareExchange(ref s_freeList, overlapped->_nextFree, freePtr) != freePtr)
                    continue;

                overlapped->_nextFree = IntPtr.Zero;
                return overlapped;
            }

            // None are free; allocate a new one.
            overlapped = (Win32ThreadPoolNativeOverlapped*)NativeMemory.Alloc((nuint)sizeof(Win32ThreadPoolNativeOverlapped));
            *overlapped = default(Win32ThreadPoolNativeOverlapped);

            // Allocate a OverlappedData object, and an index at which to store it in _dataArray.
            data = new OverlappedData();
            int dataIndex = Interlocked.Increment(ref s_dataCount) - 1;

            // Make sure we didn't wrap around.
            if (dataIndex < 0)
                Environment.FailFast("Too many outstanding Win32ThreadPoolNativeOverlapped instances");

            while (true)
            {
                OverlappedData[]? dataArray = Volatile.Read(ref s_dataArray);
                int currentLength = dataArray == null ? 0 : dataArray.Length;

                // If the current array is too small, create a new, larger one.
                if (currentLength <= dataIndex)
                {
                    int newLength = currentLength;
                    if (newLength == 0)
                        newLength = 128;
                    while (newLength <= dataIndex)
                        newLength = (newLength * 3) / 2;

                    OverlappedData[]? newDataArray = dataArray;
                    Array.Resize(ref newDataArray, newLength);

                    if (Interlocked.CompareExchange(ref s_dataArray, newDataArray, dataArray) != dataArray)
                        continue; // Someone else got the free one, try again

                    dataArray = newDataArray;
                }

                // If we haven't stored this object in the array yet, do so now.  Then we need to make another pass through
                // the loop, in case another thread resized the array before we made this update.
                if (s_dataArray[dataIndex] == null)
                {
                    // Full fence so this write can't move past subsequent reads.
                    Interlocked.Exchange(ref dataArray![dataIndex], data);
                    continue;
                }

                // We're already in the array, so we're done.
                Debug.Assert(dataArray![dataIndex] == data);
                overlapped->_dataIndex = dataIndex;
                return overlapped;
            }
        }

        private void SetData(IOCompletionCallback callback, object state, object pinData, PreAllocatedOverlapped preAllocated, bool flowExecutionContext)
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
                object[]? objArray = pinData as object[];
                if (objArray != null && objArray.GetType() == typeof(object[]))
                {
                    if (data._pinnedData == null || data._pinnedData.Length < objArray.Length)
                        Array.Resize(ref data._pinnedData, objArray.Length);

                    for (int i = 0; i < objArray.Length; i++)
                    {
                        if (!data._pinnedData[i].IsAllocated)
                            data._pinnedData[i] = GCHandle.Alloc(objArray[i], GCHandleType.Pinned);
                        else
                            data._pinnedData[i].Target = objArray[i];
                    }
                }
                else
                {
                    if (data._pinnedData == null)
                        data._pinnedData = new GCHandle[1];

                    if (!data._pinnedData[0].IsAllocated)
                        data._pinnedData[0] = GCHandle.Alloc(pinData, GCHandleType.Pinned);
                    else
                        data._pinnedData[0].Target = pinData;
                }
            }
        }

        internal static unsafe void Free(Win32ThreadPoolNativeOverlapped* overlapped)
        {
            // Reset all data.
            overlapped->Data.Reset();
            overlapped->_overlapped = default(NativeOverlapped);

            // Add to the free list.
            while (true)
            {
                IntPtr freePtr = Volatile.Read(ref s_freeList);
                overlapped->_nextFree = freePtr;

                if (Interlocked.CompareExchange(ref s_freeList, (IntPtr)overlapped, freePtr) == freePtr)
                    break;
            }
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

            ContextCallback callback = s_executionContextCallback;
            if (callback == null)
                s_executionContextCallback = callback = OnExecutionContextCallback;

            // Get an args object from the per-thread cache.
            ExecutionContextCallbackArgs args = t_executionContextCallbackArgs;
            if (args == null)
                args = new ExecutionContextCallbackArgs();

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
            OverlappedData data = args._data;

            // Put the args object back in the per-thread cache, now that we're done with it.
            args._data = null;
            t_executionContextCallbackArgs = args;

            Debug.Assert(data._callback != null, "Does OnExecutionContextCallback called after Reset?");
            data._callback(errorCode, bytesWritten, ToNativeOverlapped(overlapped));
        }

        internal bool IsUserObject(byte[]? buffer) => ReferenceEquals(Data._pinnedData, buffer);
    }
}
