// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: JITinterfaceGen.CPP
//

// ===========================================================================

// This contains generic C versions of some of the routines
// required by JITinterface.cpp. They are modeled after
// X86 specific routines found in JIThelp.asm or JITinterfaceX86.cpp
// More and more we're making AMD64 and IA64 specific versions of
// the helpers as well, JitInterfaceGen.cpp sticks around for rotor...


#include "common.h"
#include "clrtypes.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "comdelegate.h"
#include "field.h"
#include "ecall.h"

#ifdef _WIN64

// These are the fastest(?) versions of JIT helpers as they have the code to GetThread patched into them
// that does not make a call.
EXTERN_C Object* JIT_TrialAllocSFastMP_InlineGetThread(CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C Object* JIT_BoxFastMP_InlineGetThread (CORINFO_CLASS_HANDLE type, void* unboxedData);
EXTERN_C Object* AllocateStringFastMP_InlineGetThread (CLR_I4 cch);
EXTERN_C Object* JIT_NewArr1OBJ_MP_InlineGetThread (CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size);
EXTERN_C Object* JIT_NewArr1VC_MP_InlineGetThread (CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size);

// This next set is the fast version that invoke GetThread but is still faster than the VM implementation (i.e.
// the "slow" versions).
EXTERN_C Object* JIT_TrialAllocSFastMP(CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C Object* JIT_TrialAllocSFastSP(CORINFO_CLASS_HANDLE typeHnd_);
EXTERN_C Object* JIT_BoxFastMP (CORINFO_CLASS_HANDLE type, void* unboxedData);
EXTERN_C Object* JIT_BoxFastUP (CORINFO_CLASS_HANDLE type, void* unboxedData);
EXTERN_C Object* AllocateStringFastMP (CLR_I4 cch);
EXTERN_C Object* AllocateStringFastUP (CLR_I4 cch);

EXTERN_C Object* JIT_NewArr1OBJ_MP (CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size);
EXTERN_C Object* JIT_NewArr1OBJ_UP (CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size);
EXTERN_C Object* JIT_NewArr1VC_MP (CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size);
EXTERN_C Object* JIT_NewArr1VC_UP (CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size);

//For the optimized JIT_Mon helpers
#if defined(_TARGET_AMD64_)
EXTERN_C void JIT_MonEnterWorker_Slow(Object* obj, BYTE* pbLockTaken);
EXTERN_C void JIT_MonExitWorker_Slow(Object* obj, BYTE* pbLockTaken);
EXTERN_C void JIT_MonTryEnter_Slow(Object* obj, INT32 timeOut, BYTE* pbLockTaken);
EXTERN_C void JIT_MonEnterStatic_Slow(AwareLock* lock, BYTE* pbLockTaken);
EXTERN_C void JIT_MonExitStatic_Slow(AwareLock* lock, BYTE* pbLockTaken);
#endif // _TARGET_AMD64_

extern "C" void* JIT_GetSharedNonGCStaticBase_Slow(SIZE_T moduleDomainID, DWORD dwModuleClassID);
extern "C" void* JIT_GetSharedNonGCStaticBaseNoCtor_Slow(SIZE_T moduleDomainID, DWORD dwModuleClassID);
extern "C" void* JIT_GetSharedGCStaticBase_Slow(SIZE_T moduleDomainID, DWORD dwModuleClassID);
extern "C" void* JIT_GetSharedGCStaticBaseNoCtor_Slow(SIZE_T moduleDomainID, DWORD dwModuleClassID);

extern "C" void* JIT_GetSharedNonGCStaticBase_SingleAppDomain(SIZE_T moduleDomainID, DWORD dwModuleClassID);
extern "C" void* JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain(SIZE_T moduleDomainID, DWORD dwModuleClassID);
extern "C" void* JIT_GetSharedGCStaticBase_SingleAppDomain(SIZE_T moduleDomainID, DWORD dwModuleClassID);
extern "C" void* JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain(SIZE_T moduleDomainID, DWORD dwModuleClassID);

#ifdef _TARGET_AMD64_
extern WriteBarrierManager g_WriteBarrierManager;
#endif // _TARGET_AMD64_

#ifndef FEATURE_IMPLICIT_TLS
EXTERN_C DWORD gThreadTLSIndex;
EXTERN_C DWORD gAppDomainTLSIndex;
#endif
#endif // _WIN64

/*********************************************************************/ 
// Initialize the part of the JIT helpers that require very little of
// EE infrastructure to be in place.
/*********************************************************************/
#ifndef _TARGET_X86_

#if defined(_TARGET_AMD64_)

void MakeIntoJumpStub(LPVOID pStubAddress, LPVOID pTarget)
{
    BYTE* pbStubAddress = (BYTE*)pStubAddress;
    BYTE* pbTarget = (BYTE*)pTarget;

    DWORD dwOldProtect;
    if (!ClrVirtualProtect(pbStubAddress, 5, PAGE_EXECUTE_READWRITE, &dwOldProtect))
    {
        ThrowLastError();
    }

    DWORD diff = (DWORD)(pbTarget - (pbStubAddress + 5));

    // Make sure that the offset fits in 32-bits
    _ASSERTE( FitsInI4(pbTarget - (pbStubAddress + 5)) );
        
    // Write a jmp pcrel32 instruction
    //
    //      0xe9xxxxxxxx
    pbStubAddress[0] = 0xE9;
    *((DWORD*)&pbStubAddress[1]) = diff;

    ClrVirtualProtect(pbStubAddress, 5, dwOldProtect, &dwOldProtect);
}

EXTERN_C void JIT_TrialAllocSFastMP_InlineGetThread__PatchTLSOffset();
EXTERN_C void JIT_BoxFastMPIGT__PatchTLSLabel();
EXTERN_C void AllocateStringFastMP_InlineGetThread__PatchTLSOffset();
EXTERN_C void JIT_NewArr1VC_MP_InlineGetThread__PatchTLSOffset();
EXTERN_C void JIT_NewArr1OBJ_MP_InlineGetThread__PatchTLSOffset();
EXTERN_C void JIT_MonEnterWorker_InlineGetThread_GetThread_PatchLabel();
EXTERN_C void JIT_MonExitWorker_InlineGetThread_GetThread_PatchLabel();
EXTERN_C void JIT_MonTryEnter_GetThread_PatchLabel();
EXTERN_C void JIT_MonEnterStaticWorker_InlineGetThread_GetThread_PatchLabel_1();
EXTERN_C void JIT_MonEnterStaticWorker_InlineGetThread_GetThread_PatchLabel_2();
EXTERN_C void JIT_MonExitStaticWorker_InlineGetThread_GetThread_PatchLabel();


static const LPVOID InlineGetThreadLocations[] = {
    (PVOID)JIT_TrialAllocSFastMP_InlineGetThread__PatchTLSOffset,
    (PVOID)JIT_BoxFastMPIGT__PatchTLSLabel,
    (PVOID)AllocateStringFastMP_InlineGetThread__PatchTLSOffset,
    (PVOID)JIT_NewArr1VC_MP_InlineGetThread__PatchTLSOffset,
    (PVOID)JIT_NewArr1OBJ_MP_InlineGetThread__PatchTLSOffset,
    (PVOID)JIT_MonEnterWorker_InlineGetThread_GetThread_PatchLabel,
    (PVOID)JIT_MonExitWorker_InlineGetThread_GetThread_PatchLabel,
    (PVOID)JIT_MonTryEnter_GetThread_PatchLabel,
    (PVOID)JIT_MonEnterStaticWorker_InlineGetThread_GetThread_PatchLabel_1,
    (PVOID)JIT_MonEnterStaticWorker_InlineGetThread_GetThread_PatchLabel_2,
    (PVOID)JIT_MonExitStaticWorker_InlineGetThread_GetThread_PatchLabel,
};

EXTERN_C void JIT_GetSharedNonGCStaticBase__PatchTLSLabel();
EXTERN_C void JIT_GetSharedNonGCStaticBaseNoCtor__PatchTLSLabel();
EXTERN_C void JIT_GetSharedGCStaticBase__PatchTLSLabel();
EXTERN_C void JIT_GetSharedGCStaticBaseNoCtor__PatchTLSLabel();

static const LPVOID InlineGetAppDomainLocations[] = {
    (PVOID)JIT_GetSharedNonGCStaticBase__PatchTLSLabel,
    (PVOID)JIT_GetSharedNonGCStaticBaseNoCtor__PatchTLSLabel,
    (PVOID)JIT_GetSharedGCStaticBase__PatchTLSLabel,
    (PVOID)JIT_GetSharedGCStaticBaseNoCtor__PatchTLSLabel
};


#endif // defined(_TARGET_AMD64_)

#if defined(_WIN64) && !defined(FEATURE_IMPLICIT_TLS)
void FixupInlineGetters(DWORD tlsSlot, const LPVOID * pLocations, int nLocations)
{
    BYTE* pInlineGetter;
    DWORD dwOldProtect;
    for (int i=0; i<nLocations; i++)
    {
        pInlineGetter = (BYTE*)GetEEFuncEntryPoint((BYTE*)pLocations[i]);

        static const DWORD cbPatch = 9;
        if (!ClrVirtualProtect(pInlineGetter, cbPatch, PAGE_EXECUTE_READWRITE, &dwOldProtect))
        {
            ThrowLastError();
        }

        DWORD offset = (tlsSlot * sizeof(LPVOID) + offsetof(TEB, TlsSlots));

#if defined(_TARGET_AMD64_)
        // mov  r??, gs:[TLS offset]
        _ASSERTE_ALL_BUILDS("clr/src/VM/JITinterfaceGen.cpp",
                            pInlineGetter[0] == 0x65 &&
                            pInlineGetter[2] == 0x8B &&
                            pInlineGetter[4] == 0x25 &&
                            "Initialization failure while stomping instructions for the TLS slot offset: the instruction at the given offset did not match what we expect");

        *((DWORD*)(pInlineGetter + 5)) = offset;
#else // _TARGET_AMD64_
        PORTABILITY_ASSERT("FixupInlineGetters");
#endif //_TARGET_AMD64_

        FlushInstructionCache(GetCurrentProcess(), pInlineGetter, cbPatch);
        ClrVirtualProtect(pInlineGetter, cbPatch, dwOldProtect, &dwOldProtect);
    }
}
#endif // defined(_WIN64) && !defined(FEATURE_IMPLICIT_TLS)

#if defined(_TARGET_AMD64_)
EXTERN_C void JIT_MonEnterStaticWorker();
EXTERN_C void JIT_MonExitStaticWorker();
#endif

void InitJITHelpers1()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(g_SystemInfo.dwNumberOfProcessors != 0);

#if defined(_TARGET_AMD64_)

    g_WriteBarrierManager.Initialize();

#ifndef FEATURE_IMPLICIT_TLS
    if (gThreadTLSIndex < TLS_MINIMUM_AVAILABLE)
    {
        FixupInlineGetters(gThreadTLSIndex, InlineGetThreadLocations, COUNTOF(InlineGetThreadLocations));
    }

    if (gAppDomainTLSIndex < TLS_MINIMUM_AVAILABLE)
    {
        FixupInlineGetters(gAppDomainTLSIndex, InlineGetAppDomainLocations, COUNTOF(InlineGetAppDomainLocations));
    }
#endif // !FEATURE_IMPLICIT_TLS

    // Allocation helpers, faster but non-logging
    if (!((TrackAllocationsEnabled()) || 
        (LoggingOn(LF_GCALLOC, LL_INFO10))
#ifdef _DEBUG 
        || (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP) != 0)
#endif // _DEBUG
        ))
    {
        // if (multi-proc || server GC)
        if (GCHeapUtilities::UseThreadAllocationContexts())
        {
#ifdef FEATURE_IMPLICIT_TLS
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_NewS_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, JIT_NewS_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_MP_FastPortable);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_MP_FastPortable);

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateString_MP_FastPortable), ECall::FastAllocateString);
#else // !FEATURE_IMPLICIT_TLS
            // If the TLS for Thread is low enough use the super-fast helpers
            if (gThreadTLSIndex < TLS_MINIMUM_AVAILABLE)
            {
                SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_TrialAllocSFastMP_InlineGetThread);
                SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, JIT_TrialAllocSFastMP_InlineGetThread);
                SetJitHelperFunction(CORINFO_HELP_BOX, JIT_BoxFastMP_InlineGetThread);
                SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_MP_InlineGetThread);
                SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_MP_InlineGetThread);

                ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateStringFastMP_InlineGetThread), ECall::FastAllocateString);
            }
            else
            {
                SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_TrialAllocSFastMP);
                SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, JIT_TrialAllocSFastMP);
                SetJitHelperFunction(CORINFO_HELP_BOX, JIT_BoxFastMP);
                SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_MP);
                SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_MP);

                ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateStringFastMP), ECall::FastAllocateString);
            }
