// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#define SKIP_TRACING_DEFINITIONS
#include "gcenv.h"
#undef SKIP_TRACING_DEFINITIONS
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "gceventstatus.h"
#include "holder.h"
#include "RhConfig.h"

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
}

HRESULT InitializeStandaloneGC();

HRESULT InitializeGCSelector()
{
    return InitializeStandaloneGC();
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
    HANDLE executableModule = PalGetModuleHandleFromPointer((void*)&PalGetModuleHandleFromPointer);
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

    IGCToCLR* gcToClr = new (nothrow) standalone::GCToEEInterface();
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

void standalone::GCToEEInterface::SuspendEE(SUSPEND_REASON reason)
{
    ::GCToEEInterface::SuspendEE(reason);
}

void standalone::GCToEEInterface::RestartEE(bool bFinishedGC)
{
    ::GCToEEInterface::RestartEE(bFinishedGC);
}

void standalone::GCToEEInterface::GcScanRoots(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    ::GCToEEInterface::GcScanRoots(fn, condemned, max_gen, sc);
}

void standalone::GCToEEInterface::GcStartWork(int condemned, int max_gen)
{
    ::GCToEEInterface::GcStartWork(condemned, max_gen);
}

void standalone::GCToEEInterface::BeforeGcScanRoots(int condemned, bool is_bgc, bool is_concurrent)
{
    ::GCToEEInterface::BeforeGcScanRoots(condemned, is_bgc, is_concurrent);
}

void standalone::GCToEEInterface::AfterGcScanRoots(int condemned, int max_gen, ScanContext* sc)
{
    ::GCToEEInterface::AfterGcScanRoots(condemned, max_gen, sc);
}

void standalone::GCToEEInterface::GcDone(int condemned)
{
    ::GCToEEInterface::GcDone(condemned);
}

bool standalone::GCToEEInterface::RefCountedHandleCallbacks(Object * pObject)
{
    return ::GCToEEInterface::RefCountedHandleCallbacks(pObject);
}

void standalone::GCToEEInterface::SyncBlockCacheWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2)
{
    ::GCToEEInterface::SyncBlockCacheWeakPtrScan(scanProc, lp1, lp2);
}

void standalone::GCToEEInterface::SyncBlockCacheDemote(int max_gen)
{
    ::GCToEEInterface::SyncBlockCacheDemote(max_gen);
}

void standalone::GCToEEInterface::SyncBlockCachePromotionsGranted(int max_gen)
{
    ::GCToEEInterface::SyncBlockCachePromotionsGranted(max_gen);
}

uint32_t standalone::GCToEEInterface::GetActiveSyncBlockCount()
{
    return ::GCToEEInterface::GetActiveSyncBlockCount();
}

bool standalone::GCToEEInterface::IsPreemptiveGCDisabled()
{
    return ::GCToEEInterface::IsPreemptiveGCDisabled();
}

bool standalone::GCToEEInterface::EnablePreemptiveGC()
{
    return ::GCToEEInterface::EnablePreemptiveGC();
}

void standalone::GCToEEInterface::DisablePreemptiveGC()
{
    ::GCToEEInterface::DisablePreemptiveGC();
}

Thread* standalone::GCToEEInterface::GetThread()
{
    return ::GCToEEInterface::GetThread();
}

gc_alloc_context * standalone::GCToEEInterface::GetAllocContext()
{
    return ::GCToEEInterface::GetAllocContext();
}

void standalone::GCToEEInterface::GcEnumAllocContexts(enum_alloc_context_func* fn, void* param)
{
    ::GCToEEInterface::GcEnumAllocContexts(fn, param);
}

uint8_t* standalone::GCToEEInterface::GetLoaderAllocatorObjectForGC(Object* pObject)
{
    return ::GCToEEInterface::GetLoaderAllocatorObjectForGC(pObject);
}

void standalone::GCToEEInterface::DiagGCStart(int gen, bool isInduced)
{
    ::GCToEEInterface::DiagGCStart(gen, isInduced);
}

void standalone::GCToEEInterface::DiagUpdateGenerationBounds()
{
    ::GCToEEInterface::DiagUpdateGenerationBounds();
}

void standalone::GCToEEInterface::DiagGCEnd(size_t index, int gen, int reason, bool fConcurrent)
{
    ::GCToEEInterface::DiagGCEnd(index, gen, reason, fConcurrent);
}

void standalone::GCToEEInterface::DiagWalkFReachableObjects(void* gcContext)
{
    ::GCToEEInterface::DiagWalkFReachableObjects(gcContext);
}

void standalone::GCToEEInterface::DiagWalkSurvivors(void* gcContext, bool fCompacting)
{
    ::GCToEEInterface::DiagWalkSurvivors(gcContext, fCompacting);
}

void standalone::GCToEEInterface::DiagWalkUOHSurvivors(void* gcContext, int gen)
{
    ::GCToEEInterface::DiagWalkUOHSurvivors(gcContext, gen);
}

