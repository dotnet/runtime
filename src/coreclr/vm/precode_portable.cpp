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

bool PortableEntryPoint::HasNativeEntryPoint(PCODE addr)
{
    LIMITED_METHOD_CONTRACT;
    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    return portableEntryPoint->HasNativeCode();
}

void* PortableEntryPoint::GetActualCode(PCODE addr)
{
    STANDARD_VM_CONTRACT;

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(portableEntryPoint->HasNativeCode());
    return portableEntryPoint->_pActualCode;
}

void PortableEntryPoint::SetActualCode(PCODE addr, PCODE actualCode)
{
    STANDARD_VM_CONTRACT;

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(actualCode != (PCODE)NULL);

    // This is a lock free write. It can either be NULL or was already set to the same value.
    _ASSERTE(!portableEntryPoint->HasNativeCode() || portableEntryPoint->_pActualCode == (void*)PCODEToPINSTR(actualCode));
    portableEntryPoint->_pActualCode = (void*)PCODEToPINSTR(actualCode);
}

MethodDesc* PortableEntryPoint::GetMethodDesc(PCODE addr)
{
    STANDARD_VM_CONTRACT;

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(portableEntryPoint->_pMD != nullptr);
    return portableEntryPoint->_pMD;
}

void* PortableEntryPoint::GetInterpreterData(PCODE addr)
{
    STANDARD_VM_CONTRACT;

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(portableEntryPoint->HasInterpreterCode());
    return portableEntryPoint->_pInterpreterData;
}

void PortableEntryPoint::SetInterpreterData(PCODE addr, PCODE interpreterData)
{
    STANDARD_VM_CONTRACT;

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(!portableEntryPoint->HasInterpreterCode());
    _ASSERTE(interpreterData != (PCODE)NULL);
    portableEntryPoint->_pInterpreterData = (void*)PCODEToPINSTR(interpreterData);
}

PortableEntryPoint* PortableEntryPoint::ToPortableEntryPoint(PCODE addr)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(addr != NULL);

    PortableEntryPoint* portableEntryPoint = (PortableEntryPoint*)PCODEToPINSTR(addr);
    _ASSERTE(portableEntryPoint->IsValid());
    return portableEntryPoint;
}

#ifdef _DEBUG
bool PortableEntryPoint::IsValid() const
{
    LIMITED_METHOD_CONTRACT;
    return _canary == CANARY_VALUE;
}
#endif // _DEBUG

void PortableEntryPoint::Init(MethodDesc* pMD)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pMD != NULL);
    _pActualCode = NULL;
    _pMD = pMD;
    _pInterpreterData = NULL;
    INDEBUG(_canary = CANARY_VALUE);
}

void PortableEntryPoint::Init(void* nativeEntryPoint)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(nativeEntryPoint != NULL);
    _pActualCode = nativeEntryPoint;
    _pMD = NULL;
    _pInterpreterData = NULL;
    INDEBUG(_canary = CANARY_VALUE);
}

InterleavedLoaderHeapConfig s_stubPrecodeHeapConfig;

void StubPrecode::Init(StubPrecode* pPrecodeRX, TADDR secretParam, LoaderAllocator *pLoaderAllocator, TADDR type, TADDR target)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"StubPrecode::Init is not supported with Portable EntryPoints");
}

BYTE StubPrecode::GetType()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"StubPrecode::GetType is not supported with Portable EntryPoints");
    return 0;
}

void StubPrecode::SetTargetUnconditional(TADDR target)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"StubPrecode::SetTargetUnconditional is not supported with Portable EntryPoints");
}

TADDR StubPrecode::GetSecretParam() const
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"StubPrecode::GetSecretParam is not supported with Portable EntryPoints");
    return (TADDR)NULL;
}

MethodDesc* StubPrecode::GetMethodDesc()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"StubPrecode::GetMethodDesc is not supported with Portable EntryPoints");
    return NULL;
}

PCODE* FixupPrecode::GetTargetSlot()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"FixupPrecode::GetTargetSlot is not supported with Portable EntryPoints");
    return NULL;
}

MethodDesc* FixupPrecode::GetMethodDesc()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"FixupPrecode::GetMethodDesc is not supported with Portable EntryPoints");
    return NULL;
}

Precode* Precode::Allocate(PrecodeType t, MethodDesc* pMD,
    LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::Allocate is not supported with Portable EntryPoints");
    return NULL;
}

PrecodeType Precode::GetType()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::GetType is not supported with Portable EntryPoints");
    return (PrecodeType)0;
}

UMEntryThunk* Precode::AsUMEntryThunk()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::AsUMEntryThunk is not supported with Portable EntryPoints");
    return NULL;
}

StubPrecode* Precode::AsStubPrecode()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::AsStubPrecode is not supported with Portable EntryPoints");
    return NULL;
}

MethodDesc* Precode::GetMethodDesc(BOOL fSpeculative)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::GetMethodDesc is not supported with Portable EntryPoints");
    return NULL;
}

PCODE Precode::GetEntryPoint()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::GetEntryPoint is not supported with Portable EntryPoints");
    return (PCODE)NULL;
}

BOOL Precode::IsPointingToNativeCode(PCODE pNativeCode)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::IsPointingToNativeCode is not supported with Portable EntryPoints");
    return FALSE;
}

void Precode::Reset()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::Reset is not supported with Portable EntryPoints");
}

PCODE Precode::GetTarget()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::GetTarget is not supported with Portable EntryPoints");
    return (PCODE)NULL;
}

void Precode::ResetTargetInterlocked()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::ResetTargetInterlocked is not supported with Portable EntryPoints");
}

BOOL Precode::SetTargetInterlocked(PCODE target, BOOL fOnlyRedirectFromPrestub)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::SetTargetInterlocked is not supported with Portable EntryPoints");
    return FALSE;
}

BOOL Precode::IsPointingToPrestub()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::IsPointingToPrestub is not supported with Portable EntryPoints");
    return FALSE;
}

BOOL Precode::IsPointingToPrestub(PCODE target)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::IsPointingToPrestub is not supported with Portable EntryPoints");
    return FALSE;
}

void FlushCacheForDynamicMappedStub(void* code, SIZE_T size)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"FlushCacheForDynamicMappedStub is not supported with Portable EntryPoints");
}

BOOL DoesSlotCallPrestub(PCODE pCode)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"DoesSlotCallPrestub is not supported with Portable EntryPoints");
    return FALSE;
}

#endif // FEATURE_PORTABLE_ENTRYPOINTS
