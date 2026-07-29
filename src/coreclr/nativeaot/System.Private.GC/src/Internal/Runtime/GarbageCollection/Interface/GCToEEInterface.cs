// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Port of gcenv.ee.standalone.inl: every call the GC makes into the EE is forwarded to the
// singular IGCToCLR instance the EE handed to the GC at load time. The C# port sees that
// instance as a pointer whose first field is the vtable, so a call is a load of the slot from
// GCInterfaceVtables.IGCToCLRVtable followed by an indirect call with the instance as the
// first argument.

namespace Internal.Runtime.GarbageCollection
{
    internal static unsafe class GCToEEInterface
    {
        // The singular interface instance. All calls here are forwarded to it. Set once by
        // GCHeapUtilities/gcload before any other GC code runs; never null afterwards.
        private static void* g_theGCToCLR;

        private static IGCToCLRVtable* Vtable => *(IGCToCLRVtable**)g_theGCToCLR;

        /// <summary>
        /// Records the IGCToCLR instance the EE passed to the GC. Must be called before any other
        /// method on this class.
        /// </summary>
        public static void Initialize(void* theGCToCLR) => g_theGCToCLR = theGCToCLR;

        public static void SuspendEE(SUSPEND_REASON reason) => Vtable->SuspendEE(g_theGCToCLR, reason);

        public static void RestartEE(byte bFinishedGC) => Vtable->RestartEE(g_theGCToCLR, bFinishedGC);

        public static void GcScanRoots(delegate* unmanaged<byte**, ScanContext*, uint, void> fn, int condemned, int max_gen, ScanContext* sc) => Vtable->GcScanRoots(g_theGCToCLR, fn, condemned, max_gen, sc);

        public static void GcStartWork(int condemned, int max_gen) => Vtable->GcStartWork(g_theGCToCLR, condemned, max_gen);

        public static void BeforeGcScanRoots(int condemned, byte is_bgc, byte is_concurrent) => Vtable->BeforeGcScanRoots(g_theGCToCLR, condemned, is_bgc, is_concurrent);

        public static void AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc) => Vtable->AfterGcScanRoots(g_theGCToCLR, condemned, max_gen, sc);

        public static void GcDone(int condemned) => Vtable->GcDone(g_theGCToCLR, condemned);

        public static byte RefCountedHandleCallbacks(byte* pObject) => Vtable->RefCountedHandleCallbacks(g_theGCToCLR, pObject);

        public static void SyncBlockCacheWeakPtrScan(delegate* unmanaged<byte**, nuint*, nuint, nuint, void> scanProc, nuint lp1, nuint lp2) => Vtable->SyncBlockCacheWeakPtrScan(g_theGCToCLR, scanProc, lp1, lp2);

        public static void SyncBlockCacheDemote(int max_gen) => Vtable->SyncBlockCacheDemote(g_theGCToCLR, max_gen);

        public static void SyncBlockCachePromotionsGranted(int max_gen) => Vtable->SyncBlockCachePromotionsGranted(g_theGCToCLR, max_gen);

        public static uint GetActiveSyncBlockCount() => Vtable->GetActiveSyncBlockCount(g_theGCToCLR);

        public static byte IsPreemptiveGCDisabled() => Vtable->IsPreemptiveGCDisabled(g_theGCToCLR);

        public static byte EnablePreemptiveGC() => Vtable->EnablePreemptiveGC(g_theGCToCLR);

        public static void DisablePreemptiveGC() => Vtable->DisablePreemptiveGC(g_theGCToCLR);

        public static void* GetThread() => Vtable->GetThread(g_theGCToCLR);

        public static gc_alloc_context* GetAllocContext() => Vtable->GetAllocContext(g_theGCToCLR);

        public static void GcEnumAllocContexts(delegate* unmanaged<gc_alloc_context*, void*, void> fn, void* param) => Vtable->GcEnumAllocContexts(g_theGCToCLR, fn, param);

        public static byte* GetLoaderAllocatorObjectForGC(byte* pObject) => Vtable->GetLoaderAllocatorObjectForGC(g_theGCToCLR, pObject);

        public static byte CreateThread(delegate* unmanaged<void*, void> threadStart, void* arg, byte is_suspendable, byte* @name) => Vtable->CreateThread(g_theGCToCLR, threadStart, arg, is_suspendable, @name);

        public static void DiagGCStart(int gen, byte isInduced) => Vtable->DiagGCStart(g_theGCToCLR, gen, isInduced);

        public static void DiagUpdateGenerationBounds() => Vtable->DiagUpdateGenerationBounds(g_theGCToCLR);

        public static void DiagGCEnd(nuint index, int gen, int reason, byte fConcurrent) => Vtable->DiagGCEnd(g_theGCToCLR, index, gen, reason, fConcurrent);

