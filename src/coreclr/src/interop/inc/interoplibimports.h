// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    // [TODO] HRESULT RequestGarbageCollectionForExternal() noexcept;

    // Delete Object instance handle
    void DeleteObjectInstanceHandle(_In_ InteropLib::OBJECTHANDLE handle) noexcept;

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

    // Given a runtime implementation, get or create
    // an IReferenceTrackerTarget instance for the supplied
    // external object.
    HRESULT GetOrCreateTrackerTargetForExternal(
        _In_ InteropLib::OBJECTHANDLE impl,
        _In_ IUnknown* externalComObject,
        _In_ INT32 externalObjectFlags,
        _In_ INT32 trackerTargetFlags,
        _Outptr_ void** trackerTarget) noexcept;
}

#endif // _INTEROP_INC_INTEROPLIBIMPORTS_H_

