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
#include "metadatagetdispenser/metadatagetdispenser.h"
#include "nullprofiler/nullprofiler.h"
#include "rejitprofiler/rejitprofiler.h"
#include "releaseondetach/releaseondetach.h"
#include "transitions/transitions.h"

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

	//A little simplistic, we create an instance of every profiler, then return the one whose CLSID matches
	Profiler* profilers[] = {
        new GCAllocateProfiler(),
		new GCBasicProfiler(),
        new ReJITProfiler(),
        new EventPipeReadingProfiler(),
        new EventPipeWritingProfiler(),
        new MetaDataGetDispenser(),
        new GetAppDomainStaticAddress(),
        new SlowPathELTProfiler(),
        new GCProfiler(),
        new ReleaseOnDetach(),
        new Transitions(),
        new NullProfiler()
		// add new profilers here
	};

	Profiler* profiler = nullptr;
	for (unsigned int i = 0; i < sizeof(profilers)/sizeof(Profiler*); i++)
	{
		if (clsid == profilers[i]->GetClsid())
		{
			profiler = profilers[i];
			break;
		}
	}

	if (profiler == nullptr)
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
