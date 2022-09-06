// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class HandlesProfiler : public Profiler
{
public:
    HandlesProfiler() : Profiler(),
        _gcCount(0),
        _failures(0),
        _isInduced(false),
        _weakHandle(NULL),
        _strongHandle(NULL),
        _pinnedHandle(NULL),
        _pinnedObject(NULL)
    {}

	static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason);
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished();
    virtual HRESULT STDMETHODCALLTYPE ObjectAllocated(ObjectID objectId, ClassID classId);

private:
    ObjectID CheckIfAlive(const char* name, ObjectHandleID handle, bool shouldBeAlive);

private:
    std::atomic<int> _gcCount;
    std::atomic<int> _failures;
    std::atomic<bool> _isInduced;
    ObjectHandleID _weakHandle;
    ObjectHandleID _strongHandle;
    ObjectHandleID _pinnedHandle;
    ObjectID _pinnedObject;
};
