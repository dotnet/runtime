// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_INC_INTEROPLIBIMPORTS_H_
#define _INTEROP_INC_INTEROPLIBIMPORTS_H_

#include "interoplib.h"

namespace InteropLibImports
{
    enum class AllocScenario
    {
        ManagedObjectWrapper,
        NativeObjectWrapper,
    };

    // Allocate the given amount of memory.
    void* MemAlloc(_In_ size_t sizeInBytes, _In_ AllocScenario scenario) noexcept;

    // Free the previously allocated memory.
    void MemFree(_In_ void* mem, _In_ AllocScenario scenario) noexcept;

    // Add memory pressure to the runtime's GC calculations.
    HRESULT AddMemoryPressureForExternal(_In_ UINT64 memoryInBytes) noexcept;

    // Remove memory pressure from the runtime's GC calculations.
    HRESULT RemoveMemoryPressureForExternal(_In_ UINT64 memoryInBytes) noexcept;

    enum class GcRequest
    {
        Default,
        FullBlocking // This is an expensive GC request, akin to a Gen2/"stop the world" GC.
    };

    // Request a GC from the runtime.
    HRESULT RequestGarbageCollectionForExternal(_In_ GcRequest req) noexcept;

    // Wait for the runtime's finalizer to clean up objects.
    HRESULT WaitForRuntimeFinalizerForExternal() noexcept;

    // Release objects associated with the current thread.
    HRESULT ReleaseExternalObjectsFromCurrentThread() noexcept;

    // Delete Object instance handle.
    void DeleteObjectInstanceHandle(_In_ InteropLib::OBJECTHANDLE handle) noexcept;

    // Check if Object instance handle still points at an Object.
    bool HasValidTarget(_In_ InteropLib::OBJECTHANDLE handle) noexcept;

    // Get the current global pegging state.
    bool GetGlobalPeggingState() noexcept;

    // Set the current global pegging state.
    void SetGlobalPeggingState(_In_ bool state) noexcept;

    // Get next External Object Context from the Runtime calling context.
    // S_OK - Context is valid.
    // S_FALSE - Iterator has reached end and context out parameter is set to NULL.
    HRESULT IteratorNext(
        _In_ RuntimeCallContext* runtimeContext,
        _Outptr_result_maybenull_ void** extObjContext) noexcept;

    // Tell the runtime a reference path between the External Object Context and
    // OBJECTHANDLE was found.
    HRESULT FoundReferencePath(
        _In_ RuntimeCallContext* runtimeContext,
        _In_ void* extObjContext,
        _In_ InteropLib::OBJECTHANDLE handle) noexcept;

    // Get or create an IReferenceTrackerTarget instance for the supplied
    // external object.
    HRESULT GetOrCreateTrackerTargetForExternal(
        _In_ IUnknown* externalComObject,
        _In_ InteropLib::Com::CreateObjectFlags externalObjectFlags,
        _In_ InteropLib::Com::CreateComInterfaceFlags trackerTargetFlags,
        _Outptr_ void** trackerTarget) noexcept;

    // The enum describes the value of System.Runtime.InteropServices.CustomQueryInterfaceResult
    // and the case where the object doesn't support ICustomQueryInterface.
    enum class TryInvokeICustomQueryInterfaceResult
    {
        OnGCThread = -2,
        FailedToInvoke = -1,
        Handled = 0,
        NotHandled = 1,
        Failed = 2,

        // Range checks
        Min = OnGCThread,
        Max = Failed,
    };

    // Attempt to call the ICustomQueryInterface on the supplied object.
    // Returns S_FALSE if the object doesn't support ICustomQueryInterface.
    TryInvokeICustomQueryInterfaceResult TryInvokeICustomQueryInterface(
        _In_ InteropLib::OBJECTHANDLE handle,
        _In_ REFGUID iid,
        _Outptr_result_maybenull_ void** obj) noexcept;
}

#endif // _INTEROP_INC_INTEROPLIBIMPORTS_H_

