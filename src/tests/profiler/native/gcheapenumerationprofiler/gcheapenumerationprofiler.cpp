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
        _failures++;
        printf("FAIL: ICorProfilerInfo::GetEnvironmentVariable() failed hr=0x%x", hr);
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
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
    }

    return hr;
}

HRESULT STDMETHODCALLTYPE GCHeapEnumerationProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    SHUTDOWNGUARD();
    printf("GCHeapEnumerationProfiler::GarbageCollectionStarted\nSleeping for 10 seconds\n");
    std::this_thread::sleep_for(std::chrono::seconds(10));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE GCHeapEnumerationProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();
    printf("GCHeapEnumerationProfiler::GarbageCollectionFinished\n");
    return S_OK;
}

HRESULT GCHeapEnumerationProfiler::Shutdown()
{
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
        _failures++;
    }

    if (_customGCHeapObjectTypesCount != 1)
    {
        printf("GCHeapEnumerationProfiler::Shutdown: FAIL: Expected 1 custom GCHeapObject type, got %d\n", _customGCHeapObjectTypesCount.load());
        _failures++;
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

String GCHeapEnumerationProfiler::GetClassIDNameHelper(ClassID classId) {
    return GetClassIDName(classId);
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

    state->objectsCount->fetch_add(1, std::memory_order_relaxed);

    ClassID classId{0};
    HRESULT hr = state->instance->pCorProfilerInfo->GetClassFromObject(object, &classId);
    if (hr != S_OK)
    {
        printf("Error: failed to get class ID from object.\n");
        // Returning FALSE will stop the enumeration, just skip this object and continue
        return TRUE;
    }

    String classIdName = state->instance->GetClassIDNameHelper(classId);
    String expectedCustomObjectClass = reinterpret_cast<const WCHAR*>(u"CustomGCHeapObject");
    if (classIdName == expectedCustomObjectClass)
    {
        state->customGcHeapObjectTypeCount->fetch_add(1, std::memory_order_relaxed);
    }

    return TRUE;
}

HRESULT GCHeapEnumerationProfiler::EnumerateGCHeapObjects()
{
    printf("GCHeapEnumerationProfiler::EnumerateGCHeapObjects\n");
    GCHeapEnumerationProfiler *instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    CallbackState state = { &_objectsCount, instance, &_customGCHeapObjectTypesCount };

    printf("Enumerating GC Heap Objects\n");
    return pCorProfilerInfo->EnumerateGCHeapObjects(heap_walk_fn, &state);
}

HRESULT GCHeapEnumerationProfiler::ValidateEnumerateGCHeapObjects(HRESULT expected)
{
    HRESULT hr = EnumerateGCHeapObjects();

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

    _failures++;
    return E_FAIL;
}

extern "C" EXPORT void STDMETHODCALLTYPE EnumerateGCHeapObjects()
{
    printf("EnumerateGCHeapObjects PInvoke\n");
    GCHeapEnumerationProfiler *instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    if (instance == nullptr)
    {
        printf("Error: profiler instance is null.\n");
        return;
    }
    instance->ValidateEnumerateGCHeapObjects(0);
}

extern "C" EXPORT void STDMETHODCALLTYPE SuspendRuntime()
{
    printf("SuspendRuntime PInvoke\n");
    GCHeapEnumerationProfiler *instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    if (instance == nullptr)
    {
        printf("Error: profiler instance is null.\n");
        return;
    }
    instance->pCorProfilerInfo->SuspendRuntime();
}

extern "C" EXPORT void STDMETHODCALLTYPE ResumeRuntime()
{
    printf("ResumeRuntime PInvoke\n");
    GCHeapEnumerationProfiler *instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    if (instance == nullptr)
    {
        printf("Error: profiler instance is null.\n");
        return;
    }
    instance->pCorProfilerInfo->ResumeRuntime();
}

extern "C" EXPORT void STDMETHODCALLTYPE EnumerateHeapObjectsInBackgroundThread()
{
    GCHeapEnumerationProfiler* instance = static_cast<GCHeapEnumerationProfiler*>(GCHeapEnumerationProfiler::Instance);
    if (instance == nullptr) {
        printf("Error: profiler instance is null.\n");
        return;
    }

    std::thread([instance]()
                {
                    std::this_thread::sleep_for(std::chrono::seconds(1));
                    instance->ValidateEnumerateGCHeapObjects(0x80131388);
                }).detach();
}
