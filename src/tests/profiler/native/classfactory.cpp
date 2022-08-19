// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "classfactory.h"
#include "eltprofiler/slowpatheltprofiler.h"
#include "eventpipeprofiler/eventpipereadingprofiler.h"
#include "eventpipeprofiler/eventpipewritingprofiler.h"
#include "getappdomainstaticaddress/getappdomainstaticaddress.h"
#include "gcallocateprofiler/gcallocateprofiler.h"
#include "gcbasicprofiler/gcbasicprofiler.h"
#include "gcprofiler/gcprofiler.h"
#include "handlesprofiler/handlesprofiler.h"
#include "metadatagetdispenser/metadatagetdispenser.h"
#include "nullprofiler/nullprofiler.h"
#include "rejitprofiler/rejitprofiler.h"
#include "releaseondetach/releaseondetach.h"
#include "transitions/transitions.h"
#include "multiple/multiple.h"
#include "inlining/inlining.h"

ClassFactory::ClassFactory(REFCLSID clsid) : refCount(0), clsid(clsid)
{
}

ClassFactory::~ClassFactory()
{
}

HRESULT STDMETHODCALLTYPE ClassFactory::QueryInterface(REFIID riid, void **ppvObject)
{
    if (riid == IID_IUnknown || riid == IID_IClassFactory)
    {
        *ppvObject = this;
        this->AddRef();
        return S_OK;
    }

    *ppvObject = nullptr;
    return E_NOINTERFACE;
}

ULONG STDMETHODCALLTYPE ClassFactory::AddRef()
{
    return std::atomic_fetch_add(&this->refCount, 1) + 1;
}

ULONG STDMETHODCALLTYPE ClassFactory::Release()
{
    int count = std::atomic_fetch_sub(&this->refCount, 1) - 1;
    if (count <= 0)
    {
        delete this;
    }

    return count;
}

HRESULT STDMETHODCALLTYPE ClassFactory::CreateInstance(IUnknown *pUnkOuter, REFIID riid, void **ppvObject)
{
    if (pUnkOuter != nullptr)
    {
        *ppvObject = nullptr;
        return CLASS_E_NOAGGREGATION;
    }

	Profiler* profiler = nullptr;
    if (clsid == GCAllocateProfiler::GetClsid())
    {
        profiler = new GCAllocateProfiler();
    }
    else if (clsid == GCBasicProfiler::GetClsid())
    {
        profiler = new GCBasicProfiler();
    }
    else if (clsid == ReJITProfiler::GetClsid())
    {
        profiler = new ReJITProfiler();
    }
    else if (clsid == EventPipeReadingProfiler::GetClsid())
    {
        profiler = new EventPipeReadingProfiler();
    }
    else if (clsid == EventPipeWritingProfiler::GetClsid())
    {
        profiler = new EventPipeWritingProfiler();
    }
    else if (clsid == MetaDataGetDispenser::GetClsid())
    {
        profiler = new MetaDataGetDispenser();
    }
    else if (clsid == GetAppDomainStaticAddress::GetClsid())
    {
        profiler = new GetAppDomainStaticAddress();
    }
    else if (clsid == SlowPathELTProfiler::GetClsid())
    {
        profiler = new SlowPathELTProfiler();
    }
    else if (clsid == GCProfiler::GetClsid())
    {
        profiler = new GCProfiler();
    }
    else if (clsid == ReleaseOnDetach::GetClsid())
    {
        profiler = new ReleaseOnDetach();
    }
    else if (clsid == Transitions::GetClsid())
    {
        profiler = new Transitions();
    }
    else if (clsid == NullProfiler::GetClsid())
    {
        profiler = new NullProfiler();
    }
    else if (clsid == MultiplyLoaded::GetClsid())
    {
        profiler = new MultiplyLoaded();
    }
    else if (clsid == InliningProfiler::GetClsid())
    {
        profiler = new InliningProfiler();
    }
    else if (clsid == HandlesProfiler::GetClsid())
    {
        profiler = new HandlesProfiler();
    }
    else
    {
        printf("No profiler found in ClassFactory::CreateInstance. Did you add your profiler to the list?\n");
        return E_FAIL;
    }

    return profiler->QueryInterface(riid, ppvObject);
}

HRESULT STDMETHODCALLTYPE ClassFactory::LockServer(BOOL fLock)
{
    return S_OK;
}
