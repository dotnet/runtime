// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// datatarget.cpp
//
// Implementation of the data-target memory-read callback declared in datatarget.h.
//*****************************************************************************

#include "datatarget.h"

namespace cdac
{
    bool ReadFromDataTarget(void* context, uint64_t address, void* buffer, uint32_t size)
    {
        ICLRDataTarget* target = (ICLRDataTarget*)context;
        ULONG32 read = 0;
        HRESULT hr = target->ReadVirtual((CLRDATA_ADDRESS)address, (PBYTE)buffer, size, &read);
        return SUCCEEDED(hr) && read == size;
    }
}
