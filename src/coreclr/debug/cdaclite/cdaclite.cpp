// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// cdaclite.cpp
//
// Creation: the classic DAC factory entry point (CLRDataCreateInstance) and the
// CDacLite COM object lifetime (construction, IUnknown, logging). The data-target
// source lives in datatarget.{h,cpp} and the memory enumeration in enumerate.cpp.
//*****************************************************************************

#include "cdaclite.h"
#include "datatarget.h"

#include <dbgutil.h>
#include "corerror.h"
#include <palclr.h>
#include <crosscomp.h>
#include <xclrdata.h>

#include <stdio.h>
#include <memory>

// Implemented per-platform in dbgutil (dbgutil.cpp / elfreader.cpp / machoreader.cpp).
// Resolves an exported symbol in the target image using only the data target.
extern "C" bool TryGetSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress);

namespace cdac
{
    CDacLite::CDacLite(ICLRDataTarget* target, uint64_t contractDescriptorAddr)
        : m_ref(1), m_target(target), m_contractDescriptorAddr(contractDescriptorAddr)
    {
        m_target->AddRef();
    }

    CDacLite::~CDacLite()
    {
        m_target->Release();
    }

    HRESULT STDMETHODCALLTYPE CDacLite::QueryInterface(REFIID riid, void** ppvObject)
    {
        if (ppvObject == nullptr)
        {
            return E_POINTER;
        }
        if (riid == IID_IUnknown || riid == __uuidof(ICLRDataEnumMemoryRegions))
        {
            *ppvObject = static_cast<ICLRDataEnumMemoryRegions*>(this);
            AddRef();
            return S_OK;
        }
        *ppvObject = nullptr;
        return E_NOINTERFACE;
    }

    ULONG STDMETHODCALLTYPE CDacLite::AddRef()
    {
        return InterlockedIncrement(&m_ref);
    }

    ULONG STDMETHODCALLTYPE CDacLite::Release()
    {
        LONG ref = InterlockedDecrement(&m_ref);
        if (ref == 0)
        {
            delete this;
        }
        return ref;
    }

    void CDacLite::Log(ICLRDataEnumMemoryRegionsCallback* callback, const char* format, ...)
    {
        char buffer[1024];
        va_list args;
        va_start(args, format);
        vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);
        buffer[sizeof(buffer) - 1] = '\0';

        ICLRDataLoggingCallback* logger = nullptr;
        if (callback != nullptr &&
            SUCCEEDED(callback->QueryInterface(__uuidof(ICLRDataLoggingCallback), (void**)&logger)) &&
            logger != nullptr)
        {
            logger->LogMessage(buffer);
            logger->Release();
        }
        else
        {
            fprintf(stderr, "cdaclite: %s\n", buffer);
#ifdef HOST_WINDOWS
            OutputDebugStringA("cdaclite: ");
            OutputDebugStringA(buffer);
            OutputDebugStringA("\n");
#endif
        }
    }
}

//
// The classic DAC factory entry point. dbghelp (Windows) and createdump
// (Unix/macOS) call this to obtain an ICLRDataEnumMemoryRegions for a target.
//
STDAPI CLRDataCreateInstance(REFIID iid, ICLRDataTarget* pLegacyTarget, void** iface)
{
    if (pLegacyTarget == nullptr || iface == nullptr)
    {
        return E_INVALIDARG;
    }

    *iface = nullptr;

    // Determine the runtime module base, preferring ICLRRuntimeLocator and
    // falling back to the well-known CLR module name.
    CLRDATA_ADDRESS base = 0;
    ICLRRuntimeLocator* locator = nullptr;
    if (SUCCEEDED(pLegacyTarget->QueryInterface(__uuidof(ICLRRuntimeLocator), (void**)&locator)) &&
        locator != nullptr &&
        locator->GetRuntimeBase(&base) == S_OK)
    {
        locator->Release();
    }
    else
    {
        if (locator != nullptr)
        {
            locator->Release();
        }
        HRESULT hr = pLegacyTarget->GetImageBase(TARGET_MAIN_CLR_DLL_NAME_W, &base);
        if (FAILED(hr))
        {
            return hr;
        }
    }

    // Locate the exported contract descriptor in the target.
    cdac::DataTargetAdapter adapter(pLegacyTarget);
    uint64_t contractDescriptorAddr = 0;
    if (!TryGetSymbol(&adapter, (uint64_t)base, "DotNetRuntimeContractDescriptor", &contractDescriptorAddr) ||
        contractDescriptorAddr == 0)
    {
        return E_FAIL;
    }

    cdac::CDacLite* instance = new (std::nothrow) cdac::CDacLite(pLegacyTarget, contractDescriptorAddr);
    if (instance == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    HRESULT hr = instance->QueryInterface(iid, iface);
    instance->Release();
    return hr;
}
