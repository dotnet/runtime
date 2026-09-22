// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "multiple.h"
#include <thread>

#define MAX_PROFILERS 3
#define FAIL_PROFILER_INITIALIZATION 1

using std::thread;

std::atomic<int> MultiplyLoaded::_exceptionThrownSeenCount(0);
std::atomic<int> MultiplyLoaded::_detachCount(0);
std::atomic<int> MultiplyLoaded::_failures(0);
std::atomic<bool> MultiplyLoaded::_waitingForAllocationCallback(false);
AutoEvent MultiplyLoaded::_allocationCallbackStarted;

GUID MultiplyLoaded::GetClsid()
{
    // {BFA8EF13-E144-49B9-B95C-FC1C150C7651}
    GUID clsid = { 0xBFA8EF13, 0xE144, 0x49B9, { 0xB9, 0x5C, 0xFC, 0x1C, 0x15, 0x0C, 0x76, 0x51 } };
    return clsid;
}

HRESULT MultiplyLoaded::InitializeCommon(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_EXCEPTIONS, 0)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x", hr);
        return hr;
    }

    return S_OK;
}

HRESULT MultiplyLoaded::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    printf("MultiplyLoaded::Initialize\n");
    return InitializeCommon(pICorProfilerInfoUnk);
}

HRESULT MultiplyLoaded::InitializeForAttach(IUnknown* pICorProfilerInfoUnk, void* pvClientData, UINT cbClientData)
{
    printf("MultiplyLoaded::InitializeForAttach\n");

    if (pvClientData != nullptr &&
        cbClientData == 1 &&
        *static_cast<BYTE*>(pvClientData) == FAIL_PROFILER_INITIALIZATION)
    {
        Profiler::Initialize(pICorProfilerInfoUnk);
        _waitingForAllocationCallback = true;
        _allocationCallbackStarted.Wait();
        _waitingForAllocationCallback = false;
        return E_FAIL;
    }

    return InitializeCommon(pICorProfilerInfoUnk);
}

bool MultiplyLoaded::IsWaitingForAllocationCallback()
{
    return _waitingForAllocationCallback.load();
}

void MultiplyLoaded::SignalAllocationCallbackStarted()
{
    _allocationCallbackStarted.Signal();
}

extern "C" EXPORT BOOL STDMETHODCALLTYPE IsNotificationProfilerWaitingForAllocationCallback()
{
    return MultiplyLoaded::IsWaitingForAllocationCallback() ? TRUE : FALSE;
}

HRESULT MultiplyLoaded::LoadAsNotificationOnly(BOOL *pbNotificationOnly)
{
    *pbNotificationOnly = TRUE;
    return S_OK;
}

HRESULT MultiplyLoaded::ProfilerDetachSucceeded()
{
    ++_detachCount;

    printf("ProfilerDetachSucceeded _detachCount=%d\n", _detachCount.load());
    if (_detachCount == MAX_PROFILERS)
    {
        if (_exceptionThrownSeenCount >= MAX_PROFILERS &&  _failures == 0)
        {
            printf("PROFILER TEST PASSES\n");
            fflush(stdout);
        }

        NotifyManagedCodeViaCallback(pCorProfilerInfo);
    }

    return S_OK;
}

HRESULT MultiplyLoaded::ExceptionThrown(ObjectID thrownObjectId)
{
    printf("MultiplyLoaded::ExceptionThrown, number seen = %d\n", ++_exceptionThrownSeenCount);

    thread detachThread([&]()
        {
            printf("Requesting detach!!\n");
            HRESULT hr = pCorProfilerInfo->RequestProfilerDetach(0);
            printf("RequestProfilerDetach hr=0x%x\n", hr);
        });

    detachThread.detach();

    return S_OK;
}

HRESULT MultiplyLoaded::Shutdown()
{
    Profiler::Shutdown();

    fflush(stdout);

    return S_OK;
}
