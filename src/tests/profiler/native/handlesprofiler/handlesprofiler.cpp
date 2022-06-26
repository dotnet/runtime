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
    // TODO: Create handles for TestClassForWeakHandle, TestClassForStrongHandle and TestClassForPinnedHandle instances
    // use String Profiler::GetClassIDName(ClassID classId)
    return S_OK;
}


HRESULT HandlesProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();

    if (!_isInduced)
    {
        return S_OK;
    }

    if (_gcCount == 1)
    {
        // TODO: Check handle references (weak should not be here anymore)
    }
    else
    if (_gcCount == 2)
    {
        // TODO: Destroy strong and pinned handles
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
