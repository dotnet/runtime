// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===========================================================================
// File: writebariermanager.cpp
// ===========================================================================

// This contains JITinterface routines for managing which write barrier function
// is currently in use, and patching all related constants.

#include "common.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "threadsuspend.h"
#include "writebarriermanager.h"
#if !defined(WRITE_BARRIER_VARS_INLINE)
#include "patchedcodeconstants.h"
#endif

extern uint8_t* g_ephemeral_low;
extern uint8_t* g_ephemeral_high;
extern uint32_t* g_card_table;
extern uint32_t* g_card_bundle_table;

// Patch Labels for the various write barriers

EXTERN_C void JIT_WriteBarrier_End();
EXTERN_C void JIT_WriteBarrier_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PreGrow64_End();
EXTERN_C void JIT_WriteBarrier_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PostGrow64_End();
#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_SVR64_End();
#endif // FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_Byte_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_Byte_Region64_End();
EXTERN_C void JIT_WriteBarrier_Bit_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_Bit_Region64_End();
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_End();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_End();
#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_End();
#endif // FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_End();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_End();
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


#if defined(WRITE_BARRIER_VARS_INLINE)

EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardBundleTable();
#endif

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardBundleTable();
#endif
#endif // FEATURE_SVR_GC

EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_CardBundleTable();
#endif

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
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardBundleTable();
#endif

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardBundleTable();
#endif
#endif // FEATURE_SVR_GC

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

#else // WRITE_BARRIER_VARS_INLINE

EXTERN_C void JIT_WriteBarrier_Table();
EXTERN_C void JIT_WriteBarrier_Table_End();
EXTERN_C void JIT_WriteBarrier_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Patch_Label_RegionShr();
EXTERN_C void JIT_WriteBarrier_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Patch_Label_CardBundleTable();
#endif
#if defined(TARGET_ARM64)
EXTERN_C void JIT_WriteBarrier_Patch_Label_LowestAddress();
EXTERN_C void JIT_WriteBarrier_Patch_Label_HighestAddress();
#if defined(WRITE_BARRIER_CHECK)
EXTERN_C void JIT_WriteBarrier_Patch_Label_GCShadow();
EXTERN_C void JIT_WriteBarrier_Patch_Label_GCShadowEnd();
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_ARM64
#endif // WRITE_BARRIER_VARS_INLINE

WriteBarrierManager g_WriteBarrierManager;

WriteBarrierManager::WriteBarrierManager() :
    m_currentWriteBarrier(WRITE_BARRIER_UNINITIALIZED)
{
    LIMITED_METHOD_CONTRACT;
}

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
        case WRITE_BARRIER_BYTE_REGIONS64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_Byte_Region64);
        case WRITE_BARRIER_BIT_REGIONS64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_Bit_Region64);
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_PreGrow64);
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_SVR64);
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_Byte_Region64);
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_Bit_Region64);
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
        case WRITE_BARRIER_BYTE_REGIONS64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_Byte_Region64);
        case WRITE_BARRIER_BIT_REGIONS64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_Bit_Region64);
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_PreGrow64);
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_SVR64);
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_Byte_Region64);
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_Bit_Region64);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_BUFFER:
#if defined(WRITE_BARRIER_VARS_INLINE)
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier);
#else
            return (size_t)((LPBYTE)GetEEFuncEntryPoint(JIT_WriteBarrier_Table_End) - (LPBYTE)GetEEFuncEntryPoint(JIT_WriteBarrier));
#endif
        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier!");
    };
#undef MARKED_FUNCTION_SIZE
}

size_t WriteBarrierManager::GetCurrentWriteBarrierSize()
{
    return GetSpecificWriteBarrierSize(m_currentWriteBarrier);
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

    // the memcpy must come before the switch statement because the asserts inside the switch
    // are actually looking into the JIT_WriteBarrier buffer
    {
        ExecutableWriterHolder<void> writeBarrierWriterHolder(GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier), GetCurrentWriteBarrierSize());
        memcpy(writeBarrierWriterHolder.GetRW(), (LPVOID)GetCurrentWriteBarrierCode(), GetCurrentWriteBarrierSize());
        stompWBCompleteActions |= SWB_ICACHE_FLUSH;
    }

