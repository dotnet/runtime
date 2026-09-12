// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// datatarget.cpp
//
// Implementation of the data-target memory-read callback declared in datatarget.h.
//*****************************************************************************

#include "datatarget.h"

#include <crosscomp.h>
#include <dbgtargetcontext.h>
#include <cstring>

namespace cdac
{
    bool ReadFromDataTarget(void* context, uint64_t address, void* buffer, uint32_t size)
    {
        ICLRDataTarget* target = (ICLRDataTarget*)context;
        ULONG32 read = 0;
        HRESULT hr = target->ReadVirtual((CLRDATA_ADDRESS)address, (PBYTE)buffer, size, &read);
        return SUCCEEDED(hr) && read == size;
    }

    bool ReadThreadContextFromDataTarget(void* context, uint64_t osThreadId, ThreadContext& threadContext)
    {
        ICLRDataTarget* target = static_cast<ICLRDataTarget*>(context);
        DT_CONTEXT targetContext;
        memset(&targetContext, 0, sizeof(targetContext));
        HRESULT hr = target->GetThreadContext(
            static_cast<ULONG32>(osThreadId),
            DT_CONTEXT_ALL,
            sizeof(targetContext),
            reinterpret_cast<PBYTE>(&targetContext));
        if (FAILED(hr))
        {
            return false;
        }

#if defined(HOST_AMD64)
        threadContext.instructionPointer = targetContext.Rip;
#elif defined(HOST_X86)
        threadContext.instructionPointer = targetContext.Eip;
#elif defined(HOST_ARM) || defined(HOST_ARM64) || defined(HOST_LOONGARCH64) || defined(HOST_RISCV64)
        threadContext.instructionPointer = targetContext.Pc;
#else
        threadContext.instructionPointer = 0;
#endif

        threadContext.registerValueCount = 0;
        const uint8_t* bytes = reinterpret_cast<const uint8_t*>(&targetContext);
        for (uint32_t offset = 0;
             offset + sizeof(void*) <= sizeof(targetContext) &&
                 threadContext.registerValueCount < ThreadContext::MaxRegisterValues;
             offset += sizeof(void*))
        {
            uint64_t value = 0;
            memcpy(&value, bytes + offset, sizeof(void*));
            threadContext.registerValues[threadContext.registerValueCount++] = value;
        }
        return true;
    }
}
