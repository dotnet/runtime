// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcheapenumerationprofiler.h"
#include "../profilerstring.h"

#if WIN32
#define EXPORT
#else // WIN32
#define EXPORT __attribute__ ((visibility ("default")))
#endif // WIN32

GUID GCHeapEnumerationProfiler::GetClsid()
{
    // {8753F0E1-6D6D-4329-B8E1-334918869C15}
	GUID clsid = { 0x8753f0e1, 0x6d6d, 0x4329,{ 0xb8, 0xe1, 0x33, 0x49, 0x18, 0x86, 0x9c, 0x15 } };
	return clsid;
}

HRESULT GCHeapEnumerationProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);
    printf("GCHeapEnumerationProfiler::Initialize\n");

    int eventMask = 0;
    constexpr ULONG bufferSize = 1024;
    ULONG envVarLen = 0;
    WCHAR envVar[bufferSize];
    HRESULT hr = S_OK;

    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("Set_Monitor_GC_Event_Mask"), bufferSize, &envVarLen, envVar)))
    {
        printf("FAIL: ICorProfilerInfo::GetEnvironmentVariable() failed hr=0x%x", hr);
        IncrementFailures();
        return hr;
    }
    String envVarStr = envVar;
    String expectedEnvVarStr = reinterpret_cast<const WCHAR*>(u"TRUE");
    if (envVarStr == expectedEnvVarStr)
    {
        printf("Setting GarbageCollectionStarted event masks\n");
        eventMask |= COR_PRF_MONITOR_GC;
    }

    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(eventMask, COR_PRF_HIGH_MONITOR_NONE)))
    {
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        IncrementFailures();
    }

    return hr;
}

void GCHeapEnumerationProfiler::IncrementFailures()
{
    _failures.fetch_add(1, std::memory_order_relaxed);
}

HRESULT STDMETHODCALLTYPE GCHeapEnumerationProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    SHUTDOWNGUARD();
    printf("GCHeapEnumerationProfiler::GarbageCollectionStarted\n");
    _gcStartSleeping = TRUE;
    GCHeapEnumerationProfiler *instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    // The current thread should block the subsequent background thread until GC completes
    // If the call to EnumerateGCHeapObjects doesn't wait for GC to complete, it should
    // observe IsGCStartSleeping() as true during the heap walk.
    _threadList.emplace_back(std::thread([instance]()
                {
                    printf("EnumerateGCHeapObject on native background thread\n");
                    instance->ValidateEnumerateGCHeapObjects(S_OK);
                }));
    std::this_thread::sleep_for(std::chrono::seconds(1));
    _gcStartSleeping = FALSE;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE GCHeapEnumerationProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();
    printf("GCHeapEnumerationProfiler::GarbageCollectionFinished\n");
    return S_OK;
}

bool GCHeapEnumerationProfiler::IsGCStartSleeping()
{
    return _gcStartSleeping;
}

HRESULT GCHeapEnumerationProfiler::Shutdown()
{
    // make sure async operations have finished
    for (auto& t : _threadList) {
        t.join();
    }
    Profiler::Shutdown();

    if (_expectedExceptions == 1)
    {
        printf("GCHeapEnumerationProfiler::Shutdown: PASS: Encountered exception as expected.\n");
        printf("PROFILER TEST PASSES\n");
        fflush(stdout);
        return S_OK;
    }

    if (_objectsCount < 100)
    {
        printf("GCHeapEnumerationProfiler::Shutdown: FAIL: Expected at least 100 objects, got %d\n", _objectsCount.load());
        IncrementFailures();
    }

    if (_customGCHeapObjectTypesCount != 1)
    {
        printf("GCHeapEnumerationProfiler::Shutdown: FAIL: Expected 1 custom GCHeapObject type, got %d\n", _customGCHeapObjectTypesCount.load());
        IncrementFailures();
    }

    if (_failures == 0)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        // failures were printed earlier when _failures was incremented
    }
    fflush(stdout);

    return S_OK;
}

struct CallbackState
{
    std::atomic<int>* objectsCount;
    GCHeapEnumerationProfiler *instance;
    std::atomic<int>* customGcHeapObjectTypeCount;
};

