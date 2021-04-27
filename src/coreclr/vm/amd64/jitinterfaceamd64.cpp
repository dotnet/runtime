// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===========================================================================
// File: JITinterfaceCpu.CPP
// ===========================================================================

// This contains JITinterface routines that are specific to the
// AMD64 platform. They are modeled after the X86 specific routines
// found in JITinterfaceX86.cpp or JIThelp.asm


#include "common.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "threadsuspend.h"

extern uint8_t* g_ephemeral_low;
extern uint8_t* g_ephemeral_high;
extern uint32_t* g_card_table;
extern uint32_t* g_card_bundle_table;

// Patch Labels for the various write barriers
EXTERN_C void JIT_WriteBarrier_End();

EXTERN_C void JIT_WriteBarrier_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardBundleTable();
#endif
EXTERN_C void JIT_WriteBarrier_PreGrow64_End();

EXTERN_C void JIT_WriteBarrier_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardBundleTable();
#endif
EXTERN_C void JIT_WriteBarrier_PostGrow64_End();

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardBundleTable();
#endif
EXTERN_C void JIT_WriteBarrier_SVR64_End();
#endif // FEATURE_SVR_GC

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardBundleTable();
#endif
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_End();

EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardBundleTable();
#endif
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_End();

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardBundleTable();
#endif
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_End();
#endif // FEATURE_SVR_GC
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

WriteBarrierManager g_WriteBarrierManager;

// Use this somewhat hokey macro to concatenate the function start with the patch
// label. This allows the code below to look relatively nice, but relies on the
// naming convention which we have established for these helpers.
#define CALC_PATCH_LOCATION(func,label,offset)      CalculatePatchLocation((PVOID)func, (PVOID)func##_##label, offset)

WriteBarrierManager::WriteBarrierManager() :
    m_currentWriteBarrier(WRITE_BARRIER_UNINITIALIZED)
{
    LIMITED_METHOD_CONTRACT;
}

#ifndef CODECOVERAGE        // Deactivate alignment validation for code coverage builds
                            // because the instrumentation tool will not preserve alignment
                            // constraints and we will fail.

void WriteBarrierManager::Validate()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    // we have an invariant that the addresses of all the values that we update in our write barrier
    // helpers must be naturally aligned, this is so that the update can happen atomically since there
    // are places where these values are updated while the EE is running
    // NOTE: we can't call this from the ctor since our infrastructure isn't ready for assert dialogs

    PBYTE pLowerBoundImmediate, pUpperBoundImmediate, pCardTableImmediate;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    PBYTE pCardBundleTableImmediate;
#endif

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_Lower, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardTable, 2);

    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#ifdef FEATURE_SVR_GC
    pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    PBYTE pWriteWatchTableImmediate;

    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_WriteWatchTable, 2);
    pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_Lower, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardTable, 2);

    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_WriteWatchTable, 2);
    pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Lower, 2);
    pUpperBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Upper, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardTable, 2);

    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#ifdef FEATURE_SVR_GC
    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_WriteWatchTable, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

#endif // CODECOVERAGE


PCODE WriteBarrierManager::GetCurrentWriteBarrierCode()
{
    LIMITED_METHOD_CONTRACT;

    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_PREGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_PreGrow64);
        case WRITE_BARRIER_POSTGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_SVR64);
#endif // FEATURE_SVR_GC
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_PreGrow64);
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_SVR64);
#endif // FEATURE_SVR_GC
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier!");
    };
}

size_t WriteBarrierManager::GetSpecificWriteBarrierSize(WriteBarrierType writeBarrier)
{
// marked asm functions are those which use the LEAF_END_MARKED macro to end them which
// creates a public Name_End label which can be used to figure out their size without
// having to create unwind info.
#define MARKED_FUNCTION_SIZE(pfn)    (size_t)((LPBYTE)GetEEFuncEntryPoint(pfn##_End) - (LPBYTE)GetEEFuncEntryPoint(pfn))

    switch (writeBarrier)
    {
        case WRITE_BARRIER_PREGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_PreGrow64);
        case WRITE_BARRIER_POSTGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_SVR64);
#endif // FEATURE_SVR_GC
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_PreGrow64);
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_SVR64);
#endif // FEATURE_SVR_GC
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_BUFFER:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier);
        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier!");
    };
#undef MARKED_FUNCTION_SIZE
}

size_t WriteBarrierManager::GetCurrentWriteBarrierSize()
{
    return GetSpecificWriteBarrierSize(m_currentWriteBarrier);
}

PBYTE WriteBarrierManager::CalculatePatchLocation(LPVOID base, LPVOID label, int offset)
{
    // the label should always come after the entrypoint for this funtion
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", (LPBYTE)label > (LPBYTE)base);

    return (GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier) + ((LPBYTE)GetEEFuncEntryPoint(label) - (LPBYTE)GetEEFuncEntryPoint(base) + offset));
}


