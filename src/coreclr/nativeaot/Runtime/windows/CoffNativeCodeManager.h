// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#if defined(TARGET_AMD64) || defined(TARGET_X86)
struct T_RUNTIME_FUNCTION {
    uint32_t BeginAddress;
    uint32_t EndAddress;
    uint32_t UnwindInfoAddress;
};
#elif defined(TARGET_ARM64)
struct T_RUNTIME_FUNCTION {
    uint32_t BeginAddress;
    union {
        uint32_t UnwindData;
        struct {
            uint32_t Flag : 2;
            uint32_t FunctionLength : 11;
            uint32_t RegF : 3;
            uint32_t RegI : 4;
            uint32_t H : 1;
            uint32_t CR : 2;
            uint32_t FrameSize : 9;
        } PackedUnwindData;
    };
};
#else
#error unexpected target architecture
#endif

typedef DPTR(T_RUNTIME_FUNCTION) PTR_RUNTIME_FUNCTION;

class CoffNativeCodeManager : public ICodeManager
{
    TADDR m_moduleBase;

    PTR_VOID m_pvManagedCodeStartRange;
    uint32_t m_cbManagedCodeRange;

    PTR_RUNTIME_FUNCTION m_pRuntimeFunctionTable;
    uint32_t m_nRuntimeFunctionTable;

    PTR_PTR_VOID m_pClasslibFunctions;
    uint32_t m_nClasslibFunctions;

public:
    CoffNativeCodeManager(TADDR moduleBase,
                          PTR_VOID pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                          PTR_RUNTIME_FUNCTION pRuntimeFunctionTable, uint32_t nRuntimeFunctionTable,
                          PTR_PTR_VOID pClasslibFunctions, uint32_t nClasslibFunctions);
    ~CoffNativeCodeManager();

    //
    // Code manager methods
    //

    bool FindMethodInfo(PTR_VOID        ControlPC,
                        MethodInfo *    pMethodInfoOut);

    bool IsFunclet(MethodInfo * pMethodInfo);

    bool IsFilter(MethodInfo * pMethodInfo);

    PTR_VOID GetFramePointer(MethodInfo *   pMethodInfo,
                             REGDISPLAY *   pRegisterSet);

    uint32_t GetCodeOffset(MethodInfo * pMethodInfo, PTR_VOID address, /*out*/ PTR_uint8_t* gcInfo);

    bool IsSafePoint(PTR_VOID pvAddress);

    void EnumGcRefs(MethodInfo *    pMethodInfo,
                    PTR_VOID        safePointAddress,
                    REGDISPLAY *    pRegisterSet,
                    GCEnumContext * hCallback,
                    bool            isActiveStackFrame);

    bool UnwindStackFrame(MethodInfo *    pMethodInfo,
                          uint32_t        flags,
                          REGDISPLAY *    pRegisterSet,                 // in/out
                          PInvokeTransitionFrame**      ppPreviousTransitionFrame);   // out

    uintptr_t GetConservativeUpperBoundForOutgoingArgs(MethodInfo *   pMethodInfo,
                                                        REGDISPLAY *   pRegisterSet);

    bool IsUnwindable(PTR_VOID pvAddress);

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