#if defined(WRITE_BARRIER_VARS_INLINE)
    UpdatePatchLocations(newWriteBarrier);
#endif

    stompWBCompleteActions |= UpdateEphemeralBounds(true);
    stompWBCompleteActions |= UpdateWriteWatchAndCardTableLocations(true, false);

    return stompWBCompleteActions;
}

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

    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_PREGROW64));
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_POSTGROW64));
#ifdef FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_SVR64));
#endif // FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_BYTE_REGIONS64));
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_BIT_REGIONS64));
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_PREGROW64));
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_POSTGROW64));
#ifdef FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_SVR64));
#endif // FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64));
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64));
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


#if !defined(WRITE_BARRIER_VARS_INLINE)

    #define CALC_TABLE_LOCATION(var, offset) \
        assert(JIT_WriteBarrier_Offset_##offset == (PBYTE)JIT_WriteBarrier_Patch_Label_##offset - (PBYTE)JIT_WriteBarrier); \
        var = ((PBYTE)GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier) + JIT_WriteBarrier_Offset_##offset);

    CALC_TABLE_LOCATION(m_pWriteWatchTableImmediate, WriteWatchTable);
    CALC_TABLE_LOCATION(m_pRegionToGenTableImmediate, RegionToGeneration);
    CALC_TABLE_LOCATION(m_pRegionShrDest, RegionShr);
    CALC_TABLE_LOCATION(m_pLowerBoundImmediate, Lower);
    CALC_TABLE_LOCATION(m_pUpperBoundImmediate, Upper);
    CALC_TABLE_LOCATION(m_pCardTableImmediate, CardTable);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    CALC_TABLE_LOCATION(m_pCardBundleTableImmediate, CardBundleTable);
#endif

#if defined(TARGET_ARM64)
    CALC_TABLE_LOCATION(m_lowestAddress, LowestAddress);
    CALC_TABLE_LOCATION(m_highestAddress, HighestAddress);
#if defined(WRITE_BARRIER_CHECK)
    CALC_TABLE_LOCATION(m_pGCShadow, GCShadow);
    CALC_TABLE_LOCATION(m_pGCShadowEnd, GCShadowEnd);
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_AMD64

#endif // !WRITE_BARRIER_VARS_INLINE

#if !defined(CODECOVERAGE) && defined(WRITE_BARRIER_VARS_INLINE)
    Validate();
#endif
}

template <typename T> int updateVariable(PBYTE loc, T value)
{
    if (*(T*)loc != value)
    {
        ExecutableWriterHolder<T> varWriterHolder((T*)loc, sizeof(T));
        *varWriterHolder.GetRW() = value;
        return SWB_ICACHE_FLUSH;
    }
    return SWB_PASS;
}

bool WriteBarrierManager::NeedDifferentWriteBarrier(bool bReqUpperBoundsCheck, bool bUseBitwiseWriteBarrier, WriteBarrierType* pNewWriteBarrierType)
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
            // The default slow write barrier has some good asserts
            if ((g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK)) {
                break;
            }
#endif
            if (g_region_shr != 0)
            {
                writeBarrierType = bUseBitwiseWriteBarrier ? WRITE_BARRIER_BIT_REGIONS64: WRITE_BARRIER_BYTE_REGIONS64;
            }
            else
            {
#ifdef FEATURE_SVR_GC
                writeBarrierType = GCHeapUtilities::IsServerHeap() ? WRITE_BARRIER_SVR64 : WRITE_BARRIER_PREGROW64;
#else
                writeBarrierType = WRITE_BARRIER_PREGROW64;
#endif // FEATURE_SVR_GC
            }
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

        case WRITE_BARRIER_BYTE_REGIONS64:
        case WRITE_BARRIER_BIT_REGIONS64:
            break;

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
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            break;
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
    if (NeedDifferentWriteBarrier(false, g_region_use_bitwise_write_barrier, &newType))
    {
        return ChangeWriteBarrierTo(newType, isRuntimeSuspended);
    }

    int stompWBCompleteActions = SWB_PASS;

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return stompWBCompleteActions;
#endif

