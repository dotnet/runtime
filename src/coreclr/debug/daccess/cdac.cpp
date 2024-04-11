// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <minipal/utils.h>
#include <sospriv.h>
#include <sstring.h>
#include <clrhost.h>
#include "dbgutil.h"
#include "cdac_reader.h"
#include "cdac.h"

namespace
{
    #define CDAC_LIB_NAME MAKEDLLNAME_W(W("cdacreader"))
    bool TryLoadCDACLibrary(HMODULE *phCDAC)
    {
        // Load cdacreader from next to DAC binary
        PathString path;
        if (FAILED(GetClrModuleDirectory(path)))
            return false;

        path.Append(CDAC_LIB_NAME);
        *phCDAC = CLRLoadLibrary(path.GetUnicode());
        if (*phCDAC == NULL)
            return false;

        return true;
    }
}

class CDACImpl final {
public:
    explicit CDACImpl(HMODULE module, uint64_t descriptorAddr, ICorDebugDataTarget* target);
    CDACImpl(const CDACImpl&) = delete;
    CDACImpl& operator=(CDACImpl&) = delete;

    CDACImpl(CDACImpl&& other)
        : m_module(other.m_module)
        , m_cdac_handle{other.m_cdac_handle}
        , m_target{other.m_target}
        , m_init{other.m_init}
        , m_free{other.m_free}
        , m_getSosInterface{other.m_getSosInterface}
    {
        other.m_module = nullptr;
    	other.m_cdac_handle = 0;
        other.m_target = nullptr;
    }

    // Returns the SOS interface. This does not AddRef the interface.
    IUnknown* SosInterface() const
    {
        return m_sos;
    }

    int ReadFromTarget(uint64_t addr, uint8_t* dest, uint32_t count)
    {
        HRESULT hr = ReadFromDataTarget(m_target, addr, dest, count);
        if (FAILED(hr))
            return hr;

        return 0;
    }

public:
    ~CDACImpl()
    {
        if (m_cdac_handle != NULL)
            m_free(m_cdac_handle);

        if (m_module != NULL)
            ::FreeLibrary(m_module);
    }

private:
    HMODULE m_module;
    intptr_t m_cdac_handle;
    ICorDebugDataTarget* m_target;
    NonVMComHolder<IUnknown> m_sos;

private:
    decltype(&cdac_reader_init) m_init;
    decltype(&cdac_reader_free) m_free;
    decltype(&cdac_reader_get_sos_interface) m_getSosInterface;
};

namespace
{
	int ReadFromTargetCallback(uint64_t addr, uint8_t* dest, uint32_t count, void* context)
	{
	    CDACImpl* cdac = reinterpret_cast<CDACImpl*>(context);
	    return cdac->ReadFromTarget(addr, dest, count);
	}
}

CDACImpl::CDACImpl(HMODULE module, uint64_t descriptorAddr, ICorDebugDataTarget* target)
    : m_module(module)
    , m_target{target}
{
    m_init = reinterpret_cast<decltype(&cdac_reader_init)>(::GetProcAddress(m_module, "cdac_reader_init"));
    m_free = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(m_module, "cdac_reader_free"));
    m_getSosInterface = reinterpret_cast<decltype(&cdac_reader_get_sos_interface)>(::GetProcAddress(m_module, "cdac_reader_get_sos_interface"));

    m_init(descriptorAddr, &ReadFromTargetCallback, this, &m_cdac_handle);
    m_getSosInterface(m_cdac_handle, &m_sos);
}


const CDAC* CDAC::Create(uint64_t descriptorAddr, ICorDebugDataTarget* target)
{
    HRESULT hr = S_OK;

    HMODULE cdacLib;
    if (!TryLoadCDACLibrary(&cdacLib))
        return nullptr;

    CDACImpl *impl = new (nothrow) CDACImpl{cdacLib, descriptorAddr, target};
    if (!impl)
        return nullptr;

    CDAC *cdac = new (nothrow) CDAC(impl);
    if (!cdac)
    {
        delete impl;
        return nullptr;
    }

    return cdac;
}

CDAC::CDAC(CDACImpl *impl) : m_impl(impl)
{
}

IUnknown* CDAC::SosInterface() const
{
    return m_impl->SosInterface();
}

CDAC::~CDAC()
{
    delete m_impl;
}
