// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <minipal/utils.h>
#include <sospriv.h>
#include <sstring.h>
#include <clrhost.h>
#include "dbgutil.h"
#include "cdac.h"

#define CDAC_LIB_NAME MAKEDLLNAME_W(W("cdacreader"))

namespace
{
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

    int ReadFromTargetCallback(uint64_t addr, uint8_t* dest, uint32_t count, void* context)
    {
        CDAC* cdac = reinterpret_cast<CDAC*>(context);
        return cdac->ReadFromTarget(addr, dest, count);
    }
}

CDAC* CDAC::Create(uint64_t descriptorAddr, ICorDebugDataTarget* target)
{
    HMODULE cdacLib;
    if (!TryLoadCDACLibrary(&cdacLib))
        return nullptr;

    CDAC *impl = new (nothrow) CDAC{cdacLib, descriptorAddr, target};
    return impl;
}

CDAC::CDAC(HMODULE module, uint64_t descriptorAddr, ICorDebugDataTarget* target)
    : m_module(module)
    , m_target{target}
{
    m_init = reinterpret_cast<decltype(&cdac_reader_init)>(::GetProcAddress(m_module, "cdac_reader_init"));
    m_free = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(m_module, "cdac_reader_free"));
    m_getSosInterface = reinterpret_cast<decltype(&cdac_reader_get_sos_interface)>(::GetProcAddress(m_module, "cdac_reader_get_sos_interface"));

    m_init(descriptorAddr, &ReadFromTargetCallback, this, &m_cdac_handle);
    m_getSosInterface(m_cdac_handle, &m_sos);
}

CDAC::~CDAC()
{
    if (m_cdac_handle != NULL)
        m_free(m_cdac_handle);

    if (m_module != NULL)
        ::FreeLibrary(m_module);
}

IUnknown* CDAC::SosInterface()
{
    return m_sos;
}

int CDAC::ReadFromTarget(uint64_t addr, uint8_t* dest, uint32_t count)
{
    HRESULT hr = ReadFromDataTarget(m_target, addr, dest, count);
    if (FAILED(hr))
        return hr;

    return 0;
}
