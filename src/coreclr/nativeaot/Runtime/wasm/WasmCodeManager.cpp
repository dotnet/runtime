// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "regdisplay.h"
#include "WasmCodeManager.h"
#include "ExceptionHandling.h"
#include "CommonMacros.inl"
#include "NativePrimitiveDecoder.h"

#define GCINFODECODER_NO_EE
#include "gcinfodecoder.cpp"

static WasmCodeManager* s_pCodeManager;

extern "C" void RegisterCodeManager(ICodeManager* pCodeManager, PTR_VOID pvStartRange, uint32_t cbRange);

struct WasmMethodInfo
{
    uint8_t* m_gcInfo;
    uint8_t* m_ehInfo;
    uint32_t m_methodStart;
    uint32_t m_frameSize;
    uint8_t m_flags;
};

static_assert(sizeof(WasmMethodInfo) <= sizeof(MethodInfo));

enum WasmFrameFlags
{
    Funclet = 0x03,
    Filter = 0x02,
    HasEHInfo = 0x04,
    ReversePInvoke = 0x08,
};

// xdata starts with a NativeFormat-encoded unsigned frame size in pointer-sized slots and flags.
// Root records contain an optional EHInfo pointer followed by GCInfo.
// Funclet records contain a linker-relocated function-table slot.
static uint8_t* GetUnwindData(const WasmRuntimeFunction* pFunction, uint32_t* pFrameSize)
{
    uint8_t* p = pFunction->m_unwindData;
    *pFrameSize = NativePrimitiveDecoder::ReadUnsigned(p) * sizeof(uintptr_t);
    return p;
}

static uintptr_t GetPhysicalFramePointer(uintptr_t shadowSP)
{
    ASSERT(shadowSP != 0);
    if (*reinterpret_cast<uintptr_t*>(shadowSP) == 0)
        return *reinterpret_cast<uintptr_t*>(shadowSP + sizeof(uintptr_t));
    return shadowSP;
}

WasmCodeManager::WasmCodeManager(const WasmRuntimeFunction* pFunctions, uint32_t nFunctions,
    void** pClasslibFunctions, uint32_t nClasslibFunctions)
    : m_pFunctions(pFunctions), m_nFunctions(nFunctions),
      m_pClasslibFunctions(pClasslibFunctions), m_nClasslibFunctions(nClasslibFunctions)
{
}

// RyuJIT's shadow frame begins with an identity and a virtual IP. With localloc, SP
// instead points at an indirection to FP. Funclet identities point at the establishing
// frame, tagged with 2; ordinary NativeAOT identities are (method start virtual IP << 1) | 1.
uintptr_t WasmCodeManager::GetFramePointer(uintptr_t shadowSP)
{
    uintptr_t fp = GetPhysicalFramePointer(shadowSP);
    ASSERT(fp != 0);

    uintptr_t identity = *reinterpret_cast<uintptr_t*>(fp);
    while ((identity & 3) == 2)
    {
        uintptr_t establishingFrame = identity - 2;
        ASSERT(establishingFrame > fp);
        fp = establishingFrame;
        identity = *reinterpret_cast<uintptr_t*>(fp);
    }
    return fp;
}

uintptr_t WasmCodeManager::GetVirtualIP(uintptr_t shadowSP)
{
    uintptr_t fp = GetPhysicalFramePointer(shadowSP);
    if (*reinterpret_cast<uintptr_t*>(fp) == WasmEHFrame::Identity)
        return reinterpret_cast<WasmEHFrame*>(fp)->m_returnIP;
    uint32_t virtualIP = *reinterpret_cast<uint32_t*>(fp + sizeof(uintptr_t));
    uintptr_t identity = *reinterpret_cast<uintptr_t*>(GetFramePointer(shadowSP));
    ASSERT((identity & 1) != 0 && identity != 1);
    return (identity >> 1) + virtualIP;
}

const WasmRuntimeFunction* WasmCodeManager::FindRuntimeFunction(uintptr_t address)
{
    uint32_t low = 0;
    uint32_t high = m_nFunctions;
    while (low < high)
    {
        uint32_t middle = low + (high - low) / 2;
        if (m_pFunctions[middle].m_virtualStart <= address)
            low = middle + 1;
        else
            high = middle;
    }
    if (low == 0 || address >= m_pFunctions[low].m_virtualStart)
        return nullptr;
    return &m_pFunctions[low - 1];
}

