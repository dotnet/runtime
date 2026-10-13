// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <dbgutil.h>
#include "elfreader.h"
#include "machoreader.h"

extern "C" bool
TryGetSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress)
{
    if (dataTarget == nullptr || symbolName == nullptr || symbolAddress == nullptr)
    {
        return false;
    }
    *symbolAddress = 0;

    CorDebugTargetInfo targetInfo = {};
    ICorDebugDataTarget5* target = nullptr;
    HRESULT hr = dataTarget->QueryInterface(IID_ICorDebugDataTarget5, reinterpret_cast<void**>(&target));
    if (FAILED(hr))
    {
        return false;
    }
    hr = target->GetTargetInfo(&targetInfo);
    target->Release();
    if (FAILED(hr))
    {
        return false;
    }

    switch (targetInfo.os)
    {
        case CORDB_OS_WINDOWS:
            return PEReader::TryLookupSymbol(dataTarget, baseAddress, symbolName, symbolAddress);
        case CORDB_OS_LINUX:
            return TryGetElfSymbol(dataTarget, baseAddress, symbolName, symbolAddress);
        case CORDB_OS_OSX:
            return TryGetMachOSymbol(dataTarget, baseAddress, symbolName, symbolAddress);
        default:
            return false;
    }
}
