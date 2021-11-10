// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCENV_EE_H_
#define _GCENV_EE_H_

#include "gcinterface.h"

#ifdef FEATURE_STANDALONE_GC

#if defined(__linux__)
extern "C" BOOL EventXplatEnabledGCStart(); // GCEventProvider_Default, GCEventLevel_Information, GCEventKeyword_GC
extern "C" BOOL EventXPlatEnabledGCJoin_V2(); //  GCEventProvider_Default, GCEventLevel_Verbose, GCEventKeyword_GC

extern "C" BOOL EventXplatEnabledGCGenerationRange(); // GCEventProvider_Default, GCEventLevel_Information, GCEventKeyword_GCHeapSurvivalAndMovement

extern "C" BOOL EventXplatEnabledSetGCHandle(); // GCEventProvider_Default, GCEventLevel_Information, GCEventKeyword_GCHandle
extern "C" BOOL EventXplatEnabledPrvSetGCHandle();; // GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCHandlePrivate

extern "C" BOOL EventXplatEnabledBGCBegin(); // GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate
extern "C" BOOL EventXplatEnabledPinPlugAtGCTime(); // GCEventProvider_Private, GCEventLevel_Verbose, GCEventKeyword_GC
#endif // __linux__

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
    void BeforeGcScanRoots(int condemned, bool is_bgc, bool is_concurrent);
    void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc);
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
    void DiagWalkSurvivors(void* gcContext, bool fCompacting);
    void DiagWalkUOHSurvivors(void* gcContext, int gen);
    void DiagWalkBGCSurvivors(void* gcContext);
    void StompWriteBarrier(WriteBarrierParameters* args);

    void EnableFinalization(bool foundFinalizers);
    void HandleFatalError(unsigned int exitCode);
    bool EagerFinalized(Object* obj);
    MethodTable* GetFreeObjectMethodTable();
    bool GetBooleanConfigValue(const char* privateKey, const char* publicKey, bool* value);
    bool GetIntConfigValue(const char* privateKey, const char* publicKey, int64_t* value);
    bool GetStringConfigValue(const char* privateKey, const char* publicKey, const char** value);
    void FreeStringConfigValue(const char* value);
    bool IsGCThread();
    bool WasCurrentThreadCreatedByGC();
    bool CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name);
    void WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback);
    void WalkAsyncPinned(Object* object, void* context, void(*callback)(Object*, Object*, void*));
    IGCToCLREventSink* EventSink();

    uint32_t GetTotalNumSizedRefHandles();

    bool AnalyzeSurvivorsRequested(int condemnedGeneration);
    void AnalyzeSurvivorsFinished(size_t gcIndex, int condemnedGeneration, uint64_t promoted_bytes, void (*reportGenerationBounds)());

    void VerifySyncTableEntry();

    void UpdateGCEventStatus(int publicLevel, int publicKeywords, int privateLevel, int privateKeywords);

    void LogStressMsg(unsigned level, unsigned facility, const StressLogMsg& msg);
    uint32_t GetCurrentProcessCpuCount();

    void DiagAddNewRegion(int generation, BYTE * rangeStart, BYTE * rangeEnd, BYTE * rangeEndReserved);
};

} // namespace standalone

#endif // FEATURE_STANDALONE_GC

#endif // _GCENV_EE_H_