#endif // FEATURE_IMPLICIT_TLS
        }
        else
        {
#ifndef FEATURE_PAL
            // Replace the 1p slow allocation helpers with faster version
            //
            // When we're running Workstation GC on a single proc box we don't have 
            // InlineGetThread versions because there is no need to call GetThread
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST, JIT_TrialAllocSFastSP);
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, JIT_TrialAllocSFastSP);
            SetJitHelperFunction(CORINFO_HELP_BOX, JIT_BoxFastUP);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, JIT_NewArr1VC_UP);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, JIT_NewArr1OBJ_UP);

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(AllocateStringFastUP), ECall::FastAllocateString);
#endif // !FEATURE_PAL
        }
    }

#ifndef FEATURE_IMPLICIT_TLS
    if (gThreadTLSIndex >= TLS_MINIMUM_AVAILABLE)
    {
        // We need to patch the helpers for FCalls
        MakeIntoJumpStub(JIT_MonEnterWorker_InlineGetThread,        JIT_MonEnterWorker_Slow);
        MakeIntoJumpStub(JIT_MonExitWorker_InlineGetThread,         JIT_MonExitWorker_Slow);
        MakeIntoJumpStub(JIT_MonTryEnter_InlineGetThread,           JIT_MonTryEnter_Slow);

        SetJitHelperFunction(CORINFO_HELP_MON_ENTER,        JIT_MonEnterWorker_Slow);
        SetJitHelperFunction(CORINFO_HELP_MON_EXIT,         JIT_MonExitWorker_Slow);

        SetJitHelperFunction(CORINFO_HELP_MON_ENTER_STATIC, JIT_MonEnterStatic_Slow);
        SetJitHelperFunction(CORINFO_HELP_MON_EXIT_STATIC,  JIT_MonExitStatic_Slow);
    }
#endif

    if(IsSingleAppDomain())
    {
        SetJitHelperFunction(CORINFO_HELP_GETSHARED_GCSTATIC_BASE,          JIT_GetSharedGCStaticBase_SingleAppDomain);
        SetJitHelperFunction(CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE,       JIT_GetSharedNonGCStaticBase_SingleAppDomain);
        SetJitHelperFunction(CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR,   JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain);
        SetJitHelperFunction(CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR,JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain);
    }
#ifndef FEATURE_IMPLICIT_TLS
    else
    if (gAppDomainTLSIndex >= TLS_MINIMUM_AVAILABLE)
    {
        SetJitHelperFunction(CORINFO_HELP_GETSHARED_GCSTATIC_BASE,          JIT_GetSharedGCStaticBase_Slow);
        SetJitHelperFunction(CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE,       JIT_GetSharedNonGCStaticBase_Slow);
        SetJitHelperFunction(CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR,   JIT_GetSharedGCStaticBaseNoCtor_Slow);
        SetJitHelperFunction(CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR,JIT_GetSharedNonGCStaticBaseNoCtor_Slow);
    }
#endif // !FEATURE_IMPLICIT_TLS
#endif // _TARGET_AMD64_
}

#endif // !_TARGET_X86_
