// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cdac.h"
#include <sospriv.h>
#include <sstring.h>
#include "dbgutil.h"
#include <cdac_reader.h>

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

CDAC CDAC::Create(uint64_t descriptorAddr, ICorDebugDataTarget* target)
{
    HMODULE cdacLib;
    if (!TryLoadCDACLibrary(&cdacLib))
        return CDAC::Invalid();

    return CDAC{cdacLib, descriptorAddr, target};
}

CDAC::CDAC(HMODULE module, uint64_t descriptorAddr, ICorDebugDataTarget* target)
    : m_module(module)
    , m_target{target}
{
    if (m_module == NULL)
    {
        m_cdac_handle = NULL;
        return;
    }

    decltype(&cdac_reader_init) init = reinterpret_cast<decltype(&cdac_reader_init)>(::GetProcAddress(m_module, "cdac_reader_init"));
    decltype(&cdac_reader_get_sos_interface) getSosInterface = reinterpret_cast<decltype(&cdac_reader_get_sos_interface)>(::GetProcAddress(m_module, "cdac_reader_get_sos_interface"));
    _ASSERTE(init != nullptr && getSosInterface != nullptr);

    init(descriptorAddr, &ReadFromTargetCallback, this, &m_cdac_handle);
    getSosInterface(m_cdac_handle, &m_sos);
}

CDAC::~CDAC()
{
    if (m_cdac_handle != NULL)
    {
        decltype(&cdac_reader_free) free = reinterpret_cast<decltype(&cdac_reader_free)>(::GetProcAddress(m_module, "cdac_reader_free"));
        _ASSERTE(free != nullptr);
        free(m_cdac_handle);
    }

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

    return S_OK;
}
