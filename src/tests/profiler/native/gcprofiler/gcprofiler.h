// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"
#include <unordered_set>


// TODO: this class is intended to be the heavyweight GC API verification (i.e. COR_PRF_MONITOR_GC)
// vs GCBasicProfiler which verifies COR_PRF_HIGH_BASIC_GC. Right now the only thing it does
// is verify we see POH objects in the RootReferences callback, but it should be fleshed out.
class GCProfiler : public Profiler
{
public:
    GCProfiler() : Profiler(),
        _gcStarts(0),
        _gcFinishes(0),
        _failures(0),
        _pohObjectsSeenRootReferences(0),
        _pohObjectsSeenObjectReferences(0),
        _rootReferencesSeen(),
        _objectReferencesSeen()
    {}

    virtual GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished();
    virtual HRESULT STDMETHODCALLTYPE ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[]);
    virtual HRESULT STDMETHODCALLTYPE RootReferences(ULONG cRootRefs, ObjectID rootRefIds[]);

private:
    std::atomic<int> _gcStarts;
    std::atomic<int> _gcFinishes;
    std::atomic<int> _failures;
    std::atomic<int> _pohObjectsSeenRootReferences;
    std::atomic<int> _pohObjectsSeenObjectReferences;
    std::unordered_set<ObjectID> _rootReferencesSeen;
    std::unordered_set<ObjectID> _objectReferencesSeen;

    int NumPOHObjectsSeen(std::unordered_set<ObjectID> objects);
};