uintptr_t WasmCodeManager::GetFuncletAddress(uintptr_t virtualIP)
{
    const WasmRuntimeFunction* pFunction = FindRuntimeFunction(virtualIP);
    ASSERT(pFunction != nullptr);
    uint32_t frameSize;
    uint8_t* p = GetUnwindData(pFunction, &frameSize);
    ASSERT((*p & Funclet) != 0);
    p++;
    return NativePrimitiveDecoder::ReadUInt32(p);
}

bool WasmCodeManager::FindMethodInfo(PTR_VOID controlPC, MethodInfo* pMethodInfoOut)
{
    const WasmRuntimeFunction* pFunction = FindRuntimeFunction(reinterpret_cast<uintptr_t>(controlPC));
    if (pFunction == nullptr)
        return false;
    WasmMethodInfo* pMethodInfo = reinterpret_cast<WasmMethodInfo*>(pMethodInfoOut);
    uint8_t* p = GetUnwindData(pFunction, &pMethodInfo->m_frameSize);
    pMethodInfo->m_flags = *p;
    while ((*p & Funclet) != 0)
    {
        ASSERT(pFunction > m_pFunctions);
        pFunction--;
        uint32_t frameSize;
        p = GetUnwindData(pFunction, &frameSize);
    }
    pMethodInfo->m_methodStart = pFunction->m_virtualStart;
    uint8_t flags = *p++;
    pMethodInfo->m_ehInfo = (flags & HasEHInfo) != 0
        ? reinterpret_cast<uint8_t*>(NativePrimitiveDecoder::ReadUInt32(p)) : nullptr;
    pMethodInfo->m_gcInfo = p;
    return true;
}

bool WasmCodeManager::IsFunclet(MethodInfo* pMethodInfo)
{
    return (reinterpret_cast<WasmMethodInfo*>(pMethodInfo)->m_flags & Funclet) != 0;
}

PTR_VOID WasmCodeManager::GetFramePointer(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet)
{
    return reinterpret_cast<PTR_VOID>(pRegisterSet->GetFP());
}

bool WasmCodeManager::IsSafePoint(PTR_VOID address)
{
    MethodInfo info;
    if (!FindMethodInfo(address, &info))
        return false;
    WasmMethodInfo* pMethodInfo = reinterpret_cast<WasmMethodInfo*>(&info);
    GcInfoDecoder decoder(GCInfoToken(pMethodInfo->m_gcInfo), DECODE_INTERRUPTIBILITY,
        static_cast<uint32_t>(reinterpret_cast<uintptr_t>(address) - pMethodInfo->m_methodStart));
    return decoder.IsInterruptible() || decoder.IsSafePoint();
}

void WasmCodeManager::EnumGcRefs(MethodInfo* pMethodInfo, PTR_VOID safePointAddress,
    REGDISPLAY* pRegisterSet, GCEnumContext* hCallback, bool isActiveStackFrame)
{
    WasmMethodInfo* pWasmMethodInfo = reinterpret_cast<WasmMethodInfo*>(pMethodInfo);
    uint32_t offset = static_cast<uint32_t>(reinterpret_cast<uintptr_t>(safePointAddress) - pWasmMethodInfo->m_methodStart);
    GcInfoDecoder decoder(GCInfoToken(pWasmMethodInfo->m_gcInfo),
        GcInfoDecoderFlags(DECODE_GC_LIFETIMES | DECODE_SECURITY_OBJECT | DECODE_VARARG), offset);
    uint32_t flags = isActiveStackFrame ? ICodeManagerFlags::ActiveStackFrame : 0;
    if ((pWasmMethodInfo->m_flags & Filter) != 0)
        flags |= ICodeManagerFlags::NoReportUntracked;
    if (!decoder.EnumerateLiveSlots(pRegisterSet, isActiveStackFrame,
        static_cast<ICodeManagerFlags>(flags), hCallback->pCallback, hCallback))
    {
        assert(false);
    }
}

