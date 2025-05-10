// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTEROP_INC_INTEROPLIBIMPORTS_H_
#define _INTEROP_INC_INTEROPLIBIMPORTS_H_

#include "interoplib.h"

namespace InteropLibImports
{
    // Check if Object instance handle still points at an Object.
    bool HasValidTarget(_In_ InteropLib::OBJECTHANDLE handle) noexcept;

    void DestroyHandle(_In_ InteropLib::OBJECTHANDLE handle) noexcept;

    bool IsObjectPromoted(_In_ InteropLib::OBJECTHANDLE handle) noexcept;

    // Get the current global pegging state.
    bool GetGlobalPeggingState() noexcept;

    // Set the current global pegging state.
    void SetGlobalPeggingState(_In_ bool state) noexcept;

    // Get next External Object Context from the Runtime calling context.
    bool IteratorNext(
        _In_ RuntimeCallContext* runtimeContext,
        _Outptr_result_maybenull_ void** trackerTarget,
        _Outptr_result_maybenull_ InteropLib::OBJECTHANDLE* proxyObject) noexcept;

    // Tell the runtime a reference path between the External Object Context and
    // OBJECTHANDLE was found.
    HRESULT FoundReferencePath(
        _In_ RuntimeCallContext* runtimeContext,
        _In_ InteropLib::OBJECTHANDLE sourceHandle,
        _In_ InteropLib::OBJECTHANDLE targetHandle) noexcept;

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

