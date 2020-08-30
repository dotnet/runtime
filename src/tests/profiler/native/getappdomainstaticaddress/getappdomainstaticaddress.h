// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"

#include <atomic>
#include <memory>
#include <set>
#include <mutex>
#include <vector>
#include <map>
#include <string>
#include <thread>
#include <chrono>
#include <condition_variable>
#include <functional>
#include "cor.h"
#include "corprof.h"

typedef HRESULT (*GetDispenserFunc) (const CLSID &pClsid, const IID &pIid, void **ppv);

class ManualEvent
{
private:
    std::mutex m_mtx;
    std::condition_variable m_cv;
    bool m_set = false;

    static void DoNothing()
    {

    }

public:
    ManualEvent() = default;
    ~ManualEvent() = default;
    ManualEvent(ManualEvent& other) = delete;
    ManualEvent(ManualEvent&& other) = delete;
    ManualEvent& operator= (ManualEvent& other) = delete;
    ManualEvent& operator= (ManualEvent&& other) = delete;

    void Wait(std::function<void()> spuriousCallback = DoNothing)
    {
        std::unique_lock<std::mutex> lock(m_mtx);
        while (!m_set)
        {
            m_cv.wait(lock, [&]() { return m_set; });
            if (!m_set)
            {
                spuriousCallback();
            }
        }
    }

    void Signal()
    {
        std::unique_lock<std::mutex> lock(m_mtx);
        m_set = true;
    }

    void Reset()
    {
        std::unique_lock<std::mutex> lock(m_mtx);
        m_set = false;
    }
};

class GetAppDomainStaticAddress : public Profiler
{
public:
    GetAppDomainStaticAddress();
    virtual ~GetAppDomainStaticAddress();

    virtual GUID GetClsid() override;
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
    virtual HRESULT STDMETHODCALLTYPE Shutdown() override;

    virtual HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override;
    virtual HRESULT STDMETHODCALLTYPE ModuleUnloadStarted(ModuleID moduleId) override;
    virtual HRESULT STDMETHODCALLTYPE ClassLoadFinished(ClassID classId, HRESULT hrStatus) override;
    virtual HRESULT STDMETHODCALLTYPE ClassUnloadStarted(ClassID classId) override;
    virtual HRESULT STDMETHODCALLTYPE JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
    virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override;

private:
    std::atomic<int> refCount;
    std::atomic<ULONG32> failures;
    std::atomic<ULONG32> successes;
    std::atomic<ULONG32> collectibleCount;
    std::atomic<ULONG32> nonCollectibleCount;

    std::atomic<int> jitEventCount;
    std::thread gcTriggerThread;
    ManualEvent gcWaitEvent;

    typedef std::map<ClassID, AppDomainID>ClassAppDomainMap;
    ClassAppDomainMap classADMap;
    std::mutex classADMapLock;

    bool IsRuntimeExecutingManagedCode();
    std::vector<ClassID> GetGenericTypeArgs(ClassID classId);
};
