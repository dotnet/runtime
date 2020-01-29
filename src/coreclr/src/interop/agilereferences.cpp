// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "comwrappers.h"

#if (NTDDI_VERSION >= NTDDI_WINBLUE)
#include <combaseapi.h>

#else
// Forward declare if OS verion is not set high enough.
enum AgileReferenceOptions
{
    AGILEREFERENCE_DEFAULT        = 0,
    AGILEREFERENCE_DELAYEDMARSHAL = 1,
};

WINOLEAPI RoGetAgileReference(
    _In_ enum AgileReferenceOptions options,
    _In_ REFIID riid,
    _In_ IUnknown* pUnk,
    _COM_Outptr_ IAgileReference** ppAgileReference
    );

#endif

template<>
HRESULT CreateAgileReference<IUnknown>(
    _In_ IUnknown* object,
    _Outptr_ IAgileReference** agileReference)
{
    // [TODO] Handle this on pre-Windows 8.1 plaforms
    _ASSERTE(object != nullptr && agileReference != nullptr);
    return ::RoGetAgileReference(AGILEREFERENCE_DEFAULT, __uuidof(object), object, agileReference);
}