#if defined(WRITE_BARRIER_VARS_INLINE)

    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_POSTGROW64:
        case WRITE_BARRIER_BYTE_REGIONS64:
        case WRITE_BARRIER_BIT_REGIONS64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
            stompWBCompleteActions |= updateVariable<UINT64>(m_pUpperBoundImmediate, (size_t)g_ephemeral_high);
        FALLTHROUGH;

        case WRITE_BARRIER_PREGROW64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
            stompWBCompleteActions |= updateVariable<UINT64>(m_pLowerBoundImmediate, (size_t)g_ephemeral_low);
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
            break;
#endif // FEATURE_SVR_GC

        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier in UpdateEphemeralBounds");
    }

#else

    stompWBCompleteActions |= updateVariable<UINT64>(m_pUpperBoundImmediate, (size_t)g_ephemeral_high);
    stompWBCompleteActions |= updateVariable<UINT64>(m_pLowerBoundImmediate, (size_t)g_ephemeral_low);
#endif //WRITE_BARRIER_VARS_INLINE


#if defined(TARGET_ARM64)
    stompWBCompleteActions |= updateVariable<UINT64>(m_lowestAddress, (size_t)g_lowest_address);
    stompWBCompleteActions |= updateVariable<UINT64>(m_highestAddress, (size_t)g_highest_address);
#if defined(WRITE_BARRIER_CHECK)
    stompWBCompleteActions |= updateVariable<UINT64>(m_pGCShadow, (size_t)g_GCShadow);
    stompWBCompleteActions |= updateVariable<UINT64>(m_pGCShadowEnd, (size_t)g_GCShadowEnd);
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_AMD64

    return stompWBCompleteActions;
}

int WriteBarrierManager::UpdateWriteWatchAndCardTableLocations(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    // If we are told that we require an upper bounds check (GC did some heap reshuffling),
    // we need to switch to the WriteBarrier_PostGrow function for good.

    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(bReqUpperBoundsCheck, g_region_use_bitwise_write_barrier, &newType))
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
#if defined(WRITE_BARRIER_VARS_INLINE)
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            stompWBCompleteActions |= updateVariable<UINT64>(m_pWriteWatchTableImmediate, (size_t)g_write_watch_table);
            break;

        default:
            break;
    }
#else
    stompWBCompleteActions |= updateVariable<UINT64>(m_pWriteWatchTableImmediate, (size_t)g_write_watch_table);
#endif
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


#if defined(WRITE_BARRIER_VARS_INLINE)
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_BYTE_REGIONS64:
        case WRITE_BARRIER_BIT_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            stompWBCompleteActions |= updateVariable<UINT64>(m_pRegionToGenTableImmediate, (size_t)g_region_to_generation_table);
            stompWBCompleteActions |= updateVariable<UINT8>(m_pRegionShrDest, (size_t)g_region_shr);
            stompWBCompleteActions |= updateVariable<UINT8>(m_pRegionShrSrc, (size_t)g_region_shr);
            break;

        default:
            break;
    }
#else
    stompWBCompleteActions |= updateVariable<UINT64>(m_pRegionToGenTableImmediate, (size_t)g_region_to_generation_table);
    stompWBCompleteActions |= updateVariable<UINT8>(m_pRegionShrDest, g_region_shr);
#endif //WRITE_BARRIER_VARS_INLINE

    stompWBCompleteActions |= updateVariable<UINT64>(m_pCardTableImmediate, (size_t)g_card_table);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    stompWBCompleteActions |= updateVariable<UINT64>(m_pCardBundleTableImmediate, (size_t)g_card_bundle_table);
#endif

