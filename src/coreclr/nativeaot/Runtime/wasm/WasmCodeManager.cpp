// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "regdisplay.h"
#include "WasmCodeManager.h"
#include "CommonMacros.inl"
#include "NativePrimitiveDecoder.h"

#define GCINFODECODER_NO_EE
#include "gcinfodecoder.cpp"

static WasmCodeManager* s_pCodeManager;

extern "C" void RegisterCodeManager(ICodeManager* pCodeManager, PTR_VOID pvStartRange, uint32_t cbRange);

struct WasmMethodInfo
{
    uint8_t* m_gcInfo;
    uint32_t m_methodStart;
    uint32_t m_frameSize;
    uint8_t m_flags;
};

static_assert(sizeof(WasmMethodInfo) <= sizeof(MethodInfo));

enum WasmFrameFlags
{
    Funclet = 0x03,
    Filter = 0x02,
    ReversePInvoke = 0x08,
};

// xdata starts with a NativeFormat-encoded unsigned frame size in pointer-sized slots and flags.
// Root records then contain GCInfo.
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
    uint32_t virtualIP = *reinterpret_cast<uint32_t*>(fp + sizeof(uintptr_t));
    uintptr_t identity = *reinterpret_cast<uintptr_t*>(GetFramePointer(shadowSP));
    ASSERT((identity & 1) != 0 && identity != 1);
    return (identity >> 1) + virtualIP;
}

bool WasmCodeManager::FindMethodInfo(PTR_VOID controlPC, MethodInfo* pMethodInfoOut)
{
    uintptr_t address = reinterpret_cast<uintptr_t>(controlPC);
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
        return false;
    const WasmRuntimeFunction* pFunction = &m_pFunctions[low - 1];
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
    pMethodInfo->m_gcInfo = p + sizeof(uint8_t);
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
        GcInfoDecoder decoder(GCInfoToken(pWasmMethodInfo->m_gcInfo), DECODE_REVERSE_PINVOKE_VAR);
        int32_t slot = decoder.GetReversePInvokeFrameStackSlot();
        ASSERT(slot != NO_REVERSE_PINVOKE_FRAME);
        uintptr_t base = decoder.GetStackBaseRegister() == NO_STACK_BASE_REGISTER
            ? pRegisterSet->GetSP() : pRegisterSet->GetFP();
        *ppPreviousTransitionFrame = *reinterpret_cast<PInvokeTransitionFrame**>(base + slot);
        if ((flags & USFF_StopUnwindOnTransitionFrame) != 0)
            return true;
        PORTABILITY_ASSERT("WASM unwind across native frames");
        return false;
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

bool WasmCodeManager::EHEnumInit(MethodInfo* pMethodInfo, PTR_VOID* pMethodStartAddress, EHEnumState* pEHEnumState)
{
    PORTABILITY_ASSERT("WASM exception handling");
    return false;
}

bool WasmCodeManager::EHEnumNext(EHEnumState* pEHEnumState, EHClause* pEHClause)
{
    PORTABILITY_ASSERT("WASM exception handling");
    return false;
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
