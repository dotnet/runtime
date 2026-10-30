// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "ICodeManager.h"

// Keep in sync with WasmRelocatableObjectWriter.EmitUnwindInfo.
struct WasmRuntimeFunction
{
    uint32_t m_virtualStart;
    uint8_t* m_unwindData;
};

static_assert(sizeof(WasmRuntimeFunction) == 2 * sizeof(uint32_t));

class WasmCodeManager final : public ICodeManager
{
    const WasmRuntimeFunction* m_pFunctions;
    uint32_t m_nFunctions; // Excludes the terminal entry containing the end virtual IP.
    void** m_pClasslibFunctions;
    uint32_t m_nClasslibFunctions;

public:
    WasmCodeManager(const WasmRuntimeFunction* pFunctions, uint32_t nFunctions,
        void** pClasslibFunctions, uint32_t nClasslibFunctions);

    static uintptr_t GetFramePointer(uintptr_t shadowSP);
    static uintptr_t GetVirtualIP(uintptr_t shadowSP);

    bool FindMethodInfo(PTR_VOID controlPC, MethodInfo* pMethodInfoOut) override;
    bool IsFunclet(MethodInfo* pMethodInfo) override;
    PTR_VOID GetFramePointer(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet) override;
    bool IsSafePoint(PTR_VOID address) override;
    void EnumGcRefs(MethodInfo* pMethodInfo, PTR_VOID safePointAddress, REGDISPLAY* pRegisterSet,
        GCEnumContext* hCallback, bool isActiveStackFrame) override;
    bool UnwindStackFrame(MethodInfo* pMethodInfo, uint32_t flags, REGDISPLAY* pRegisterSet,
        PInvokeTransitionFrame** ppPreviousTransitionFrame) override;
    uintptr_t GetConservativeUpperBoundForOutgoingArgs(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet) override;
    bool IsUnwindable(PTR_VOID address) override;
    bool GetReturnAddressHijackInfo(MethodInfo* pMethodInfo, REGDISPLAY* pRegisterSet,
        PTR_PTR_VOID* ppRetAddrLocation, uintptr_t* pSpForArm64PacSign) override;
    PTR_VOID RemapHardwareFaultToGCSafePoint(MethodInfo* pMethodInfo, PTR_VOID controlPC) override;
    bool EHEnumInit(MethodInfo* pMethodInfo, PTR_VOID* pMethodStartAddress, EHEnumState* pEHEnumState) override;
    bool EHEnumNext(EHEnumState* pEHEnumState, EHClause* pEHClause) override;
    PTR_VOID GetMethodStartAddress(MethodInfo* pMethodInfo) override;
    PTR_VOID GetOsModuleHandle() override;
    void* GetClasslibFunction(ClasslibFunctionId functionId) override;
    PTR_VOID GetAssociatedData(PTR_VOID controlPC) override;
};