int WriteBarrierManager::ChangeWriteBarrierTo(WriteBarrierType newWriteBarrier, bool isRuntimeSuspended)
{
    GCX_MAYBE_COOP_NO_THREAD_BROKEN((!isRuntimeSuspended && GetThreadNULLOk() != NULL));
    int stompWBCompleteActions = SWB_PASS;
    if (!isRuntimeSuspended && m_currentWriteBarrier != WRITE_BARRIER_UNINITIALIZED)
    {
        ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_GC_PREP);
        stompWBCompleteActions |= SWB_EE_RESTART;
    }

    _ASSERTE(m_currentWriteBarrier != newWriteBarrier);
    m_currentWriteBarrier = newWriteBarrier;

    // the memcpy must come before the switch statment because the asserts inside the switch
    // are actually looking into the JIT_WriteBarrier buffer
    memcpy(GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier), (LPVOID)GetCurrentWriteBarrierCode(), GetCurrentWriteBarrierSize());

    switch (newWriteBarrier)
    {
        case WRITE_BARRIER_PREGROW64:
        {
            m_pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_Lower, 2);
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

        case WRITE_BARRIER_POSTGROW64:
        {
            m_pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
        {
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }
#endif // FEATURE_SVR_GC

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
        {
            m_pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_WriteWatchTable, 2);
            m_pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_Lower, 2);
            m_pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
        {
            m_pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_WriteWatchTable, 2);
            m_pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Upper, 2);
            m_pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
        {
            m_pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_WriteWatchTable, 2);
            m_pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", 0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }
#endif // FEATURE_SVR_GC
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        default:
            UNREACHABLE_MSG("unexpected write barrier type!");
    }

    stompWBCompleteActions |= UpdateEphemeralBounds(true);
    stompWBCompleteActions |= UpdateWriteWatchAndCardTableLocations(true, false);

    return stompWBCompleteActions;
}

#undef CALC_PATCH_LOCATION

void WriteBarrierManager::Initialize()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;


    // Ensure that the generic JIT_WriteBarrier function buffer is large enough to hold any of the more specific
    // write barrier implementations.
    size_t cbWriteBarrierBuffer = GetSpecificWriteBarrierSize(WRITE_BARRIER_BUFFER);

    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_PREGROW64));
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_POSTGROW64));
#ifdef FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_SVR64));
#endif // FEATURE_SVR_GC
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_PREGROW64));
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_POSTGROW64));
#ifdef FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS("clr/src/VM/AMD64/JITinterfaceAMD64.cpp", cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_SVR64));
#endif // FEATURE_SVR_GC
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#if !defined(CODECOVERAGE)
    Validate();
#endif
}

bool WriteBarrierManager::NeedDifferentWriteBarrier(bool bReqUpperBoundsCheck, WriteBarrierType* pNewWriteBarrierType)
{
    // Init code for the JIT_WriteBarrier assembly routine.  Since it will be bashed everytime the GC Heap
    // changes size, we want to do most of the work just once.
    //
    // The actual JIT_WriteBarrier routine will only be called in free builds, but we keep this code (that
    // modifies it) around in debug builds to check that it works (with assertions).


    WriteBarrierType writeBarrierType = m_currentWriteBarrier;

    for(;;)
    {
        switch (writeBarrierType)
        {
        case WRITE_BARRIER_UNINITIALIZED:
#ifdef _DEBUG
            // Use the default slow write barrier some of the time in debug builds because of of contains some good asserts
            if ((g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK) || DbgRandomOnExe(0.5)) {
                break;
            }
#endif

            writeBarrierType = GCHeapUtilities::IsServerHeap() ? WRITE_BARRIER_SVR64 : WRITE_BARRIER_PREGROW64;
            continue;

        case WRITE_BARRIER_PREGROW64:
            if (bReqUpperBoundsCheck)
            {
                writeBarrierType = WRITE_BARRIER_POSTGROW64;
            }
            break;

        case WRITE_BARRIER_POSTGROW64:
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            break;
#endif // FEATURE_SVR_GC

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            if (bReqUpperBoundsCheck)
            {
                writeBarrierType = WRITE_BARRIER_WRITE_WATCH_POSTGROW64;
            }
            break;

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            break;
#endif // FEATURE_SVR_GC
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        default:
            UNREACHABLE_MSG("unexpected write barrier type!");
        }
        break;
    }

    *pNewWriteBarrierType = writeBarrierType;
    return m_currentWriteBarrier != writeBarrierType;
}

