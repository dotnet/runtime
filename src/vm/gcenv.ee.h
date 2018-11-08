// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _GCENV_EE_H_
#define _GCENV_EE_H_

#include "gcinterface.h"

#ifdef FEATURE_STANDALONE_GC

namespace standalone
{

class GCToEEInterface : public IGCToCLR {
public:
    GCToEEInterface() = default;
    ~GCToEEInterface() = default;

    void SuspendEE(SUSPEND_REASON reason);
    void RestartEE(bool bFinishedGC);
    void GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc);
    void GcStartWork(int condemned, int max_gen);
    void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc);
    void GcBeforeBGCSweepWork();
    void GcDone(int condemned);
    bool RefCountedHandleCallbacks(Object * pObject);
    void SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2);
    void SyncBlockCacheDemote(int max_gen);
    void SyncBlockCachePromotionsGranted(int max_gen);
    uint32_t GetActiveSyncBlockCount();
    bool IsPreemptiveGCDisabled();
    bool EnablePreemptiveGC();
    void DisablePreemptiveGC();
    Thread* GetThread();
    gc_alloc_context * GetAllocContext();
    void GcEnumAllocContexts(enum_alloc_context_func* fn, void* param);
    uint8_t* GetLoaderAllocatorObjectForGC(Object* pObject);

    // Diagnostics methods.
    void DiagGCStart(int gen, bool isInduced);
    void DiagUpdateGenerationBounds();
    void DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent);
    void DiagWalkFReachableObjects(void* gcContext);
    void DiagWalkSurvivors(void* gcContext);
    void DiagWalkLOHSurvivors(void* gcContext);
    void DiagWalkBGCSurvivors(void* gcContext);
    void StompWriteBarrier(WriteBarrierParameters* args);

    void EnableFinalization(bool foundFinalizers);
    void HandleFatalError(unsigned int exitCode);
    bool ShouldFinalizeObjectForUnload(void* pDomain, Object* obj);
    bool EagerFinalized(Object* obj);
    MethodTable* GetFreeObjectMethodTable();
    bool GetBooleanConfigValue(const char* key, bool* value);
    bool GetIntConfigValue(const char* key, int64_t* value);
    bool GetStringConfigValue(const char* key, const char** value);
    void FreeStringConfigValue(const char* value);
    bool IsGCThread();
    bool WasCurrentThreadCreatedByGC();
    bool CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name);
    void WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback);
    void WalkAsyncPinned(Object* object, void* context, void(*callback)(Object*, Object*, void*));
    IGCToCLREventSink* EventSink();

    uint32_t GetDefaultDomainIndex();
    void *GetAppDomainAtIndex(uint32_t appDomainIndex);
    bool AppDomainCanAccessHandleTable(uint32_t appDomainID);
    uint32_t GetIndexOfAppDomainBeingUnloaded();
    uint32_t GetTotalNumSizedRefHandles();
    bool AppDomainIsRudeUnload(void *appDomain);

    bool AnalyzeSurvivorsRequested(int condemnedGeneration);
    void AnalyzeSurvivorsFinished(int condemnedGeneration);
};

} // namespace standalone

#endif // FEATURE_STANDALONE_GC

#endif // _GCENV_EE_H_
