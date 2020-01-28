// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_INC_INTEROPLIB_H_
#define _INTEROP_INC_INTEROPLIB_H_

namespace InteropLib
{
    using Ptr_OBJECTREF = void*;
    using OBJECTHANDLE = void*;

#ifdef _WIN32

    // Create an IUnknown instance that represents the supplied managed object instance.
    HRESULT CreateComInterfaceForObject(
        _In_ OBJECTHANDLE instance,
        _In_ INT32 vtableCount,
        _In_ void* vtables,
        _In_ INT32 flags,
        _Outptr_ IUnknown** comObject);

    // Register the default callback in the Reference Tracker Host scenario.
    // Returns true if registration succeeded, otherwise false.
    bool RegisterReferenceTrackerHostCallback(_In_ OBJECTHANDLE objectHandle);

    // Get internal interop IUnknown dispatch pointers.
    void GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease);

#endif // _WIN32

}

#endif // _INTEROP_INC_INTEROPLIB_H_

