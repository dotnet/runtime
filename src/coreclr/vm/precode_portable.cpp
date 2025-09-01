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

#define NATIVE_ENTRYPOINT_HELPER_BIT 0x1

bool PortableEntryPoint::IsNativeEntryPoint(TADDR addr)
{
    LIMITED_METHOD_CONTRACT;
    return (addr & NATIVE_ENTRYPOINT_HELPER_BIT) == NATIVE_ENTRYPOINT_HELPER_BIT;
}

TADDR PortableEntryPoint::MarkNativeEntryPoint(TADDR entryPoint)
{
    LIMITED_METHOD_CONTRACT;
    return entryPoint | NATIVE_ENTRYPOINT_HELPER_BIT;
}

void* PortableEntryPoint::GetActualCode(TADDR addr)
{
    STANDARD_VM_CONTRACT;

    if (IsNativeEntryPoint(addr))
    {
        const TADDR mask = ~NATIVE_ENTRYPOINT_HELPER_BIT;
        return (void*)(addr & mask);
    }

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(portableEntryPoint->_pActualCode != (BYTE*)NULL);
    return portableEntryPoint->_pActualCode;
}

void PortableEntryPoint::SetActualCode(TADDR addr, void* actualCode)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(actualCode != NULL);

    PortableEntryPoint* portableEntryPoint = ToPortableEntryPoint(addr);
    _ASSERTE(portableEntryPoint->_pActualCode == (BYTE*)NULL || portableEntryPoint->_pActualCode == (BYTE*)actualCode);
    portableEntryPoint->_pActualCode = (BYTE*)actualCode;
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
    _ASSERTE(interpreterData != (PCODE)NULL);
    portableEntryPoint->_pInterpreterData = (void*)PCODEToPINSTR(interpreterData);
}

PortableEntryPoint* PortableEntryPoint::ToPortableEntryPoint(TADDR addr)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(addr != NULL);
    _ASSERTE(!IsNativeEntryPoint(addr));

    PortableEntryPoint* portableEntryPoint = (PortableEntryPoint*)addr;
    _ASSERTE(portableEntryPoint->IsValid());
    return portableEntryPoint;
}

void PortableEntryPoint::Init(MethodDesc* pMD)
{
    LIMITED_METHOD_CONTRACT;
    _pActualCode = NULL;
    _pMD = pMD;
    _pInterpreterData = NULL;
    INDEBUG(_canary = CANARY_VALUE);
}

bool PortableEntryPoint::IsValid() const
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(_canary == CANARY_VALUE);
    return _pMD != nullptr;
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

Precode* Precode::GetPrecodeFromEntryPoint(PCODE addr, BOOL fSpeculative)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Precode::GetPrecodeFromEntryPoint is not supported with Portable EntryPoints");
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
