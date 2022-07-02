// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "handlesprofiler.h"

GUID HandlesProfiler::GetClsid()
{
    // {A0F96622-522D-4654-AA56-BF421E79B210}
	GUID clsid = { 0xa0f96622, 0x522d, 0x4654, { 0xaa, 0x56, 0xbf, 0x42, 0x1e, 0x79, 0xb2, 0x10 } };
	return clsid;
}

// The goal of this test is to validate the ICorProfilerInfo13 handle management methods:
//   CreateHandle (weak, strong, pinned)
//   DestroyHandle
//   GetObjectIDFromHandle
//
// SCENARIO:
//   1. Specific managed types instances are created but no reference are kept.
//   2. The corresponding native HandlesProfiler creates a handle for each.
//   3. A gen0 GC is triggered 
//   --> HandlesProfiler ensures:
//       - weak wrapped objects are no more alive
//       - strong and pinned wrapped objects are still alive
//   4. A gen1 is triggered.
//   5. HandlesProfiler destroys strong and pinned handles.
//   6. A gen2 is triggered.
//   7. HandlesProfiler ensures that no more instances are alive.
//
HRESULT HandlesProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_OBJECT_ALLOCATED, COR_PRF_HIGH_BASIC_GC)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT HandlesProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_gcCount == 0)
    {
        printf("HandlesProfiler::Shutdown: FAIL: Expected GarbageCollectionStarted to be called\n");
    }
    else if(_failures == 0)
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

HRESULT HandlesProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    SHUTDOWNGUARD();

    _isInduced = (COR_PRF_GC_REASON::COR_PRF_GC_INDUCED == (reason & COR_PRF_GC_REASON::COR_PRF_GC_INDUCED));
    if (_isInduced)
    {
        _gcCount++;
    }

    return S_OK;
}

HRESULT HandlesProfiler::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    // Create handles for TestClassForWeakHandle, TestClassForStrongHandle and TestClassForPinnedHandle instances
    String typeName = GetClassIDName(classId);
    HRESULT hr = S_OK;
    if (typeName == WCHAR("Profiler.Tests.TestClassForWeakHandle"))
    {
        hr = pCorProfilerInfo->CreateHandle(objectId, COR_PRF_HANDLE_TYPE::COR_PRF_HANDLE_TYPE_WEAK, &_weakHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::ObjectAllocated: FAIL: CreateHandle failed for weak handle.\n");
        }
    }
    else
    if (typeName == WCHAR("Profiler.Tests.TestClassForStrongHandle"))
    {
        hr = pCorProfilerInfo->CreateHandle(objectId, COR_PRF_HANDLE_TYPE::COR_PRF_HANDLE_TYPE_STRONG, &_strongHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::ObjectAllocated: FAIL: CreateHandle failed for strong handle.\n");
        }
    }
    else
    if (typeName == WCHAR("Profiler.Tests.TestClassForPinnedHandle"))
    {
        hr = pCorProfilerInfo->CreateHandle(objectId, COR_PRF_HANDLE_TYPE::COR_PRF_HANDLE_TYPE_PINNED, &_pinnedHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::ObjectAllocated: FAIL: CreateHandle failed for pinned handle.\n");
        }
    }
    return S_OK;
}

void HandlesProfiler::CheckIfAlive(ObjectHandleID handle, bool shouldBeAlive)
{
        ObjectID objectId{0};
        HRESULT hr = pCorProfilerInfo->GetObjectIDFromHandle(handle, &objectId);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::CheckIfAlive: FAIL: GetObjectIDFromHandle failed.\n");
            return;
        }

        if (shouldBeAlive)
        {
            if (objectId == NULL)
            {
                _failures++;
                printf("HandlesProfiler::CheckIfAlive: FAIL: the object should be alive.\n");
            }
        }
        else
        {
            if (objectId != NULL)
            {
                _failures++;
                printf("HandlesProfiler::CheckIfAlive: FAIL: the object should not be alive anymore.\n");
            }
        }
}

HRESULT HandlesProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();

    if (!_isInduced)
    {
        return S_OK;
    }

    HRESULT hr = S_OK;
    if (_gcCount == 1)
    {
        // weak should not be here anymore
        CheckIfAlive(_weakHandle, false);

        // the others should still be alive
        CheckIfAlive(_strongHandle, true);
        CheckIfAlive(_pinnedHandle, true);
    }
    else
    if (_gcCount == 2)
    {
        // Destroy strong and pinned handles so next GC will release the objects
        HRESULT hr = pCorProfilerInfo->DestroyHandle(_strongHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::GarbageCollectionFinished: FAIL: DestroyHandle failed for strong handle.\n");
        }

        hr = pCorProfilerInfo->DestroyHandle(_pinnedHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::GarbageCollectionFinished: FAIL: DestroyHandle failed for pinned handle.\n");
        }
    }
    else
    if (_gcCount == 3)
    {
        // TODO: Check that instances wrapped by strong and pinned handles are not here any more
        // ?? is it supported to get object from a destroyed handle ??
    }
    else
    {
        _failures++;
        printf("HandlesProfiler::GarbageCollectionStarted: FAIL: no more than 3 garbage collections are expected.\n");
    }

    return S_OK;
}
