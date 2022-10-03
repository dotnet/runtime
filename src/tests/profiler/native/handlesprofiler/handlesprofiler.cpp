// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "handlesprofiler.h"
#include <iostream>

using std::wcout;
using std::endl;

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
//   3. A gen2 GC is triggered
//   --> HandlesProfiler ensures:
//       - weak wrapped objects are no more alive
//       - strong and pinned wrapped objects are still alive
//   4. A gen2 is triggered.
//   --> HandlesProfiler destroys strong and pinned handles + wrap the corresponding
//       instances with a weak reference
//   5. A gen2 is triggered.
//   --> HandlesProfiler ensures that no more instances are alive.
//
HRESULT HandlesProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_ENABLE_OBJECT_ALLOCATED | COR_PRF_MONITOR_OBJECT_ALLOCATED, COR_PRF_HIGH_BASIC_GC | COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED | COR_PRF_HIGH_MONITOR_PINNEDOBJECT_ALLOCATED)))
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
        else
        {
            printf("HandlesProfiler::ObjectAllocated: weak handle created.\n");
        }
    }
    else if (typeName == WCHAR("Profiler.Tests.TestClassForStrongHandle"))
    {
        hr = pCorProfilerInfo->CreateHandle(objectId, COR_PRF_HANDLE_TYPE::COR_PRF_HANDLE_TYPE_STRONG, &_strongHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::ObjectAllocated: FAIL: CreateHandle failed for strong handle.\n");
        }
        else
        {
            printf("HandlesProfiler::ObjectAllocated: strong handle created.\n");
        }
    }
    else if (typeName == WCHAR("Profiler.Tests.TestClassForPinnedHandle"))
    {
        // Keep track of the address of the p�nned object to be able
        // to check that it will not be moved by the next collection
        _pinnedObject = objectId;

        hr = pCorProfilerInfo->CreateHandle(objectId, COR_PRF_HANDLE_TYPE::COR_PRF_HANDLE_TYPE_PINNED, &_pinnedHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::ObjectAllocated: FAIL: CreateHandle failed for pinned handle.\n");
        }
        else
        {
            printf("HandlesProfiler::ObjectAllocated: pinned handle created.\n");
        }
    }
    return S_OK;
}

ObjectID HandlesProfiler::CheckIfAlive(const char* name, ObjectHandleID handle, bool shouldBeAlive)
{
    if (handle == NULL)
    {
        _failures++;
        printf("HandlesProfiler::CheckIfAlive(%s): FAIL: null handle.\n", name);
        return NULL;
    }

    ObjectID objectId{0};
    HRESULT hr = pCorProfilerInfo->GetObjectIDFromHandle(handle, &objectId);
    if (FAILED(hr))
    {
        _failures++;
        printf("HandlesProfiler::CheckIfAlive(%s): FAIL: GetObjectIDFromHandle failed.\n", name);
        return NULL;
    }

    if (shouldBeAlive)
    {
        if (objectId == NULL)
        {
            _failures++;
            printf("HandlesProfiler::CheckIfAlive(%s): FAIL: the object should be alive.\n", name);
        }
        else
        {
            printf("HandlesProfiler::CheckIfAlive(%s): object alive as expected ", name);
            ClassID classId{0};
            hr = pCorProfilerInfo->GetClassFromObject(objectId, &classId);
            if (FAILED(hr))
            {
                _failures++;
                printf("(FAIL: impossible to get class from object).\n");
            }
            else
            {
                String typeName = GetClassIDName(classId);
                wcout << "("<< typeName.ToWString() << ")" << std::endl;

                return objectId;
            }
        }
    }
    else
    {
        if (objectId != NULL)
        {
            _failures++;
            printf("HandlesProfiler::CheckIfAlive(%s): FAIL: the object should not be alive anymore.\n", name);
        }
        else
        {
            printf("HandlesProfiler::CheckIfAlive(%s): object not alive as expected.\n", name);
        }
    }

    return NULL;
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
        // Weak should not be here anymore
        CheckIfAlive("weak", _weakHandle, false);

        // The others should still be alive
        CheckIfAlive("strong", _strongHandle, true);
        ObjectID pinnedObject = CheckIfAlive("pinned", _pinnedHandle, true);

        // Check that pinned object was not moved by the previous collection
        if (pinnedObject != _pinnedObject)
        {
            _failures++;
            printf("HandlesProfiler::GarbageCollectionFinished(#1): FAIL: pinned handle object address has changed.\n");
        }
        else
        {
            printf("HandlesProfiler::GarbageCollectionFinished(#1): pinned handle object address did not changed as expected.\n");
        }
    }
    else if (_gcCount == 2)
    {
        if (_strongHandle == NULL)
        {
            _failures++;
            printf("HandlesProfiler::GarbageCollectionFinished(#2): FAIL: null strong handle.\n");
            return S_OK;
        }
        if (_pinnedHandle == NULL)
        {
            _failures++;
            printf("HandlesProfiler::GarbageCollectionFinished(#2): FAIL: null pinned handle.\n");
            return S_OK;
        }

        // Keep a weak reference on them to be able to ensure that the instances
        // will be released after the next collection
        ObjectID strongObject = CheckIfAlive("strong", _strongHandle, true);
        ObjectID pinnedObject = CheckIfAlive("pinned", _pinnedHandle, true);

        // Destroy strong and pinned handles so next GC will release the objects
        HRESULT hr = pCorProfilerInfo->DestroyHandle(_strongHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::GarbageCollectionFinished(#2): FAIL: DestroyHandle failed for strong handle.\n");
        }
        else
        {
            printf("HandlesProfiler::GarbageCollectionFinished(#2): strong handle destroyed.\n");
            pCorProfilerInfo->CreateHandle(strongObject, COR_PRF_HANDLE_TYPE::COR_PRF_HANDLE_TYPE_WEAK, &_strongHandle);
        }

        hr = pCorProfilerInfo->DestroyHandle(_pinnedHandle);
        if (FAILED(hr))
        {
            _failures++;
            printf("HandlesProfiler::GarbageCollectionFinished(#2): FAIL: DestroyHandle failed for pinned handle.\n");
        }
        else
        {
            printf("HandlesProfiler::GarbageCollectionFinished(#2): pinned handle destroyed.\n");
            pCorProfilerInfo->CreateHandle(pinnedObject, COR_PRF_HANDLE_TYPE::COR_PRF_HANDLE_TYPE_WEAK, &_pinnedHandle);
        }
    }
    else if (_gcCount == 3)
    {
        printf("HandlesProfiler::GarbageCollectionFinished(#3): Checking handles:\n");

        // Check that instances wrapped by strong and pinned handles are not here any more
        CheckIfAlive("strong", _strongHandle, false);
        CheckIfAlive("pinned", _pinnedHandle, false);
    }
    else
    {
        _failures++;
        printf("HandlesProfiler::GarbageCollectionStarted: FAIL: no more than 3 garbage collections are expected.\n");
    }

    return S_OK;
}
