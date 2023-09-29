// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "gceventstatus.h"
#include "holder.h"
#include "RhConfig.h"

// This is the global GC heap, maintained by the VM.
GPTR_IMPL(IGCHeap, g_pGCHeap);

// These globals are variables used within the GC and maintained
// by the EE for use in write barriers. It is the responsibility
// of the GC to communicate updates to these globals to the EE through
// GCToEEInterface::StompWriteBarrier.
GPTR_IMPL_INIT(uint32_t, g_card_table,      nullptr);
GPTR_IMPL_INIT(uint8_t,  g_lowest_address,  nullptr);
GPTR_IMPL_INIT(uint8_t,  g_highest_address, nullptr);
GVAL_IMPL_INIT(GCHeapType, g_heap_type,     GC_HEAP_INVALID);
uint8_t* g_ephemeral_low  = (uint8_t*)1;
uint8_t* g_ephemeral_high = (uint8_t*)~0;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
uint32_t* g_card_bundle_table = nullptr;
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
uint8_t* g_write_watch_table = nullptr;
bool g_sw_ww_enabled_for_gc_heap = false;
#endif

IGCHandleManager* g_pGCHandleManager = nullptr;

GcDacVars g_gc_dac_vars;
GPTR_IMPL(GcDacVars, g_gcDacGlobals);

// GC entrypoints for the linked-in GC. These symbols are invoked
// directly if we are not using a standalone GC.
extern "C" HRESULT GC_Initialize(
    /* In  */ IGCToCLR* clrToGC,
    /* Out */ IGCHeap** gcHeap,
    /* Out */ IGCHandleManager** gcHandleManager,
    /* Out */ GcDacVars* gcDacVars
);

#ifndef DACCESS_COMPILE

enum GC_LOAD_STATUS {
    GC_LOAD_STATUS_BEFORE_START,
    GC_LOAD_STATUS_START,
    GC_LOAD_STATUS_DONE_LOAD,
    GC_LOAD_STATUS_GET_VERSIONINFO,
    GC_LOAD_STATUS_CALL_VERSIONINFO,
    GC_LOAD_STATUS_DONE_VERSION_CHECK,
    GC_LOAD_STATUS_GET_INITIALIZE,
    GC_LOAD_STATUS_LOAD_COMPLETE
};

// Load status of the GC. If GC loading fails, the value of this
// global indicates where the failure occurred.
GC_LOAD_STATUS g_gc_load_status = GC_LOAD_STATUS_BEFORE_START;

// The version of the GC that we have loaded.
VersionInfo g_gc_version_info;

class MyGCToEEInterface : public IGCToCLR {
public:
    MyGCToEEInterface() = default;
    ~MyGCToEEInterface() = default;

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

    void EnableFinalization(bool gcHasWorkForFinalizerThread);
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

    void LogErrorToHost(const char *message);
};

void MyGCToEEInterface::SuspendEE(SUSPEND_REASON reason)
{
    GCToEEInterface::SuspendEE(reason);
}

void MyGCToEEInterface::RestartEE(bool bFinishedGC)
{
    GCToEEInterface::RestartEE(bFinishedGC);
}

void MyGCToEEInterface::GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    GCToEEInterface::GcScanRoots(fn, condemned, max_gen, sc);
}

void MyGCToEEInterface::GcStartWork(int condemned, int max_gen)
{
    GCToEEInterface::GcStartWork(condemned, max_gen);
}

void MyGCToEEInterface::BeforeGcScanRoots(int condemned, bool is_bgc, bool is_concurrent)
{
    GCToEEInterface::BeforeGcScanRoots(condemned, is_bgc, is_concurrent);
}

void MyGCToEEInterface::AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc)
{
    GCToEEInterface::AfterGcScanRoots(condemned, max_gen, sc);
}

void MyGCToEEInterface::GcDone(int condemned)
{
    GCToEEInterface::GcDone(condemned);
}

bool MyGCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
    return GCToEEInterface::RefCountedHandleCallbacks(pObject);
}

void MyGCToEEInterface::SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2)
{
    GCToEEInterface::SyncBlockCacheWeakPtrScan(scanProc, lp1, lp2);
}

