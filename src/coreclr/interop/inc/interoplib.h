// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_INC_INTEROPLIB_H_
#define _INTEROP_INC_INTEROPLIB_H_

namespace InteropLibImports
{
    // Forward declaration of Runtime calling context.
    // This class is used by the consuming runtime to pass through details
    // that may be required during a subsequent callback from the InteropLib.
    // InteropLib never directly modifies or inspects supplied instances.
    struct RuntimeCallContext;
}

namespace InteropLib
{
    using OBJECTHANDLE = void*;

    namespace Com
    {
        // See CreateComInterfaceFlags in ComWrappers.cs
        enum CreateComInterfaceFlags
        {
            CreateComInterfaceFlags_None = 0,
            CreateComInterfaceFlags_CallerDefinedIUnknown = 1,
            CreateComInterfaceFlags_TrackerSupport = 2,
        };

        // Create an IUnknown instance that represents the supplied managed object instance.
        HRESULT CreateWrapperForObject(
            _In_ OBJECTHANDLE instance,
            _In_ INT32 vtableCount,
            _In_ void* vtables,
            _In_ enum CreateComInterfaceFlags flags,
            _Outptr_ IUnknown** wrapper) noexcept;

        // Destroy the supplied wrapper
        void DestroyWrapperForObject(_In_ void* wrapper) noexcept;

        // Check if a wrapper is considered a GC root.
        HRESULT IsWrapperRooted(_In_ IUnknown* wrapper) noexcept;

        // Get the object for the supplied wrapper
        HRESULT GetObjectForWrapper(_In_ IUnknown* wrapper, _Outptr_result_maybenull_ OBJECTHANDLE* object) noexcept;

        HRESULT MarkComActivated(_In_ IUnknown* wrapper) noexcept;
        HRESULT IsComActivated(_In_ IUnknown* wrapper) noexcept;

        struct ExternalWrapperResult
        {
            // The returned context memory is guaranteed to be initialized to zero.
            void* Context;

            // See https://docs.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/
            // for details.
            bool FromTrackerRuntime;

            // The supplied external object is wrapping a managed object.
            bool ManagedObjectWrapper;
        };

        // See CreateObjectFlags in ComWrappers.cs
        enum CreateObjectFlags
        {
            CreateObjectFlags_None = 0,
            CreateObjectFlags_TrackerObject = 1,
            CreateObjectFlags_UniqueInstance = 2,
            CreateObjectFlags_Aggregated = 4,
            CreateObjectFlags_Unwrap = 8,
        };

        // Get the true identity and inner for the supplied IUnknown.
        HRESULT DetermineIdentityAndInnerForExternal(
            _In_ IUnknown* external,
            _In_ enum CreateObjectFlags flags,
            _Outptr_ IUnknown** identity,
            _Inout_ IUnknown** innerMaybe) noexcept;

        // Allocate a wrapper context for an external object.
        // The runtime supplies the external object, flags, and a memory
        // request in order to bring the object into the runtime.
        HRESULT CreateWrapperForExternal(
            _In_ IUnknown* external,
            _In_opt_ IUnknown* inner,
            _In_ enum CreateObjectFlags flags,
            _In_ size_t contextSize,
            _Out_ ExternalWrapperResult* result) noexcept;

        // Inform the wrapper it is being collected.
        void NotifyWrapperForExternalIsBeingCollected(_In_ void* context) noexcept;

         // Destroy the supplied wrapper.
        // Optionally notify the wrapper of collection at the same time.
        void DestroyWrapperForExternal(_In_ void* context, _In_ bool notifyIsBeingCollected = false) noexcept;

        // Separate the supplied wrapper from the tracker runtime.
        void SeparateWrapperFromTrackerRuntime(_In_ void* context) noexcept;

        // Get internal interop IUnknown dispatch pointers.
        void GetIUnknownImpl(
            _Out_ void** fpQueryInterface,
            _Out_ void** fpAddRef,
            _Out_ void** fpRelease) noexcept;

        // Begin the reference tracking process on external COM objects.
        // This should only be called during a runtime's GC phase.
        HRESULT BeginExternalObjectReferenceTracking(_In_ InteropLibImports::RuntimeCallContext* cxt) noexcept;

        // End the reference tracking process.
        // This should only be called during a runtime's GC phase.
        HRESULT EndExternalObjectReferenceTracking() noexcept;
    }
}

#endif // _INTEROP_INC_INTEROPLIB_H_