int WriteBarrierManager::UpdateEphemeralBounds(bool isRuntimeSuspended)
{
    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(false, &newType))
    {
        return ChangeWriteBarrierTo(newType, isRuntimeSuspended);
    }

    int stompWBCompleteActions = SWB_PASS;

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return stompWBCompleteActions;
#endif

    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_POSTGROW64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        {
            // Change immediate if different from new g_ephermeral_high.
            if (*(UINT64*)m_pUpperBoundImmediate != (size_t)g_ephemeral_high)
            {
                *(UINT64*)m_pUpperBoundImmediate = (size_t)g_ephemeral_high;
                stompWBCompleteActions |= SWB_ICACHE_FLUSH;
            }
        }
        FALLTHROUGH;
        case WRITE_BARRIER_PREGROW64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        {
            // Change immediate if different from new g_ephermeral_low.
            if (*(UINT64*)m_pLowerBoundImmediate != (size_t)g_ephemeral_low)
            {
                *(UINT64*)m_pLowerBoundImmediate = (size_t)g_ephemeral_low;
                stompWBCompleteActions |= SWB_ICACHE_FLUSH;
            }
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        {
            break;
        }
#endif // FEATURE_SVR_GC

        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier in UpdateEphemeralBounds");
    }

    return stompWBCompleteActions;
}

int WriteBarrierManager::UpdateWriteWatchAndCardTableLocations(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    // If we are told that we require an upper bounds check (GC did some heap reshuffling),
    // we need to switch to the WriteBarrier_PostGrow function for good.

    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(bReqUpperBoundsCheck, &newType))
    {
        return ChangeWriteBarrierTo(newType, isRuntimeSuspended);
    }

    int stompWBCompleteActions = SWB_PASS;

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return stompWBCompleteActions;
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
#endif // FEATURE_SVR_GC
            if (*(UINT64*)m_pWriteWatchTableImmediate != (size_t)g_sw_ww_table)
            {
                *(UINT64*)m_pWriteWatchTableImmediate = (size_t)g_sw_ww_table;
                stompWBCompleteActions |= SWB_ICACHE_FLUSH;
            }
            break;

        default:
            break; // clang seems to require all enum values to be covered for some reason
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    if (*(UINT64*)m_pCardTableImmediate != (size_t)g_card_table)
    {
        *(UINT64*)m_pCardTableImmediate = (size_t)g_card_table;
        stompWBCompleteActions |= SWB_ICACHE_FLUSH;
    }

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    if (*(UINT64*)m_pCardBundleTableImmediate != (size_t)g_card_bundle_table)
    {
        *(UINT64*)m_pCardBundleTableImmediate = (size_t)g_card_bundle_table;
        stompWBCompleteActions |= SWB_ICACHE_FLUSH;
    }
#endif

    return stompWBCompleteActions;
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
int WriteBarrierManager::SwitchToWriteWatchBarrier(bool isRuntimeSuspended)
{
    WriteBarrierType newWriteBarrierType;
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_UNINITIALIZED:
            // Using the debug-only write barrier
            return SWB_PASS;

        case WRITE_BARRIER_PREGROW64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_PREGROW64;
            break;

        case WRITE_BARRIER_POSTGROW64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_POSTGROW64;
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_SVR64;
            break;
#endif // FEATURE_SVR_GC

        default:
            UNREACHABLE();
    }

    return ChangeWriteBarrierTo(newWriteBarrierType, isRuntimeSuspended);
}

int WriteBarrierManager::SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended)
{
    WriteBarrierType newWriteBarrierType;
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_UNINITIALIZED:
            // Using the debug-only write barrier
            return SWB_PASS;

        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            newWriteBarrierType = WRITE_BARRIER_PREGROW64;
            break;

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            newWriteBarrierType = WRITE_BARRIER_POSTGROW64;
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            newWriteBarrierType = WRITE_BARRIER_SVR64;
            break;
#endif // FEATURE_SVR_GC

        default:
            UNREACHABLE();
    }

    return ChangeWriteBarrierTo(newWriteBarrierType, isRuntimeSuspended);
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

// This function bashes the super fast amd64 version of the JIT_WriteBarrier
// helper.  It should be called by the GC whenever the ephermeral region
// bounds get changed, but still remain on the top of the GC Heap.
int StompWriteBarrierEphemeral(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    return g_WriteBarrierManager.UpdateEphemeralBounds(isRuntimeSuspended);
}

// This function bashes the super fast amd64 versions of the JIT_WriteBarrier
// helpers.  It should be called by the GC whenever the ephermeral region gets moved
// from being at the top of the GC Heap, and/or when the cards table gets moved.
int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    WRAPPER_NO_CONTRACT;

    return g_WriteBarrierManager.UpdateWriteWatchAndCardTableLocations(isRuntimeSuspended, bReqUpperBoundsCheck);
}

void FlushWriteBarrierInstructionCache()
{
    FlushInstructionCache(GetCurrentProcess(), GetWriteBarrierCodeLocation((PVOID)JIT_WriteBarrier), g_WriteBarrierManager.GetCurrentWriteBarrierSize());
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
int SwitchToWriteWatchBarrier(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    return g_WriteBarrierManager.SwitchToWriteWatchBarrier(isRuntimeSuspended);
}

int SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    return g_WriteBarrierManager.SwitchToNonWriteWatchBarrier(isRuntimeSuspended);
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
