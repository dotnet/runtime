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


// Patch Labels for the various write barriers

EXTERN_C void JIT_WriteBarrier_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardBundleTable();
#endif

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardBundleTable();
#endif
#endif // FEATURE_SVR_GC

EXTERN_C void JIT_WriteBarrier_Byte_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_Bit_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_CardBundleTable();
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardBundleTable();
#endif

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardBundleTable();
#endif
#endif // FEATURE_SVR_GC

EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardBundleTable();
#endif

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

extern WriteBarrierManager g_WriteBarrierManager;

// Use this somewhat hokey macro to concatenate the function start with the patch
// label. This allows the code below to look relatively nice, but relies on the
// naming convention which we have established for these helpers.
#define CALC_PATCH_LOCATION(func,label,offset)      CalculatePatchLocation((PVOID)func, (PVOID)func##_##label, offset)


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

    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#ifdef FEATURE_SVR_GC
    pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC

    PBYTE pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_RegionToGeneration, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pRegionToGenTableImmediate) & 0x7) == 0);

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_RegionToGeneration, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pRegionToGenTableImmediate) & 0x7) == 0);

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    PBYTE pWriteWatchTableImmediate;

    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_WriteWatchTable, 2);
    pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_Lower, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardTable, 2);

    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_WriteWatchTable, 2);
    pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Lower, 2);
    pUpperBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Upper, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardTable, 2);

    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#ifdef FEATURE_SVR_GC
    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_WriteWatchTable, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC

    pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_RegionToGeneration, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pRegionToGenTableImmediate) & 0x7) == 0);

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_RegionToGeneration, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pRegionToGenTableImmediate) & 0x7) == 0);

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

#endif // CODECOVERAGE


void WriteBarrierManager::UpdatePatchLocations()
{
    switch (newWriteBarrier)
    {
        case WRITE_BARRIER_PREGROW64:
        {
            m_pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_Lower, 2);
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

        case WRITE_BARRIER_POSTGROW64:
        {
            m_pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
        {
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_BYTE_REGIONS64:
            m_pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_RegionToGeneration, 2);
            m_pRegionShrDest             = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_RegionShrDest, 3);
            m_pRegionShrSrc              = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_RegionShrSrc, 3);
            m_pLowerBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_Upper, 2);
            m_pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pRegionToGenTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrDest);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrSrc);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;

        case WRITE_BARRIER_BIT_REGIONS64:
            m_pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_RegionToGeneration, 2);
            m_pRegionShrDest             = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_RegionShrDest, 3);
            m_pRegionShrSrc              = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_RegionShrSrc, 3);
            m_pLowerBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_Upper, 2);
            m_pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pRegionToGenTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrDest);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrSrc);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
        {
            m_pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_WriteWatchTable, 2);
            m_pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_Lower, 2);
            m_pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
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
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
        {
            m_pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_WriteWatchTable, 2);
            m_pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            m_pWriteWatchTableImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_WriteWatchTable, 2);
            m_pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_RegionToGeneration, 2);
            m_pRegionShrDest             = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_RegionShrDest, 3);
            m_pRegionShrSrc              = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_RegionShrSrc, 3);
            m_pLowerBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_Upper, 2);
            m_pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pRegionToGenTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrDest);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrSrc);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;

        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            m_pWriteWatchTableImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_WriteWatchTable, 2);
            m_pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_RegionToGeneration, 2);
            m_pRegionShrDest             = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_RegionShrDest, 3);
            m_pRegionShrSrc              = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_RegionShrSrc, 3);
            m_pLowerBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_Upper, 2);
            m_pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pRegionToGenTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrDest);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrSrc);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;


#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        default:
            UNREACHABLE_MSG("unexpected write barrier type!");
    }
}

#undef CALC_PATCH_LOCATION

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