bool WasmCodeManager::UnwindStackFrame(MethodInfo* pMethodInfo, uint32_t flags,
    REGDISPLAY* pRegisterSet, PInvokeTransitionFrame** ppPreviousTransitionFrame)
{
    WasmMethodInfo* pWasmMethodInfo = reinterpret_cast<WasmMethodInfo*>(pMethodInfo);
    *ppPreviousTransitionFrame = nullptr;
    if ((pWasmMethodInfo->m_flags & ReversePInvoke) != 0)
    {
        ASSERT((flags & USFF_StopUnwindOnTransitionFrame) != 0);
        GcInfoDecoder decoder(GCInfoToken(pWasmMethodInfo->m_gcInfo), DECODE_REVERSE_PINVOKE_VAR);
        int32_t slot = decoder.GetReversePInvokeFrameStackSlot();
        ASSERT(slot != NO_REVERSE_PINVOKE_FRAME);
        uintptr_t base = decoder.GetStackBaseRegister() == NO_STACK_BASE_REGISTER
            ? pRegisterSet->GetSP() : pRegisterSet->GetFP();
        *ppPreviousTransitionFrame = *reinterpret_cast<PInvokeTransitionFrame**>(base + slot);
        // The register display still describes the current managed frame.
        // The iterator either reconnects through the published transition frame
        // or terminates EH with UnwoundReversePInvoke set.
        return true;
    }

    uintptr_t fp = GetPhysicalFramePointer(pRegisterSet->GetSP());
    uintptr_t callerSP = fp + pWasmMethodInfo->m_frameSize;
    ASSERT(callerSP > pRegisterSet->GetSP());
    pRegisterSet->SetSP(callerSP);
    pRegisterSet->FP = GetFramePointer(callerSP);
    pRegisterSet->SetIP(GetVirtualIP(callerSP));
    return true;
}

uintptr_t WasmCodeManager::GetConservativeUpperBoundForOutgoingArgs(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet)
{
    assert(false);
    return 0;
}

bool WasmCodeManager::IsUnwindable(PTR_VOID address)
{
    MethodInfo info;
    return FindMethodInfo(address, &info);
}

bool WasmCodeManager::GetReturnAddressHijackInfo(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet,
    PTR_PTR_VOID* ppRetAddrLocation, uintptr_t* pSpForArm64PacSign)
{
    return false;
}

PTR_VOID WasmCodeManager::RemapHardwareFaultToGCSafePoint(MethodInfo* pMethodInfo, PTR_VOID controlPC)
{
    PORTABILITY_ASSERT("WASM hardware fault GC safe point");
    return controlPC;
}

struct WasmEHEnumState
{
    PTR_uint8_t pMethodStartAddress;
    PTR_uint8_t pEHInfo;
    uint32_t uClause;
    uint32_t nClauses;
};

// Ensure that WasmEHEnumState fits into the space reserved by EHEnumState
static_assert(sizeof(WasmEHEnumState) <= sizeof(EHEnumState), "WasmEHEnumState too big");

bool WasmCodeManager::EHEnumInit(MethodInfo * pMethodInfo, PTR_VOID * pMethodStartAddress, EHEnumState * pEHEnumStateOut)
{
    assert(pMethodInfo != NULL);
    assert(pMethodStartAddress != NULL);
    assert(pEHEnumStateOut != NULL);

    WasmMethodInfo * pNativeMethodInfo = (WasmMethodInfo *)pMethodInfo;

    // return if there is no EH info associated with this method
    if (pNativeMethodInfo->m_ehInfo == nullptr)
    {
        return false;
    }

    WasmEHEnumState * pEnumState = (WasmEHEnumState *)pEHEnumStateOut;

    *pMethodStartAddress = reinterpret_cast<PTR_VOID>(pNativeMethodInfo->m_methodStart);

    pEnumState->pMethodStartAddress = dac_cast<PTR_uint8_t>(*pMethodStartAddress);
    pEnumState->pEHInfo = pNativeMethodInfo->m_ehInfo;
    pEnumState->uClause = 0;
    pEnumState->nClauses = NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);

    return true;
}

