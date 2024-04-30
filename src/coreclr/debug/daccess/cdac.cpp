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
        ICorDebugDataTarget* target = reinterpret_cast<ICorDebugDataTarget*>(context);
        HRESULT hr = ReadFromDataTarget(target, addr, dest, count);
        if (FAILED(hr))
            return hr;

        return S_OK;
    }
}

CDAC CDAC::Create(uint64_t descriptorAddr, ICorDebugDataTarget* target)
{
    HMODULE cdacLib;
    if (!TryLoadCDACLibrary(&cdacLib))
        return {};

    decltype(&cdac_reader_init) init = reinterpret_cast<decltype(&cdac_reader_init)>(::GetProcAddress(cdacLib, "cdac_reader_init"));
    _ASSERTE(init != nullptr);

    intptr_t handle;
    if (init(descriptorAddr, &ReadFromTargetCallback, target, &handle) != 0)
    {
        ::FreeLibrary(cdacLib);
        return {};
    }

    return CDAC{cdacLib, handle, target};
}

CDAC::CDAC(HMODULE module, intptr_t handle, ICorDebugDataTarget* target)
    : m_module{module}
    , m_cdac_handle{handle}
    , m_target{target}
{
    _ASSERTE(m_module != NULL && m_cdac_handle != 0 && m_target != NULL);

    m_target->AddRef();
    decltype(&cdac_reader_get_sos_interface) getSosInterface = reinterpret_cast<decltype(&cdac_reader_get_sos_interface)>(::GetProcAddress(m_module, "cdac_reader_get_sos_interface"));
    _ASSERTE(getSosInterface != nullptr);
    getSosInterface(m_cdac_handle, &m_sos);
}

CDAC::~CDAC()
{
    if (m_cdac_handle)
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
