// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class UnixNativeCodeManager : public ICodeManager
{
    TADDR m_moduleBase;

    PTR_VOID m_pvManagedCodeStartRange;
    uint32_t m_cbManagedCodeRange;

    PTR_PTR_VOID m_pClasslibFunctions;
    uint32_t m_nClasslibFunctions;

public:
    UnixNativeCodeManager(TADDR moduleBase,
                          PTR_VOID pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                          PTR_PTR_VOID pClasslibFunctions, uint32_t nClasslibFunctions);

    virtual ~UnixNativeCodeManager();

    //
    // Code manager methods
    //

    bool FindMethodInfo(PTR_VOID        ControlPC,
                        MethodInfo *    pMethodInfoOut);

    bool IsFunclet(MethodInfo * pMethodInfo);

    bool IsFilter(MethodInfo * pMethodInfo);

    PTR_VOID GetFramePointer(MethodInfo *   pMethodInfo,
                             REGDISPLAY *   pRegisterSet);

    uint32_t GetCodeOffset(MethodInfo* pMethodInfo, PTR_VOID address, PTR_UInt8* gcInfo);

    bool IsSafePoint(PTR_VOID pvAddress);

    void EnumGcRefs(MethodInfo *    pMethodInfo,
                    PTR_VOID        safePointAddress,
                    REGDISPLAY *    pRegisterSet,
                    GCEnumContext * hCallback,
                    bool            isActiveStackFrame);

    bool UnwindStackFrame(MethodInfo *    pMethodInfo,
                          REGDISPLAY *    pRegisterSet,                 // in/out
                          PInvokeTransitionFrame**      ppPreviousTransitionFrame);   // out

    uintptr_t GetConservativeUpperBoundForOutgoingArgs(MethodInfo *   pMethodInfo,
                                                        REGDISPLAY *   pRegisterSet);

    bool IsUnwindable(PTR_VOID pvAddress);

    int TrailingEpilogueInstructionsCount(PTR_VOID pvAddress); 

    bool GetReturnAddressHijackInfo(MethodInfo *    pMethodInfo,
                                    REGDISPLAY *    pRegisterSet,       // in
                                    PTR_PTR_VOID *  ppvRetAddrLocation, // out
                                    GCRefKind *     pRetValueKind);     // out

    PTR_VOID RemapHardwareFaultToGCSafePoint(MethodInfo * pMethodInfo, PTR_VOID controlPC);

    bool EHEnumInit(MethodInfo * pMethodInfo, PTR_VOID * pMethodStartAddress, EHEnumState * pEHEnumState);

    bool EHEnumNext(EHEnumState * pEHEnumState, EHClause * pEHClause);

    PTR_VOID GetMethodStartAddress(MethodInfo * pMethodInfo);

    void * GetClasslibFunction(ClasslibFunctionId functionId);

    PTR_VOID GetAssociatedData(PTR_VOID ControlPC);

    PTR_VOID GetOsModuleHandle();
};