        public static void DiagWalkFReachableObjects(void* gcContext) => Vtable->DiagWalkFReachableObjects(g_theGCToCLR, gcContext);

        public static void DiagWalkSurvivors(void* gcContext, byte fCompacting) => Vtable->DiagWalkSurvivors(g_theGCToCLR, gcContext, fCompacting);

        public static void DiagWalkUOHSurvivors(void* gcContext, int gen) => Vtable->DiagWalkUOHSurvivors(g_theGCToCLR, gcContext, gen);

        public static void DiagWalkBGCSurvivors(void* gcContext) => Vtable->DiagWalkBGCSurvivors(g_theGCToCLR, gcContext);

        public static void StompWriteBarrier(WriteBarrierParameters* args) => Vtable->StompWriteBarrier(g_theGCToCLR, args);

        public static void EnableFinalization(byte gcHasWorkForFinalizerThread) => Vtable->EnableFinalization(g_theGCToCLR, gcHasWorkForFinalizerThread);

        public static void HandleFatalError(uint exitCode) => Vtable->HandleFatalError(g_theGCToCLR, exitCode);

        public static byte EagerFinalized(byte* obj) => Vtable->EagerFinalized(g_theGCToCLR, obj);

        public static void* GetFreeObjectMethodTable() => Vtable->GetFreeObjectMethodTable(g_theGCToCLR);

        public static byte GetBooleanConfigValue(byte* privateKey, byte* publicKey, byte* value) => Vtable->GetBooleanConfigValue(g_theGCToCLR, privateKey, publicKey, value);

        public static byte GetIntConfigValue(byte* privateKey, byte* publicKey, long* value) => Vtable->GetIntConfigValue(g_theGCToCLR, privateKey, publicKey, value);

        public static byte GetStringConfigValue(byte* privateKey, byte* publicKey, byte** value) => Vtable->GetStringConfigValue(g_theGCToCLR, privateKey, publicKey, value);

        public static void FreeStringConfigValue(byte* value) => Vtable->FreeStringConfigValue(g_theGCToCLR, value);

        public static byte IsGCThread() => Vtable->IsGCThread(g_theGCToCLR);

        public static byte WasCurrentThreadCreatedByGC() => Vtable->WasCurrentThreadCreatedByGC(g_theGCToCLR);

        public static void WalkAsyncPinnedForPromotion(byte* @object, ScanContext* sc, delegate* unmanaged<byte**, ScanContext*, uint, void> callback) => Vtable->WalkAsyncPinnedForPromotion(g_theGCToCLR, @object, sc, callback);

        public static void WalkAsyncPinned(byte* @object, void* context, delegate* unmanaged<byte*, byte*, void*, void> callback) => Vtable->WalkAsyncPinned(g_theGCToCLR, @object, context, callback);

        public static void* EventSink() => Vtable->EventSink(g_theGCToCLR);

        public static uint GetTotalNumSizedRefHandles() => Vtable->GetTotalNumSizedRefHandles(g_theGCToCLR);

        public static byte AnalyzeSurvivorsRequested(int condemnedGeneration) => Vtable->AnalyzeSurvivorsRequested(g_theGCToCLR, condemnedGeneration);

        public static void AnalyzeSurvivorsFinished(nuint gcIndex, int condemnedGeneration, ulong promoted_bytes, delegate* unmanaged<void> reportGenerationBounds) => Vtable->AnalyzeSurvivorsFinished(g_theGCToCLR, gcIndex, condemnedGeneration, promoted_bytes, reportGenerationBounds);

        public static void VerifySyncTableEntry() => Vtable->VerifySyncTableEntry(g_theGCToCLR);

        public static void UpdateGCEventStatus(int publicLevel, int publicKeywords, int privateLEvel, int privateKeywords) => Vtable->UpdateGCEventStatus(g_theGCToCLR, publicLevel, publicKeywords, privateLEvel, privateKeywords);

        public static void LogStressMsg(uint level, uint facility, void* msg) => Vtable->LogStressMsg(g_theGCToCLR, level, facility, msg);

        public static uint GetCurrentProcessCpuCount() => Vtable->GetCurrentProcessCpuCount(g_theGCToCLR);

        public static void DiagAddNewRegion(int generation, byte* rangeStart, byte* rangeEnd, byte* rangeEndReserved) => Vtable->DiagAddNewRegion(g_theGCToCLR, generation, rangeStart, rangeEnd, rangeEndReserved);

        public static void LogErrorToHost(byte* message) => Vtable->LogErrorToHost(g_theGCToCLR, message);

        public static ulong GetThreadOSThreadId(void* thread) => Vtable->GetThreadOSThreadId(g_theGCToCLR, thread);

        public static void TriggerClientBridgeProcessing(MarkCrossReferencesArgs* args) => Vtable->TriggerClientBridgeProcessing(g_theGCToCLR, args);

    }
}