bool WasmCodeManager::EHEnumNext(EHEnumState * pEHEnumState, EHClause * pEHClauseOut)
{
    assert(pEHEnumState != NULL);
    assert(pEHClauseOut != NULL);

    WasmEHEnumState * pEnumState = (WasmEHEnumState *)pEHEnumState;
    if (pEnumState->uClause >= pEnumState->nClauses)
    {
        return false;
    }

    pEnumState->uClause++;

    pEHClauseOut->m_tryStartOffset = NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);

    uint32_t tryEndDeltaAndClauseKind = NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);
    pEHClauseOut->m_clauseKind = (EHClauseKind)(tryEndDeltaAndClauseKind & 0x3);
    pEHClauseOut->m_tryEndOffset = pEHClauseOut->m_tryStartOffset + (tryEndDeltaAndClauseKind >> 2);

    // For each clause, we have up to 4 integers:
    //      1)  try start offset
    //      2)  (try length << 2) | clauseKind
    //      3)  if (typed || fault || filter)    { handler start offset }
    //      4a) if (typed)                       { type RVA }
    //      4b) if (filter)                      { filter start offset }
    //
    // The first two integers have already been decoded

    switch (pEHClauseOut->m_clauseKind)
    {
    case EH_CLAUSE_TYPED:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);

        // Read target type
        {
            // @TODO: Compress EHInfo using type table index scheme
            // https://github.com/dotnet/corert/issues/972
            uint8_t* pBase = pEnumState->pEHInfo;
            int32_t typeRelAddr = NativePrimitiveDecoder::ReadInt32(pEnumState->pEHInfo);
            pEHClauseOut->m_pTargetType = dac_cast<PTR_VOID>(pBase + typeRelAddr);
        }
        break;
    case EH_CLAUSE_FAULT:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);
        break;
    case EH_CLAUSE_FILTER:
        pEHClauseOut->m_handlerAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);
        pEHClauseOut->m_filterAddress = dac_cast<uint8_t*>(PINSTRToPCODE(dac_cast<TADDR>(pEnumState->pMethodStartAddress))) + NativePrimitiveDecoder::ReadUnsigned(pEnumState->pEHInfo);
        break;
    default:
        UNREACHABLE_MSG("unexpected EHClauseKind");
    }

    return true;
}

PTR_VOID WasmCodeManager::GetMethodStartAddress(MethodInfo* pMethodInfo)
{
    return reinterpret_cast<PTR_VOID>(reinterpret_cast<WasmMethodInfo*>(pMethodInfo)->m_methodStart);
}

PTR_VOID WasmCodeManager::GetOsModuleHandle()
{
    return nullptr;
}

void* WasmCodeManager::GetClasslibFunction(ClasslibFunctionId functionId)
{
    uint32_t index = static_cast<uint32_t>(functionId);
    return index < m_nClasslibFunctions ? m_pClasslibFunctions[index] : nullptr;
}

PTR_VOID WasmCodeManager::GetAssociatedData(PTR_VOID controlPC)
{
    return nullptr;
}

EXTERN_C bool RhRegisterWasmModule(void* pFunctions, uint32_t cbFunctions,
    void** pClasslibFunctions, uint32_t nClasslibFunctions)
{
    ASSERT(s_pCodeManager == nullptr);
    ASSERT(cbFunctions % sizeof(WasmRuntimeFunction) == 0);
    const WasmRuntimeFunction* pRuntimeFunctions = static_cast<const WasmRuntimeFunction*>(pFunctions);
    uint32_t nEntries = cbFunctions / sizeof(WasmRuntimeFunction);
    if (nEntries < 2)
        return false;
    uint32_t nFunctions = nEntries - 1;
    ASSERT(pRuntimeFunctions[nFunctions].m_unwindData == nullptr);
    uint32_t virtualStart = pRuntimeFunctions[0].m_virtualStart;
    uint32_t virtualEnd = pRuntimeFunctions[nFunctions].m_virtualStart;
    ASSERT(virtualStart != 0 && virtualStart < virtualEnd);
    WasmCodeManager* pCodeManager = new (nothrow) WasmCodeManager(
        pRuntimeFunctions, nFunctions, pClasslibFunctions, nClasslibFunctions);
    if (pCodeManager == nullptr)
        return false;
    s_pCodeManager = pCodeManager;
    RegisterCodeManager(pCodeManager, reinterpret_cast<PTR_VOID>(virtualStart), virtualEnd - virtualStart);
    return true;
}
