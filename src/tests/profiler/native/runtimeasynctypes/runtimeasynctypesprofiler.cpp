// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "runtimeasynctypesprofiler.h"

RuntimeAsyncTypesProfiler::RuntimeAsyncTypesProfiler()
    : Profiler(),
      _failures(0),
      _classLoadStarts(0),
      _classLoadFinishes(0),
      _classLoadPairFailures(0),
      _arrayClassLoads(0),
      _continuationClassLoads(0),
      _gcStarts(0),
      _classInfo1Incomplete(0),
      _classInfo2Incomplete(0),
      _classLayoutIncomplete(0),
      _continuationContractFailures(0),
      _nilTokenLeaks(0),
      _continuationObjects(0),
      _continuationArrayEdges(0)
{
}

GUID RuntimeAsyncTypesProfiler::GetClsid()
{
    // {7F4E1A63-92C5-4D81-AB37-560EC294F108}
    GUID clsid = {0x7f4e1a63, 0x92c5, 0x4d81, {0xab, 0x37, 0x56, 0x0e, 0xc2, 0x94, 0xf1, 0x08}};
    return clsid;
}

HRESULT RuntimeAsyncTypesProfiler::Initialize(IUnknown* pCorProfilerInfoUnk)
{
    HRESULT hr = Profiler::Initialize(pCorProfilerInfoUnk);
    if (FAILED(hr))
    {
        return hr;
    }

    return pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_CLASS_LOADS | COR_PRF_MONITOR_GC, 0);
}

HRESULT RuntimeAsyncTypesProfiler::ClassLoadStarted(ClassID classId)
{
    SHUTDOWNGUARD();

    _classLoadStarts++;
    std::lock_guard<std::mutex> lock(_stateLock);
    _classLoadsInProgress.insert(classId);
    return S_OK;
}

HRESULT RuntimeAsyncTypesProfiler::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    SHUTDOWNGUARD();

    _classLoadFinishes++;

    {
        std::lock_guard<std::mutex> lock(_stateLock);
        if (_classLoadsInProgress.erase(classId) == 0)
        {
            _classLoadPairFailures++;
        }
    }

    if (FAILED(hrStatus))
    {
        _failures++;
        return S_OK;
    }

    CorElementType baseElementType = ELEMENT_TYPE_END;
    ClassID baseClassId = 0;
    ULONG rank = 0;
    HRESULT hr = pCorProfilerInfo->IsArrayClass(classId, &baseElementType, &baseClassId, &rank);
    if (hr == S_OK)
    {
        _arrayClassLoads++;
    }
    else if (hr == S_FALSE)
    {
        ModuleID moduleId = 0;
        mdTypeDef token = 0;
        ClassID parentClassId = 0;
        ULONG32 typeArgumentCount = 0;
        hr = pCorProfilerInfo->GetClassIDInfo2(
            classId, &moduleId, &token, &parentClassId, 0, &typeArgumentCount, nullptr);
        if (hr == CORPROF_E_DATAINCOMPLETE)
        {
            _continuationClassLoads++;
        }
    }
    else
    {
        _failures++;
    }

    return S_OK;
}

bool RuntimeAsyncTypesProfiler::CheckContinuationApis(ClassID classId)
{
    ModuleID moduleId = 0;
    mdTypeDef token = 0;
    ClassID parentClassId = 0;
    ULONG32 typeArgumentCount = 0;

    HRESULT info2 = pCorProfilerInfo->GetClassIDInfo2(
        classId, &moduleId, &token, &parentClassId, 0, &typeArgumentCount, nullptr);
    if (info2 == CORPROF_E_DATAINCOMPLETE)
    {
        _classInfo2Incomplete++;
    }
    bool info2ReturnedNilToken = info2 == S_OK && IsNilToken(token);

    moduleId = 0;
    token = 0;
    HRESULT info1 = pCorProfilerInfo->GetClassIDInfo(classId, &moduleId, &token);
    if (info1 == CORPROF_E_DATAINCOMPLETE)
    {
        _classInfo1Incomplete++;
    }
    bool info1ReturnedNilToken = info1 == S_OK && IsNilToken(token);

    COR_FIELD_OFFSET fieldOffsets[64];
    ULONG fieldCount = 0;
    ULONG classSize = 0;
    HRESULT layout = pCorProfilerInfo->GetClassLayout(
        classId,
        fieldOffsets,
        static_cast<ULONG>(sizeof(fieldOffsets) / sizeof(fieldOffsets[0])),
        &fieldCount,
        &classSize);
    if (layout == CORPROF_E_DATAINCOMPLETE)
    {
        _classLayoutIncomplete++;
    }

    bool metadataLess = info1 == CORPROF_E_DATAINCOMPLETE ||
                        info2 == CORPROF_E_DATAINCOMPLETE ||
                        layout == CORPROF_E_DATAINCOMPLETE;
    if (metadataLess &&
        (info1 != CORPROF_E_DATAINCOMPLETE ||
         info2 != CORPROF_E_DATAINCOMPLETE ||
         layout != CORPROF_E_DATAINCOMPLETE))
    {
        _continuationContractFailures++;
    }
    if (metadataLess && (info1ReturnedNilToken || info2ReturnedNilToken))
    {
        _nilTokenLeaks++;
    }

    return metadataLess;
}