void MyGCToEEInterface::SyncBlockCacheDemote(int max_gen)
{
    GCToEEInterface::SyncBlockCacheDemote(max_gen);
}

void MyGCToEEInterface::SyncBlockCachePromotionsGranted(int max_gen)
{
    GCToEEInterface::SyncBlockCachePromotionsGranted(max_gen);
}

uint32_t MyGCToEEInterface::GetActiveSyncBlockCount()
{
    return GCToEEInterface::GetActiveSyncBlockCount();
}

bool MyGCToEEInterface::IsPreemptiveGCDisabled()
{
    return GCToEEInterface::IsPreemptiveGCDisabled();
}

bool MyGCToEEInterface::EnablePreemptiveGC()
{
    return GCToEEInterface::EnablePreemptiveGC();
}

void MyGCToEEInterface::DisablePreemptiveGC()
{
    GCToEEInterface::DisablePreemptiveGC();
}

Thread* MyGCToEEInterface::GetThread()
{
    return GCToEEInterface::GetThread();
}

gc_alloc_context * MyGCToEEInterface::GetAllocContext()
{
    return GCToEEInterface::GetAllocContext();
}

void MyGCToEEInterface::GcEnumAllocContexts(enum_alloc_context_func* fn, void* param)
{
    GCToEEInterface::GcEnumAllocContexts(fn, param);
}

uint8_t* MyGCToEEInterface::GetLoaderAllocatorObjectForGC(Object* pObject)
{
    return GCToEEInterface::GetLoaderAllocatorObjectForGC(pObject);
}

void MyGCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
    GCToEEInterface::DiagGCStart(gen, isInduced);
}

void MyGCToEEInterface::DiagUpdateGenerationBounds()
{
    GCToEEInterface::DiagUpdateGenerationBounds();
}

void MyGCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
    GCToEEInterface::DiagGCEnd(index, gen, reason, fConcurrent);
}

void MyGCToEEInterface::DiagWalkFReachableObjects(void* gcContext)
{
    GCToEEInterface::DiagWalkFReachableObjects(gcContext);
}

void MyGCToEEInterface::DiagWalkSurvivors(void* gcContext, bool fCompacting)
{
    GCToEEInterface::DiagWalkSurvivors(gcContext, fCompacting);
}

void MyGCToEEInterface::DiagWalkUOHSurvivors(void* gcContext, int gen)
{
    GCToEEInterface::DiagWalkUOHSurvivors(gcContext, gen);
}

void MyGCToEEInterface::DiagWalkBGCSurvivors(void* gcContext)
{
    GCToEEInterface::DiagWalkBGCSurvivors(gcContext);
}

void MyGCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
    GCToEEInterface::StompWriteBarrier(args);
}

void MyGCToEEInterface::EnableFinalization(bool gcHasWorkForFinalizerThread)
{
    GCToEEInterface::EnableFinalization(gcHasWorkForFinalizerThread);
}

void MyGCToEEInterface::HandleFatalError(unsigned int exitCode)
{
    GCToEEInterface::HandleFatalError(exitCode);
}

bool MyGCToEEInterface::EagerFinalized(Object* obj)
{
    return GCToEEInterface::EagerFinalized(obj);
}

MethodTable* MyGCToEEInterface::GetFreeObjectMethodTable()
{
    return GCToEEInterface::GetFreeObjectMethodTable();
}

bool MyGCToEEInterface::GetBooleanConfigValue(const char* privateKey, const char* publicKey, bool* value)
{
    return GCToEEInterface::GetBooleanConfigValue(privateKey, publicKey, value);
}

bool MyGCToEEInterface::GetIntConfigValue(const char* privateKey, const char* publicKey, int64_t* value)
{
    return GCToEEInterface::GetIntConfigValue(privateKey, publicKey, value);
}

bool MyGCToEEInterface::GetStringConfigValue(const char* privateKey, const char* publicKey, const char** value)
{
    return GCToEEInterface::GetStringConfigValue(privateKey, publicKey, value);
}

void MyGCToEEInterface::FreeStringConfigValue(const char* value)
{
    return GCToEEInterface::FreeStringConfigValue(value);
}

