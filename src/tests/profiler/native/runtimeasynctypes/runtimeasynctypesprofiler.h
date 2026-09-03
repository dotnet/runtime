// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

#include <mutex>
#include <unordered_map>
#include <unordered_set>
#include <utility>
#include <vector>

class RuntimeAsyncTypesProfiler : public Profiler
{
public:
    RuntimeAsyncTypesProfiler();

    static GUID GetClsid();
    HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pCorProfilerInfoUnk) override;
    HRESULT STDMETHODCALLTYPE Shutdown() override;
    HRESULT STDMETHODCALLTYPE ClassLoadStarted(ClassID classId) override;
    HRESULT STDMETHODCALLTYPE ClassLoadFinished(ClassID classId, HRESULT hrStatus) override;
    HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(
        int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override;
    HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override;
    HRESULT STDMETHODCALLTYPE ObjectReferences(
        ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[]) override;

private:
    bool CheckContinuationApis(ClassID classId);
    void AnalyzeObjectGraph();

    std::atomic<int> _failures;
    std::atomic<int> _classLoadStarts;
    std::atomic<int> _classLoadFinishes;
    std::atomic<int> _classLoadPairFailures;
    std::atomic<int> _arrayClassLoads;
    std::atomic<int> _continuationClassLoads;
    std::atomic<int> _gcStarts;
    std::atomic<int> _classInfo1Incomplete;
    std::atomic<int> _classInfo2Incomplete;
    std::atomic<int> _classLayoutIncomplete;
    std::atomic<int> _continuationContractFailures;
    std::atomic<int> _nilTokenLeaks;
    std::atomic<int> _continuationObjects;
    std::atomic<int> _continuationArrayEdges;

    std::mutex _stateLock;
    std::unordered_set<ClassID> _classLoadsInProgress;
    std::unordered_map<ObjectID, ClassID> _objectClasses;
    std::vector<std::pair<ObjectID, ObjectID>> _continuationEdges;
};
