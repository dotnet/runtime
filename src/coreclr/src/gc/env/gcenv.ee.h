// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Interface between the GC and EE
//

#ifndef __GCENV_EE_H__
#define __GCENV_EE_H__

#include "gcinterface.h"

class GCToEEInterface
{
public:
    static void SuspendEE(SUSPEND_REASON reason);
    static void RestartEE(bool bFinishedGC); //resume threads.

    // 
    // The GC roots enumeration callback
    //
    static void GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc);

    // 
    // Callbacks issues during GC that the execution engine can do its own bookeeping
    //

    // start of GC call back - single threaded
    static void GcStartWork(int condemned, int max_gen); 

    //EE can perform post stack scanning action, while the 
    // user threads are still suspended 
    static void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc);

    // Called before BGC starts sweeping, the heap is walkable
    static void GcBeforeBGCSweepWork();

    // post-gc callback.
    static void GcDone(int condemned);

    // Promote refcounted handle callback
    static bool RefCountedHandleCallbacks(Object * pObject);

    // Sync block cache management
    static void SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2);
    static void SyncBlockCacheDemote(int max_gen);
    static void SyncBlockCachePromotionsGranted(int max_gen);
    static uint32_t GetActiveSyncBlockCount();

    // Thread functions
    static bool IsPreemptiveGCDisabled();
    static bool EnablePreemptiveGC();
    static void DisablePreemptiveGC();
    static Thread* GetThread();

    static gc_alloc_context * GetAllocContext();

    static void GcEnumAllocContexts(enum_alloc_context_func* fn, void* param);

    static uint8_t* GetLoaderAllocatorObjectForGC(Object* pObject);

    // Diagnostics methods.
    static void DiagGCStart(int gen, bool isInduced);
    static void DiagUpdateGenerationBounds();
    static void DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent);
    static void DiagWalkFReachableObjects(void* gcContext);
    static void DiagWalkSurvivors(void* gcContext, bool fCompacting);
    static void DiagWalkLOHSurvivors(void* gcContext);
    static void DiagWalkBGCSurvivors(void* gcContext);
    static void StompWriteBarrier(WriteBarrierParameters* args);

    static void EnableFinalization(bool foundFinalizers);

    static void HandleFatalError(unsigned int exitCode);
    static bool EagerFinalized(Object* obj);
    static MethodTable* GetFreeObjectMethodTable();
    static bool GetBooleanConfigValue(const char* key, bool* value);
    static bool GetIntConfigValue(const char* key, int64_t* value);
    static bool GetStringConfigValue(const char* key, const char** value);
    static void FreeStringConfigValue(const char* key);
    static bool IsGCThread();
    static bool WasCurrentThreadCreatedByGC();
    static bool CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name);
    static void WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback);
    static void WalkAsyncPinned(Object* object, void* context, void(*callback)(Object*, Object*, void*));
    static IGCToCLREventSink* EventSink();

    static uint32_t GetTotalNumSizedRefHandles();

    static bool AnalyzeSurvivorsRequested(int condemnedGeneration);
    static void AnalyzeSurvivorsFinished(int condemnedGeneration);

    static void VerifySyncTableEntry();
    static void UpdateGCEventStatus(int publicLevel, int publicKeywords, int privateLevel, int privateKeywords);
};

#endif // __GCENV_EE_H__