bool MyGCToEEInterface::IsGCThread()
{
    return GCToEEInterface::IsGCThread();
}

bool MyGCToEEInterface::WasCurrentThreadCreatedByGC()
{
    return GCToEEInterface::WasCurrentThreadCreatedByGC();
}

bool MyGCToEEInterface::CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name)
{
    return GCToEEInterface::CreateThread(threadStart, arg, is_suspendable, name);
}

void MyGCToEEInterface::WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback)
{
    GCToEEInterface::WalkAsyncPinnedForPromotion(object, sc, callback);
}

void MyGCToEEInterface::WalkAsyncPinned(Object* object, void* context, void(*callback)(Object*, Object*, void*))
{
    GCToEEInterface::WalkAsyncPinned(object, context, callback);
}

IGCToCLREventSink* MyGCToEEInterface::EventSink()
{
    return GCToEEInterface::EventSink();
}

uint32_t MyGCToEEInterface::GetTotalNumSizedRefHandles()
{
    return GCToEEInterface::GetTotalNumSizedRefHandles();
}

bool MyGCToEEInterface::AnalyzeSurvivorsRequested(int condemnedGeneration)
{
    return GCToEEInterface::AnalyzeSurvivorsRequested(condemnedGeneration);
}

void MyGCToEEInterface::AnalyzeSurvivorsFinished(size_t gcIndex, int condemnedGeneration, uint64_t promoted_bytes, void (*reportGenerationBounds)())
{
    GCToEEInterface::AnalyzeSurvivorsFinished(gcIndex, condemnedGeneration, promoted_bytes, reportGenerationBounds);
}

void MyGCToEEInterface::VerifySyncTableEntry()
{
    GCToEEInterface::VerifySyncTableEntry();
}

void MyGCToEEInterface::UpdateGCEventStatus(int publicLevel, int publicKeywords, int privateLevel, int privateKeywords)
{
    GCToEEInterface::UpdateGCEventStatus(publicLevel, publicKeywords, privateLevel, privateKeywords);
}

void MyGCToEEInterface::LogStressMsg(unsigned level, unsigned facility, const StressLogMsg& msg)
{
    // GCToEEInterface::LogStressMsg(level, facility, msg);
}

uint32_t MyGCToEEInterface::GetCurrentProcessCpuCount()
{
    return GCToEEInterface::GetCurrentProcessCpuCount();
}

void MyGCToEEInterface::DiagAddNewRegion(int generation, BYTE * rangeStart, BYTE * rangeEnd, BYTE * rangeEndReserved)
{
    GCToEEInterface::DiagAddNewRegion(generation, rangeStart, rangeEnd, rangeEndReserved);
}

void MyGCToEEInterface::LogErrorToHost(const char *message)
{
    GCToEEInterface::LogErrorToHost(message);
}

HRESULT InitializeGCSelector();

HRESULT GCHeapUtilities::InitializeGC()
{
    return InitializeGCSelector();
}

HRESULT InitializeDefaultGC()
{
    return GCHeapUtilities::InitializeDefaultGC();
}

HRESULT InitializeStandaloneGC()
{
    return GCHeapUtilities::InitializeStandaloneGC();
}

