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

    // Delete Object instance handle
    void DeleteObjectInstanceHandle(_In_ InteropLib::OBJECTHANDLE handle) noexcept;

    // Get next External Object Context from the iterator.
    // S_OK - Context is valid.
    // S_FALSE - Iterator has reached end and context out parameter is set to NULL.
    HRESULT IteratorNext(_In_ ExtObjCxtIterator* iter, _Outptr_result_maybenull_ void** context) noexcept;

    // Given a ComWrappers implementation, get or create
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

