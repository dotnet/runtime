// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "gcallocateprofiler.h"

#include <stdlib.h>
#include <string.h>

GUID GCAllocateProfiler::GetClsid()
{
    // {55b9554d-6115-45a2-be1e-c80f7fa35369}
	GUID clsid = { 0x55b9554d, 0x6115, 0x45a2,{ 0xbe, 0x1e, 0xc8, 0x0f, 0x7f, 0xa3, 0x53, 0x69 } };
	return clsid;
}

HRESULT GCAllocateProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

#if WIN32
    char* hookLibrary = nullptr;
    size_t hookLibraryLength = 0;
    _dupenv_s(&hookLibrary, &hookLibraryLength, "P14_NATIVE_HOOK_LIBRARY");
    if (hookLibrary != nullptr)
    {
        HMODULE module = LoadLibraryA(hookLibrary);
        free(hookLibrary);
        if (module == NULL)
        {
            printf("FAIL: unable to load allocation notification hooks\n");
            return E_FAIL;
        }

        _getAllocationNotificationCount =
            reinterpret_cast<GetAllocationNotificationCount>(GetProcAddress(
                module,
                "GC_AllocationNotificationTest_GetCount"));
        _getAllocationNotificationErrorCount =
            reinterpret_cast<GetAllocationNotificationCount>(GetProcAddress(
                module,
                "GC_AllocationNotificationTest_GetErrorCount"));
        _getAllocationNotificationFlags =
            reinterpret_cast<GetAllocationNotificationFlags>(GetProcAddress(
                module,
                "GC_AllocationNotificationTest_GetFlags"));
        _getAllocationNotificationObject =
            reinterpret_cast<GetAllocationNotificationObject>(GetProcAddress(
                module,
                "GC_AllocationNotificationTest_GetObject"));
        _resetAllocationNotifications =
            reinterpret_cast<ResetAllocationNotifications>(GetProcAddress(
                module,
                "GC_AllocationNotificationTest_Reset"));
        if ((_getAllocationNotificationCount == nullptr) ||
            (_getAllocationNotificationErrorCount == nullptr) ||
            (_getAllocationNotificationFlags == nullptr) ||
            (_getAllocationNotificationObject == nullptr) ||
            (_resetAllocationNotifications == nullptr))
        {
            printf("FAIL: unable to resolve allocation notification hooks\n");
            return E_FAIL;
        }

        _resetAllocationNotifications(nullptr, 0);

        char* placementFlagPerturbation = nullptr;
        size_t placementFlagPerturbationLength = 0;
        _dupenv_s(
            &placementFlagPerturbation,
            &placementFlagPerturbationLength,
            "P14_PROFILER_PLACEMENT_PERTURBATION");
        if (placementFlagPerturbation != nullptr)
        {
            if (strcmp(placementFlagPerturbation, "drop") == 0)
            {
                _placementFlagPerturbation = PlacementFlagPerturbation::Drop;
            }
            else if (strcmp(placementFlagPerturbation, "swap") == 0)
            {
                _placementFlagPerturbation = PlacementFlagPerturbation::Swap;
            }
            else
            {
                printf("FAIL: unknown placement flag perturbation\n");
                free(placementFlagPerturbation);
                return E_INVALIDARG;
            }
            free(placementFlagPerturbation);
        }
    }
#endif // WIN32

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_ENABLE_OBJECT_ALLOCATED, COR_PRF_HIGH_BASIC_GC | COR_PRF_HIGH_MONITOR_LARGEOBJECT_ALLOCATED | COR_PRF_HIGH_MONITOR_PINNEDOBJECT_ALLOCATED)))
    {
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE GCAllocateProfiler::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    COR_PRF_GC_GENERATION_RANGE gen;
    HRESULT hr = pCorProfilerInfo->GetObjectGeneration(objectId, &gen);
    if (FAILED(hr))
    {
        printf("GetObjectGeneration failed hr=0x%x\n", hr);
        _failures++;
    }
    constexpr uint32_t LargeObjectHeapFlag = 1u << 2;
    constexpr uint32_t PinnedObjectHeapFlag = 1u << 3;
    constexpr uint32_t PlacementFlags = LargeObjectHeapFlag | PinnedObjectHeapFlag;

    uint32_t expectedAllocationFlag = 0;
    if (SUCCEEDED(hr) && gen.generation == COR_PRF_GC_LARGE_OBJECT_HEAP)
    {
        expectedAllocationFlag = LargeObjectHeapFlag;
        _gcLOHAllocations++;
    }
    else if (SUCCEEDED(hr) && gen.generation == COR_PRF_GC_PINNED_OBJECT_HEAP)
    {
        expectedAllocationFlag = PinnedObjectHeapFlag;
        _gcPOHAllocations++;
    }
    else if (SUCCEEDED(hr))
    {
        printf("Unexpected object allocation captured, gen.generation=0x%x\n", gen.generation);
        _failures++;
    }

    if ((_getAllocationNotificationCount != nullptr) && (expectedAllocationFlag != 0))
    {
        int64_t count = _getAllocationNotificationCount();
        bool found = false;
        uint32_t actualAllocationFlags = 0;
        for (int64_t index = count; index > 0; index--)
        {
            if (_getAllocationNotificationObject(index - 1) == reinterpret_cast<void*>(objectId))
            {
                found = true;
                actualAllocationFlags = _getAllocationNotificationFlags(index - 1);
                if (_placementFlagPerturbation == PlacementFlagPerturbation::Drop)
                {
                    actualAllocationFlags &= ~expectedAllocationFlag;
                }
                else if (_placementFlagPerturbation == PlacementFlagPerturbation::Swap)
                {
                    uint32_t placementFlags = actualAllocationFlags & PlacementFlags;
                    actualAllocationFlags &= ~PlacementFlags;
                    if ((placementFlags & LargeObjectHeapFlag) != 0)
                    {
                        actualAllocationFlags |= PinnedObjectHeapFlag;
                    }
                    if ((placementFlags & PinnedObjectHeapFlag) != 0)
                    {
                        actualAllocationFlags |= LargeObjectHeapFlag;
                    }
                }
                break;
            }
        }

        int64_t errors = _getAllocationNotificationErrorCount();
        if (!found || (errors != 0))
        {
            printf(
                "Allocation profiler callback preceded the allocation-complete callback "
                "(count=%lld, errors=%lld, expected=0x%x)\n",
                (long long)count,
                (long long)errors,
                expectedAllocationFlag);
            _failures++;
        }
        else if ((actualAllocationFlags & expectedAllocationFlag) == 0)
        {
            printf(
                "Allocation-complete callback placement mismatch "
                "(flags=0x%x, expected=0x%x)\n",
                actualAllocationFlags,
                expectedAllocationFlag);
            _failures++;
        }
    }

    return S_OK;
}

HRESULT GCAllocateProfiler::Shutdown()
{
    Profiler::Shutdown();
    if (_gcPOHAllocations == 0)
    {
        printf("There is no POH allocations\n");
    }
    else if (_gcLOHAllocations == 0)
    {
        printf("There is no LOH allocations\n");
    }
    else if (_failures == 0)
    {
        printf("%d LOH objects allocated\n", (int)_gcLOHAllocations);
        printf("%d POH objects allocated\n", (int)_gcPOHAllocations);
        printf("PROFILER TEST PASSES\n");
    }
    fflush(stdout);

    return S_OK;
}