void standalone::GCToEEInterface::DiagWalkBGCSurvivors(void* gcContext)
{
    ::GCToEEInterface::DiagWalkBGCSurvivors(gcContext);
}

void standalone::GCToEEInterface::StompWriteBarrier(WriteBarrierParameters* args)
{
    ::GCToEEInterface::StompWriteBarrier(args);
}

void standalone::GCToEEInterface::EnableFinalization(bool gcHasWorkForFinalizerThread)
{
    ::GCToEEInterface::EnableFinalization(gcHasWorkForFinalizerThread);
}

void standalone::GCToEEInterface::HandleFatalError(unsigned int exitCode)
{
    ::GCToEEInterface::HandleFatalError(exitCode);
}

bool standalone::GCToEEInterface::EagerFinalized(Object* obj)
{
    return ::GCToEEInterface::EagerFinalized(obj);
}

MethodTable* standalone::GCToEEInterface::GetFreeObjectMethodTable()
{
    return ::GCToEEInterface::GetFreeObjectMethodTable();
}

bool standalone::GCToEEInterface::GetBooleanConfigValue(const char* privateKey, const char* publicKey, bool* value)
{
    return ::GCToEEInterface::GetBooleanConfigValue(privateKey, publicKey, value);
}

bool standalone::GCToEEInterface::GetIntConfigValue(const char* privateKey, const char* publicKey, int64_t* value)
{
    return ::GCToEEInterface::GetIntConfigValue(privateKey, publicKey, value);
}

bool standalone::GCToEEInterface::GetStringConfigValue(const char* privateKey, const char* publicKey, const char** value)
{
    return ::GCToEEInterface::GetStringConfigValue(privateKey, publicKey, value);
}

void standalone::GCToEEInterface::FreeStringConfigValue(const char* value)
{
    return ::GCToEEInterface::FreeStringConfigValue(value);
}

bool standalone::GCToEEInterface::IsGCThread()
{
    return ::GCToEEInterface::IsGCThread();
}

bool standalone::GCToEEInterface::WasCurrentThreadCreatedByGC()
{
    return ::GCToEEInterface::WasCurrentThreadCreatedByGC();
}

bool standalone::GCToEEInterface::CreateThread(void (*threadStart)(void*), void* arg, bool is_suspendable, const char* name)
{
    return ::GCToEEInterface::CreateThread(threadStart, arg, is_suspendable, name);
}

void standalone::GCToEEInterface::WalkAsyncPinnedForPromotion(Object* object, ScanContext* sc, promote_func* callback)
{
    ::GCToEEInterface::WalkAsyncPinnedForPromotion(object, sc, callback);
}

void standalone::GCToEEInterface::WalkAsyncPinned(Object* object, void* context, void(*callback)(Object*, Object*, void*))
{
    ::GCToEEInterface::WalkAsyncPinned(object, context, callback);
}

IGCToCLREventSink* standalone::GCToEEInterface::EventSink()
{
    return ::GCToEEInterface::EventSink();
}

uint32_t standalone::GCToEEInterface::GetTotalNumSizedRefHandles()
{
    return ::GCToEEInterface::GetTotalNumSizedRefHandles();
}

bool standalone::GCToEEInterface::AnalyzeSurvivorsRequested(int condemnedGeneration)
{
    return ::GCToEEInterface::AnalyzeSurvivorsRequested(condemnedGeneration);
}

void standalone::GCToEEInterface::AnalyzeSurvivorsFinished(size_t gcIndex, int condemnedGeneration, uint64_t promoted_bytes, void (*reportGenerationBounds)())
{
    ::GCToEEInterface::AnalyzeSurvivorsFinished(gcIndex, condemnedGeneration, promoted_bytes, reportGenerationBounds);
}

void standalone::GCToEEInterface::VerifySyncTableEntry()
{
    ::GCToEEInterface::VerifySyncTableEntry();
}

void standalone::GCToEEInterface::UpdateGCEventStatus(int publicLevel, int publicKeywords, int privateLevel, int privateKeywords)
{
    ::GCToEEInterface::UpdateGCEventStatus(publicLevel, publicKeywords, privateLevel, privateKeywords);
}

void standalone::GCToEEInterface::LogStressMsg(unsigned level, unsigned facility, const StressLogMsg& msg)
{
    ::GCToEEInterface::LogStressMsg(level, facility, msg);
}

uint32_t standalone::GCToEEInterface::GetCurrentProcessCpuCount()
{
    return ::GCToEEInterface::GetCurrentProcessCpuCount();
}

void standalone::GCToEEInterface::DiagAddNewRegion(int generation, BYTE * rangeStart, BYTE * rangeEnd, BYTE * rangeEndReserved)
{
    ::GCToEEInterface::DiagAddNewRegion(generation, rangeStart, rangeEnd, rangeEndReserved);
}

void standalone::GCToEEInterface::LogErrorToHost(const char *message)
{
    ::GCToEEInterface::LogErrorToHost(message);
}