HRESULT RuntimeAsyncTypesProfiler::GarbageCollectionStarted(
    int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    SHUTDOWNGUARD();

    _gcStarts++;
    std::lock_guard<std::mutex> lock(_stateLock);
    _objectClasses.clear();
    _continuationEdges.clear();
    return S_OK;
}

HRESULT RuntimeAsyncTypesProfiler::ObjectReferences(
    ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[])
{
    SHUTDOWNGUARD();

    bool metadataLess = CheckContinuationApis(classId);

    std::lock_guard<std::mutex> lock(_stateLock);
    _objectClasses[objectId] = classId;
    if (metadataLess)
    {
        _continuationObjects++;
        for (ULONG i = 0; i < cObjectRefs; i++)
        {
            if (objectRefIds[i] != 0)
            {
                _continuationEdges.emplace_back(objectId, objectRefIds[i]);
            }
        }
    }

    return S_OK;
}

void RuntimeAsyncTypesProfiler::AnalyzeObjectGraph()
{
    int arrayEdges = 0;
    std::lock_guard<std::mutex> lock(_stateLock);
    for (const auto& edge : _continuationEdges)
    {
        auto target = _objectClasses.find(edge.second);
        if (target == _objectClasses.end())
        {
            continue;
        }

        CorElementType baseElementType = ELEMENT_TYPE_END;
        ClassID baseClassId = 0;
        ULONG rank = 0;
        if (pCorProfilerInfo->IsArrayClass(
                target->second, &baseElementType, &baseClassId, &rank) == S_OK &&
            rank == 1)
        {
            arrayEdges++;
        }
    }

    _continuationArrayEdges += arrayEdges;
}

HRESULT RuntimeAsyncTypesProfiler::GarbageCollectionFinished()
{
    SHUTDOWNGUARD();

    AnalyzeObjectGraph();
    return S_OK;
}

HRESULT RuntimeAsyncTypesProfiler::Shutdown()
{
    HRESULT hr = Profiler::Shutdown();

    printf("RuntimeAsyncTypesProfiler: failures=%d classLoads=%d/%d pairFailures=%d "
           "arrays=%d continuations=%d info1=%d info2=%d layout=%d contractFailures=%d "
           "nilTokens=%d gcStarts=%d continuationObjects=%d arrayEdges=%d\n",
           _failures.load(),
           _classLoadStarts.load(), _classLoadFinishes.load(), _classLoadPairFailures.load(),
           _arrayClassLoads.load(), _continuationClassLoads.load(),
           _classInfo1Incomplete.load(), _classInfo2Incomplete.load(),
           _classLayoutIncomplete.load(), _continuationContractFailures.load(),
           _nilTokenLeaks.load(), _gcStarts.load(), _continuationObjects.load(),
           _continuationArrayEdges.load());

    bool passed =
        SUCCEEDED(hr) &&
        _failures == 0 &&
        _classLoadStarts == _classLoadFinishes &&
        _classLoadPairFailures == 0 &&
        _arrayClassLoads > 0 &&
        _continuationClassLoads > 0 &&
        _gcStarts > 0 &&
        _classInfo1Incomplete > 0 &&
        _classInfo2Incomplete > 0 &&
        _classLayoutIncomplete > 0 &&
        _continuationContractFailures == 0 &&
        _nilTokenLeaks == 0 &&
        _continuationObjects > 0 &&
        _continuationArrayEdges > 0;

    printf(passed ? "PROFILER TEST PASSES\n" : "PROFILER TEST FAILED\n");
    fflush(stdout);
    return hr;
}
