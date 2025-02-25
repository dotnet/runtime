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
