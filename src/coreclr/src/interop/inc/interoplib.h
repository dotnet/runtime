// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _INTEROP_INC_INTEROPLIB_H_
#define _INTEROP_INC_INTEROPLIB_H_

namespace InteropLib
{
    using OBJECTHANDLE = void*;

#ifdef _WIN32

    namespace Com
    {
        using NativeObjectWrapperHandle = void*;

        // Create an IUnknown instance that represents the supplied managed object instance.
        HRESULT CreateWrapperForObject(
            _In_ OBJECTHANDLE instance,
            _In_ INT32 vtableCount,
            _In_ void* vtables,
            _In_ INT32 flags,
            _Outptr_ IUnknown** comObject) noexcept;

        // Destroy the supplied COM wrapper
        void DestroyWrapperForObject(_In_ void* wrapper) noexcept;

        // Register the default callback in the Reference Tracker Host scenario.
        // Returns true if registration succeeded, otherwise false.
        bool RegisterReferenceTrackerHostCallback(_In_ OBJECTHANDLE objectHandle) noexcept;

        // Get internal interop IUnknown dispatch pointers.
        void GetIUnknownImpl(
            _Out_ void** fpQueryInterface,
            _Out_ void** fpAddRef,
            _Out_ void** fpRelease) noexcept;

        // Ensure the wrapper is active and take an AddRef.
        // S_OK    - the wrapper is active and the OBJECTHANDLE wasn't needed.
        // S_FALSE - the wrapper was inactive and the OBJECTHANDLE argument was used.
        HRESULT EnsureActiveWrapperAndAddRef(_In_ IUnknown* wrapper, _In_ OBJECTHANDLE handle) noexcept;
    }

#endif // _WIN32

}

#endif // _INTEROP_INC_INTEROPLIB_H_