HRESULT GCHeapUtilities::InitializeStandaloneGC()
{
    char* moduleName;

    if (!RhConfig::Environment::TryGetStringValue("GCName", &moduleName))
    {
        return GCHeapUtilities::InitializeDefaultGC();
    }

    NewArrayHolder<char> moduleNameHolder(moduleName);
    HANDLE executableModule = PalGetModuleHandleFromPointer((void*)&GC_Initialize);
    const TCHAR * executableModulePath = NULL;
    PalGetModuleFileName(&executableModulePath, executableModule);
    char* convertedExecutableModulePath = PalCopyTCharAsChar(executableModulePath);
    if (!convertedExecutableModulePath)
    {
        return E_OUTOFMEMORY;
    }
    NewArrayHolder<char> convertedExecutableModulePathHolder(convertedExecutableModulePath);
    {
        char* p = convertedExecutableModulePath;
        char* q = nullptr;
        while (*p != '\0')
        {
            if (*p == DIRECTORY_SEPARATOR_CHAR)
            {
                q = p;
            }
            p++;
        }
        assert(q != nullptr);
        q++;
        *q = '\0';
    }
    size_t folderLength = strlen(convertedExecutableModulePath);
    size_t nameLength = strlen(moduleName);
    char* moduleFullPath = new (nothrow) char[folderLength + nameLength + 1];
    if (!moduleFullPath)
    {
        return E_OUTOFMEMORY;
    }
    NewArrayHolder<char> moduleFullPathHolder(moduleFullPath);
    strcpy(moduleFullPath, convertedExecutableModulePath);
    strcpy(moduleFullPath + folderLength, moduleName);

    HANDLE hMod = PalLoadLibrary(moduleFullPath);

    if (!hMod)
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed to load the Standalone GC library.\n"));
        return E_FAIL;
    }
    IGCToCLR* gcToClr = new (nothrow) MyGCToEEInterface();
    if (!gcToClr)
    {
        return E_OUTOFMEMORY;
    }

    GC_VersionInfoFunction versionInfo = (GC_VersionInfoFunction)PalGetProcAddress(hMod, "GC_VersionInfo");
    if (!versionInfo)
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with the GC_VersionInfo function not found.\n"));
        return E_FAIL;
    }

    g_gc_load_status = GC_LOAD_STATUS_GET_VERSIONINFO;
    g_gc_version_info.MajorVersion = EE_INTERFACE_MAJOR_VERSION;
    g_gc_version_info.MinorVersion = 0;
    g_gc_version_info.BuildVersion = 0;
    versionInfo(&g_gc_version_info);
    g_gc_load_status = GC_LOAD_STATUS_CALL_VERSIONINFO;

    if (g_gc_version_info.MajorVersion < GC_INTERFACE_MAJOR_VERSION)
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with the Standalone GC reported a major version lower than what the runtime requires.\n"));
        return E_FAIL;
    }

    GC_InitializeFunction initFunc = (GC_InitializeFunction)PalGetProcAddress(hMod, "GC_Initialize");
    if (!initFunc)
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with the GC_Initialize function not found.\n"));
        return E_FAIL;
    }

    g_gc_load_status = GC_LOAD_STATUS_GET_INITIALIZE;
    IGCHeap* heap;
    IGCHandleManager* manager;
    HRESULT initResult = initFunc(gcToClr, &heap, &manager, &g_gc_dac_vars);
    if (initResult == S_OK)
    {
        g_pGCHeap = heap;
        g_pGCHandleManager = manager;
        g_gcDacGlobals = &g_gc_dac_vars;
        LOG((LF_GC, LL_INFO100, "GC load successful\n"));
    }
    else
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with HR = 0x%X\n", initResult));
    }

    return initResult;
}

// Initializes a non-standalone GC. The protocol for initializing a non-standalone GC
// is similar to loading a standalone one, except that the GC_VersionInfo and
// GC_Initialize symbols are linked to directory and thus don't need to be loaded.
//
HRESULT GCHeapUtilities::InitializeDefaultGC()
{
    // we should only call this once on startup. Attempting to load a GC
    // twice is an error.
    assert(g_pGCHeap == nullptr);

    IGCHeap* heap;
    IGCHandleManager* manager;
    HRESULT initResult = GC_Initialize(nullptr, &heap, &manager, &g_gc_dac_vars);
    if (initResult == S_OK)
    {
        g_pGCHeap = heap;
        g_pGCHandleManager = manager;
        g_gcDacGlobals = &g_gc_dac_vars;
        LOG((LF_GC, LL_INFO100, "GC load successful\n"));
    }
    else
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with HR = 0x%X\n", initResult));
    }

    return initResult;
}

void GCHeapUtilities::RecordEventStateChange(bool isPublicProvider, GCEventKeyword keywords, GCEventLevel level)
{
    // NativeAOT does not support standalone GC. Call GCEventStatus directly to keep things simple.
    GCEventStatus::Set(isPublicProvider ? GCEventProvider_Default : GCEventProvider_Private, keywords, level);
}

#endif // DACCESS_COMPILE
