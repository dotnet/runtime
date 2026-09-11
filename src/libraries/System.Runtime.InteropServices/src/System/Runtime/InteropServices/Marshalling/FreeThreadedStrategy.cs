// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementations of the COM strategy interfaces defined in Com.cs that we would want to ship (can be internal only if we don't want to allow users to provide their own implementations in v1).
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    // This class is called "FreeThreaded" for free threaded COM objects that are not apartment threaded.
    // However, it is also valid for COM objects that are affinitized to an apartment but are currently on
    // a thread with the correct apartment type. In that case, the COM object is not actually free threaded,
    // but it is safe to call AddRef/Release/QueryInterface on it from the current thread.
    internal sealed unsafe class FreeThreadedStrategy : IIUnknownStrategy
    {
        public static readonly IIUnknownStrategy Instance = new FreeThreadedStrategy();

        void* IIUnknownStrategy.CreateInstancePointer(void* unknown)
        {
            Marshal.AddRef((nint)unknown);
            return unknown;
        }

        unsafe int IIUnknownStrategy.QueryInterface(void* thisPtr, in Guid handle, out void* ppObj)
        {
            int hr = Marshal.QueryInterface((nint)thisPtr, handle, out nint ppv);
            if (hr < 0)
            {
                ppObj = null;
            }
            else
            {
                ppObj = (void*)ppv;
            }
            return hr;
        }

        unsafe int IIUnknownStrategy.Release(void* thisPtr)
            => Marshal.Release((nint)thisPtr);
    }
}
