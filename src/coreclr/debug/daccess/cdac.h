// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDAC_H
#define CDAC_H

#include <cdac_reader.h>
#include <holder.h>

class CDAC final
{
public:
    static CDAC* Create(uint64_t descriptorAddr, ICorDebugDataTarget *pDataTarget);

public:
    ~CDAC();
    IUnknown* SosInterface();
    int ReadFromTarget(uint64_t addr, uint8_t* dest, uint32_t count);

private:
    explicit CDAC(HMODULE module, uint64_t descriptorAddr, ICorDebugDataTarget* target);

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

#endif // CDAC_H
