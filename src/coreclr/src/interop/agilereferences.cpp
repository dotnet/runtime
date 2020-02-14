// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Runtime headers
#include <winwrap.h>

#include "comwrappers.hpp"

// Forward declare the Win32 API.
enum AgileReferenceOptions
{
    AGILEREFERENCE_DEFAULT        = 0,
    AGILEREFERENCE_DELAYEDMARSHAL = 1,
};

EXTERN_C HRESULT STDAPICALLTYPE RoGetAgileReference(
    _In_ enum AgileReferenceOptions options,
    _In_ REFIID riid,
    _In_ IUnknown* pUnk,
    _COM_Outptr_ IAgileReference** ppAgileReference
    );

namespace
{
    // Global function pointer for RoGetAgileReference
    decltype(RoGetAgileReference)* fpRoGetAgileReference;
}

template<>
HRESULT CreateAgileReference<IUnknown>(
    _In_ IUnknown* object,
    _Outptr_ IAgileReference** agileReference)
{
    _ASSERTE(object != nullptr && agileReference != nullptr);

    // If the pointer isn't set, then attempt to load it in process.
    if (fpRoGetAgileReference == nullptr)
    {
        HMODULE hmod = WszLoadLibrary(W("ole32.dll"));
        if (hmod != nullptr)
            fpRoGetAgileReference = (decltype(RoGetAgileReference)*)::GetProcAddress(hmod, "RoGetAgileReference");

        // Could't find binary or export. Either way, the OS version is too old.
        if (fpRoGetAgileReference == nullptr)
            return HRESULT_FROM_WIN32(ERROR_OLD_WIN_VERSION);
    }

    return fpRoGetAgileReference(AGILEREFERENCE_DEFAULT, __uuidof(object), object, agileReference);
}
