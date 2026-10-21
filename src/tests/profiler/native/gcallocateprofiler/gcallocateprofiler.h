// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

class GCAllocateProfiler : public Profiler
{
public:
    GCAllocateProfiler() : Profiler(),
        _gcLOHAllocations(0),
        _gcPOHAllocations(0),
        _failures(0)
    {}

	static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE ObjectAllocated(ObjectID objectId, ClassID classId);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

private:
    enum class PlacementFlagPerturbation
    {
        None,
        Drop,
        Swap,
    };

    typedef int64_t (*GetAllocationNotificationCount)();
    typedef uint32_t (*GetAllocationNotificationFlags)(int64_t);
    typedef void* (*GetAllocationNotificationObject)(int64_t);
    typedef void (*ResetAllocationNotifications)(void*, size_t);

    std::atomic<int> _gcLOHAllocations;
    std::atomic<int> _gcPOHAllocations;
    std::atomic<int> _failures;
    GetAllocationNotificationCount _getAllocationNotificationCount = nullptr;
    GetAllocationNotificationCount _getAllocationNotificationErrorCount = nullptr;
    GetAllocationNotificationFlags _getAllocationNotificationFlags = nullptr;
    GetAllocationNotificationObject _getAllocationNotificationObject = nullptr;
    ResetAllocationNotifications _resetAllocationNotifications = nullptr;
    PlacementFlagPerturbation _placementFlagPerturbation = PlacementFlagPerturbation::None;
};