#if defined(TARGET_ARM64)
    stompWBCompleteActions |= updateVariable<UINT64>(m_lowestAddress, (size_t)g_lowest_address);
    stompWBCompleteActions |= updateVariable<UINT64>(m_highestAddress, (size_t)g_highest_address);
#if defined(WRITE_BARRIER_CHECK)
    stompWBCompleteActions |= updateVariable<UINT64>(m_pGCShadow, (size_t)g_GCShadow);
    stompWBCompleteActions |= updateVariable<UINT64>(m_pGCShadowEnd, (size_t)g_GCShadowEnd);
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_AMD64

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

        case WRITE_BARRIER_BYTE_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64;
            break;

        case WRITE_BARRIER_BIT_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64;
            break;

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

        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_BYTE_REGIONS64;
            break;

        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_BIT_REGIONS64;
            break;

        default:
            UNREACHABLE();
    }

    return ChangeWriteBarrierTo(newWriteBarrierType, isRuntimeSuspended);
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


#if defined(WRITE_BARRIER_VARS_INLINE)


// Use this somewhat hokey macro to concatenate the function start with the patch
// label. This allows the code below to look relatively nice, but relies on the
// naming convention which we have established for these helpers.
#define CALC_PATCH_LOCATION(func,label,offset)      CalculatePatchLocation((PVOID)func, (PVOID)func##_##label, offset)

PBYTE WriteBarrierManager::CalculatePatchLocation(LPVOID base, LPVOID label, int inlineOffset)
{
    // the label should always come after or at the entrypoint for this funtion
    _ASSERTE_ALL_BUILDS((LPBYTE)label >= (LPBYTE)base);

    BYTE* patchBase = GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier);
    return (patchBase + ((LPBYTE)GetEEFuncEntryPoint(label) - (LPBYTE)GetEEFuncEntryPoint(base))) + inlineOffset;
}

// Deactivate alignment validation for code coverage builds
// because the instrumentation tool will not preserve alignment
// constraints and we will fail.
#if !defined(CODECOVERAGE)

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

void WriteBarrierManager::UpdatePatchLocations(WriteBarrierType newWriteBarrier)
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

#endif // WRITE_BARRIER_VARS_INLINE


// This function bashes the super fast version of the JIT_WriteBarrier
// helper.  It should be called by the GC whenever the ephermeral region
// bounds get changed, but still remain on the top of the GC Heap.
int StompWriteBarrierEphemeral(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    if (IsWriteBarrierCopyEnabled())
        return g_WriteBarrierManager.UpdateEphemeralBounds(isRuntimeSuspended);
    else
        return SWB_PASS;
}

// This function bashes the super fast versions of the JIT_WriteBarrier
// helpers.  It should be called by the GC whenever the ephermeral region gets moved
// from being at the top of the GC Heap, and/or when the cards table gets moved.
int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    WRAPPER_NO_CONTRACT;

    if (IsWriteBarrierCopyEnabled())
        return g_WriteBarrierManager.UpdateWriteWatchAndCardTableLocations(isRuntimeSuspended, bReqUpperBoundsCheck);
    else
        return SWB_PASS;
}

void FlushWriteBarrierInstructionCache()
{
    if (IsWriteBarrierCopyEnabled())
        FlushInstructionCache(GetCurrentProcess(), GetWriteBarrierCodeLocation((PVOID)JIT_WriteBarrier), g_WriteBarrierManager.GetCurrentWriteBarrierSize());
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

int SwitchToWriteWatchBarrier(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    if (IsWriteBarrierCopyEnabled())
        return g_WriteBarrierManager.SwitchToWriteWatchBarrier(isRuntimeSuspended);
    else
        return SWB_PASS;
}

int SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    if (IsWriteBarrierCopyEnabled())
        return g_WriteBarrierManager.SwitchToNonWriteWatchBarrier(isRuntimeSuspended);
    else
        return SWB_PASS;
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

void InitJITWriteBarrierHelpers()
{
    STANDARD_VM_CONTRACT;

    g_WriteBarrierManager.Initialize();
}
