// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cdac.h"
#include <sospriv.h>
#include <sstring.h>
#include "dbgutil.h"
#include <cdac_reader.h>

#define CDAC_LIB_NAME MAKEDLLNAME_W(W("mscordaccore_universal"))

namespace
{
    bool TryLoadCDACLibrary(HMODULE *phCDAC)
    {
        // Load cdac from next to current module (DAC binary)
        PathString path;
        if (WszGetModuleFileName((HMODULE)GetCurrentModuleBase(), path) == 0)
            return false;

        SString::Iterator iter = path.End();
        if (!path.FindBack(iter, DIRECTORY_SEPARATOR_CHAR_W))
            return false;

        iter++;
        path.Truncate(iter);
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

    int WriteToTargetCallback(uint64_t addr, const uint8_t* buff, uint32_t count, void* context)
    {
        ICorDebugMutableDataTarget* target = static_cast<ICorDebugMutableDataTarget*>(context);
        HRESULT hr = target->WriteVirtual((CORDB_ADDRESS)addr, buff, count);
        if (FAILED(hr))
            return hr;

        return S_OK;
    }

    int ReadThreadContext(uint32_t threadId, uint32_t contextFlags, uint32_t contextBufferSize, uint8_t* contextBuffer, void* context)
    {
        ICorDebugDataTarget* target = reinterpret_cast<ICorDebugDataTarget*>(context);
        HRESULT hr = target->GetThreadContext(threadId, contextFlags, contextBufferSize, contextBuffer);
        if (FAILED(hr))
            return hr;

        return S_OK;
    }
}

CDAC CDAC::Create(uint64_t descriptorAddr, ICorDebugMutableDataTarget* target, IUnknown* legacyImpl)
{
    HMODULE cdacLib;
    if (!TryLoadCDACLibrary(&cdacLib))
        return {};

    decltype(&cdac_reader_init) init = reinterpret_cast<decltype(&cdac_reader_init)>(::GetProcAddress(cdacLib, "cdac_reader_init"));
    _ASSERTE(init != nullptr);

    intptr_t handle;
    if (init(descriptorAddr, &ReadFromTargetCallback, &WriteToTargetCallback, &ReadThreadContext, target, &handle) != 0)
    {
        ::FreeLibrary(cdacLib);
        return {};
    }

    return CDAC{cdacLib, handle, target, legacyImpl};
}

CDAC::CDAC(HMODULE module, intptr_t handle, ICorDebugDataTarget* target, IUnknown* legacyImpl)
    : m_module{module}
    , m_cdac_handle{handle}
    , m_target{target}
    , m_legacyImpl{legacyImpl}
{
    _ASSERTE(m_module != NULL && m_cdac_handle != 0 && m_target != NULL);

    m_target->AddRef();
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

void CDAC::CreateSosInterface(IUnknown** sos)
{
    decltype(&cdac_reader_create_sos_interface) createSosInterface = reinterpret_cast<decltype(&cdac_reader_create_sos_interface)>(::GetProcAddress(m_module, "cdac_reader_create_sos_interface"));
    _ASSERTE(createSosInterface != nullptr);
    int ret = createSosInterface(m_cdac_handle, m_legacyImpl, sos);
    _ASSERTE(ret == 0);
}
