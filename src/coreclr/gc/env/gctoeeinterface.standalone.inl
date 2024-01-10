// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace standalone
{
    class GCToEEInterface : public IGCToCLR
    {
    public:
        GCToEEInterface() = default;
        ~GCToEEInterface() = default;

        void SuspendEE(SUSPEND_REASON reason)
        {
            ::GCToEEInterface::SuspendEE(reason);
        }

        void RestartEE(bool bFinishedGC)
        {
            ::GCToEEInterface::RestartEE(bFinishedGC);
        }

        void GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
        {
            ::GCToEEInterface::GcScanRoots(fn, condemned, max_gen, sc);
        }

        void GcStartWork(int condemned, int max_gen)
        {
            ::GCToEEInterface::GcStartWork(condemned, max_gen);
        }

        void BeforeGcScanRoots(int condemned, bool is_bgc, bool is_concurrent)
        {
            ::GCToEEInterface::BeforeGcScanRoots(condemned, is_bgc, is_concurrent);
        }

        void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc)
        {
            ::GCToEEInterface::AfterGcScanRoots(condemned, max_gen, sc);
        }

        void GcDone(int condemned)
        {
            ::GCToEEInterface::GcDone(condemned);
        }

        bool RefCountedHandleCallbacks(Object * pObject)
        {
            return ::GCToEEInterface::RefCountedHandleCallbacks(pObject);
        }

        void SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2)
        {
            ::GCToEEInterface::SyncBlockCacheWeakPtrScan(scanProc, lp1, lp2);
        }

        void SyncBlockCacheDemote(int max_gen)
        {
            ::GCToEEInterface::SyncBlockCacheDemote(max_gen);
        }

        void SyncBlockCachePromotionsGranted(int max_gen)
        {
            ::GCToEEInterface::SyncBlockCachePromotionsGranted(max_gen);
        }

        uint32_t GetActiveSyncBlockCount()
        {
            return ::GCToEEInterface::GetActiveSyncBlockCount();
        }

        bool IsPreemptiveGCDisabled()
        {
            return ::GCToEEInterface::IsPreemptiveGCDisabled();
        }

        bool EnablePreemptiveGC()
        {
            return ::GCToEEInterface::EnablePreemptiveGC();
        }

        void DisablePreemptiveGC()
        {
            ::GCToEEInterface::DisablePreemptiveGC();
        }

        Thread* GetThread()
        {
            return ::GCToEEInterface::GetThread();
        }

        gc_alloc_context * GetAllocContext()
        {
            return ::GCToEEInterface::GetAllocContext();
        }

        void GcEnumAllocContexts(enum_alloc_context_func* fn, void* param)
        {
            ::GCToEEInterface::GcEnumAllocContexts(fn, param);
        }

        uint8_t* GetLoaderAllocatorObjectForGC(Object* pObject)
        {
            return ::GCToEEInterface::GetLoaderAllocatorObjectForGC(pObject);
        }

        void DiagGCStart(int gen, bool isInduced)
        {
            ::GCToEEInterface::DiagGCStart(gen, isInduced);
        }

        void DiagUpdateGenerationBounds()
        {
            ::GCToEEInterface::DiagUpdateGenerationBounds();
        }

        void DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
        {
            ::GCToEEInterface::DiagGCEnd(index, gen, reason, fConcurrent);
        }

        void DiagWalkFReachableObjects(void* gcContext)
        {
            ::GCToEEInterface::DiagWalkFReachableObjects(gcContext);
        }

        void DiagWalkSurvivors(void* gcContext, bool fCompacting)
        {
            ::GCToEEInterface::DiagWalkSurvivors(gcContext, fCompacting);
        }

        void DiagWalkUOHSurvivors(void* gcContext, int gen)
        {
            ::GCToEEInterface::DiagWalkUOHSurvivors(gcContext, gen);
        }

        void DiagWalkBGCSurvivors(void* gcContext)
        {
            ::GCToEEInterface::DiagWalkBGCSurvivors(gcContext);
        }

        void StompWriteBarrier(WriteBarrierParameters* args)
        {
            ::GCToEEInterface::StompWriteBarrier(args);
        }

        void EnableFinalization(bool gcHasWorkForFinalizerThread)
        {
            ::GCToEEInterface::EnableFinalization(gcHasWorkForFinalizerThread);
        }

        void HandleFatalError(unsigned int exitCode)
        {
            ::GCToEEInterface::HandleFatalError(exitCode);
        }

        bool EagerFinalized(Object* obj)
        {
            return ::GCToEEInterface::EagerFinalized(obj);
        }

        MethodTable* GetFreeObjectMethodTable()
        {
            return ::GCToEEInterface::GetFreeObjectMethodTable();
        }

        bool GetBooleanConfigValue(const char* privateKey, const char* publicKey, bool* value)
        {
            return ::GCToEEInterface::GetBooleanConfigValue(privateKey, publicKey, value);
        }

        bool GetIntConfigValue(const char* privateKey, const char* publicKey, int64_t* value)
        {
            return ::GCToEEInterface::GetIntConfigValue(privateKey, publicKey, value);
        }

        bool GetStringConfigValue(const char* privateKey, const char* publicKey, const char** value)
        {
            return ::GCToEEInterface::GetStringConfigValue(privateKey, publicKey, value);
        }

        void FreeStringConfigValue(const char* value)
        {
            return ::GCToEEInterface::FreeStringConfigValue(value);
        }

        bool IsGCThread()
        {
            return ::GCToEEInterface::IsGCThread();
        }

        bool WasCurrentThreadCreatedByGC()
        {
            return ::GCToEEInterface::WasCurrentThreadCreatedByGC();
        }

        bool CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name)
        {
            return ::GCToEEInterface::CreateThread(threadStart, arg, is_suspendable, name);
        }

        void WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback)
        {
            ::GCToEEInterface::WalkAsyncPinnedForPromotion(object, sc, callback);
        }

        void WalkAsyncPinned(Object* object, void* context, void(*callback)(Object*, Object*, void*))
        {
            ::GCToEEInterface::WalkAsyncPinned(object, context, callback);
        }

        IGCToCLREventSink* EventSink()
        {
            return ::GCToEEInterface::EventSink();
        }

        uint32_t GetTotalNumSizedRefHandles()
        {
            return ::GCToEEInterface::GetTotalNumSizedRefHandles();
        }

        bool AnalyzeSurvivorsRequested(int condemnedGeneration)
        {
            return ::GCToEEInterface::AnalyzeSurvivorsRequested(condemnedGeneration);
        }

        void AnalyzeSurvivorsFinished(size_t gcIndex, int condemnedGeneration, uint64_t promoted_bytes, void (*reportGenerationBounds)())
        {
            ::GCToEEInterface::AnalyzeSurvivorsFinished(gcIndex, condemnedGeneration, promoted_bytes, reportGenerationBounds);
        }

        void VerifySyncTableEntry()
        {
            ::GCToEEInterface::VerifySyncTableEntry();
        }

        void UpdateGCEventStatus(int publicLevel, int publicKeywords, int privateLevel, int privateKeywords)
        {
            ::GCToEEInterface::UpdateGCEventStatus(publicLevel, publicKeywords, privateLevel, privateKeywords);
        }

        void LogStressMsg(unsigned level, unsigned facility, const StressLogMsg& msg)
        {
            ::GCToEEInterface::LogStressMsg(level, facility, msg);
        }

        uint32_t GetCurrentProcessCpuCount()
        {
            return ::GCToEEInterface::GetCurrentProcessCpuCount();
        }

        void DiagAddNewRegion(int generation, uint8_t* rangeStart, uint8_t* rangeEnd, uint8_t* rangeEndReserved)
        {
            ::GCToEEInterface::DiagAddNewRegion(generation, rangeStart, rangeEnd, rangeEndReserved);
        }

        void LogErrorToHost(const char *message)
        {
            ::GCToEEInterface::LogErrorToHost(message);
        }
    };
}
