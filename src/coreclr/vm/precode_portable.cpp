// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifdef FEATURE_PORTABLE_ENTRYPOINTS

#include "common.h"
#include "precode_portable.hpp"

#ifdef HOST_64BIT
    #define CANARY_VALUE 0x1234567812345678
#else // HOST_64BIT
    #define CANARY_VALUE 0x12345678
#endif // HOST_64BIT

bool PortableEntryPoint::IsNativeEntryPoint(TADDR addr)
{
    STANDARD_VM_CONTRACT;

    return false;
}

MethodDesc* PortableEntryPoint::GetMethodDesc(TADDR addr)
{
    STANDARD_VM_CONTRACT;

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(portableEntryPoint->_pMD != NULL);
    return portableEntryPoint->_pMD;
}

void* PortableEntryPoint::GetInterpreterData(TADDR addr)
{
    STANDARD_VM_CONTRACT;

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(portableEntryPoint->_pInterpreterData != NULL);
    return portableEntryPoint->_pInterpreterData;
}

void PortableEntryPoint::SetInterpreterData(TADDR addr, PCODE interpreterData)
{
    STANDARD_VM_CONTRACT;

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(portableEntryPoint->_pInterpreterData == NULL);
    portableEntryPoint->_pInterpreterData = (void*)PCODEToPINSTR(interpreterData);
}

PortableEntryPoint* PortableEntryPoint::ToPortableEntryPoint(TADDR addr)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(addr != NULL);

    PortableEntryPoint* portableEntryPoint = (PortableEntryPoint*)addr;
    _ASSERTE(portableEntryPoint->_canary == CANARY_VALUE);
    return portableEntryPoint;
}

void PortableEntryPoint::Init(MethodDesc* pMD)
{
    LIMITED_METHOD_CONTRACT;
    INDEBUG(_canary = CANARY_VALUE);
    _pMD = pMD;
    _pInterpreterData = NULL;
    _pActualCode = NULL;
}

void FlushCacheForDynamicMappedStub(void* code, SIZE_T size)
{

}

BOOL DoesSlotCallPrestub(PCODE pCode)
{
    return FALSE;
}

InterleavedLoaderHeapConfig s_stubPrecodeHeapConfig;
#ifdef HAS_FIXUP_PRECODE
InterleavedLoaderHeapConfig s_fixupStubPrecodeHeapConfig;
#endif

#endif // FEATURE_PORTABLE_ENTRYPOINTS
