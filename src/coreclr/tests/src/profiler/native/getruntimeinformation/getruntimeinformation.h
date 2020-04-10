// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

class GetRuntimeInformation : public Profiler
{
public:
    GetRuntimeInformation();
    virtual ~GetRuntimeInformation();

    virtual GUID GetClsid() override;
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
    virtual HRESULT STDMETHODCALLTYPE Shutdown() override;

private:
    std::atomic<ULONG32> failures;
};