static BOOL STDMETHODCALLTYPE heap_walk_fn(ObjectID object, void* callbackState)
{
    CallbackState* state = static_cast<CallbackState*>(callbackState);

    if (state->instance->IsGCStartSleeping())
    {
        printf("Error: no callbacks expected during GC\n");
        state->instance->IncrementFailures();
        return FALSE;
    }

    state->objectsCount->fetch_add(1, std::memory_order_relaxed);

    ClassID classId{0};
    HRESULT hr = state->instance->pCorProfilerInfo->GetClassFromObject(object, &classId);
    if (FAILED(hr))
    {
        printf("Error: failed to get class ID from object with ID 0x%p. hr=0x%x\n", (void *)object, hr);
        state->instance->IncrementFailures();
        return FALSE;
    }

    String classIdName = state->instance->GetClassIDName(classId);
    String expectedCustomObjectClass = reinterpret_cast<const WCHAR*>(u"CustomGCHeapObject");
    if (classIdName == expectedCustomObjectClass)
    {
        state->customGcHeapObjectTypeCount->fetch_add(1, std::memory_order_relaxed);
    }

    return TRUE;
}

HRESULT GCHeapEnumerationProfiler::ValidateEnumerateGCHeapObjects(HRESULT expected)
{
    printf("GCHeapEnumerationProfiler::ValidateEnumerateGCHeapObjects\n");
    GCHeapEnumerationProfiler *instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    CallbackState state = { &_objectsCount, instance, &_customGCHeapObjectTypesCount };

    printf("Enumerating GC Heap Objects\n");
    HRESULT hr = pCorProfilerInfo->EnumerateGCHeapObjects(heap_walk_fn, &state);

    if (hr == expected)
    {
        if (FAILED(expected))
        {
            printf("Encountered exception as expected.\n");
            _expectedExceptions++;
            return S_OK;
        }

        printf("EnumerateGCHeapObjects succeeded.\n");
        printf("Number of objects: %d\n", _objectsCount.load());
        printf("Number of custom GCHeapObject types: %d\n", _customGCHeapObjectTypesCount.load());
        return S_OK;
    }

    if (FAILED(hr) && FAILED(expected))
    {
        printf("EnumerateGCHeapObjects failed with exception hr=0x%x, but expected exception hr=0x%x\n", hr, expected);
    }
    else if (FAILED(expected))
    {
        printf("EnumerateGCHeapObjects succeeded, but expected exception hr=0x%x\n", expected);
    }
    else // FAILED(hr)
    {
        printf("Error: failed to enumerate GC heap objects. hr=0x%x\n", hr);
    }

    IncrementFailures();
    return E_FAIL;
}

extern "C" EXPORT void STDMETHODCALLTYPE EnumerateGCHeapObjectsWithoutProfilerRequestedRuntimeSuspension()
{
    printf("EnumerateGCHeapObjectsWithoutPriorRuntimeSuspension PInvoke\n");
    GCHeapEnumerationProfiler *instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    if (instance == nullptr)
    {
        printf("Error: profiler instance is null.\n");
        return;
    }

    HRESULT hr = instance->ValidateEnumerateGCHeapObjects(S_OK);
    if (FAILED(hr))
    {
        instance->IncrementFailures();
    }
}

extern "C" EXPORT void STDMETHODCALLTYPE EnumerateGCHeapObjectsWithinProfilerRequestedRuntimeSuspension()
{
    printf("EnumerateGCHeapObjectsWithinProfilerRequestedRuntimeSuspension PInvoke\n");
    GCHeapEnumerationProfiler *instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    if (instance == nullptr)
    {
        printf("Error: profiler instance is null.\n");
        return;
    }

    printf("Profiler Suspending Runtime\n");
    HRESULT hr = instance->pCorProfilerInfo->SuspendRuntime();
    if (FAILED(hr))
    {
        printf("Error: failed to suspend runtime. hr=0x%x\n", hr);
        instance->IncrementFailures();
        return;
    }

    hr = instance->ValidateEnumerateGCHeapObjects(S_OK);
    if (FAILED(hr))
    {
        return;
    }

    printf("Profiler Resuming Runtime\n");
    hr = instance->pCorProfilerInfo->ResumeRuntime();
    if (FAILED(hr))
    {
        printf("Error: failed to resume runtime. hr=0x%x\n", hr);
        instance->IncrementFailures();
        return;
    }
}