// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"

#include "utilcode.h"
#include "corjit.h"
#include "jithost.h"

void* JitHost::allocateMemory(size_t size, bool usePageAllocator)
{
    WRAPPER_NO_CONTRACT;

    if (usePageAllocator)
    {
        return GetEEMemoryManager()->ClrVirtualAlloc(nullptr, size, MEM_COMMIT, PAGE_READWRITE);
    }
    else
    {
        return ClrAllocInProcessHeap(0, S_SIZE_T(size));
    }
}

void JitHost::freeMemory(void* block, bool usePageAllocator)
{
    WRAPPER_NO_CONTRACT;

    if (usePageAllocator)
    {
        GetEEMemoryManager()->ClrVirtualFree(block, 0, MEM_RELEASE);
    }
    else
    {
        ClrFreeInProcessHeap(0, block);
    }
}

int JitHost::getIntConfigValue(const wchar_t* name, int defaultValue)
{
    WRAPPER_NO_CONTRACT;

    // Translate JIT call into runtime configuration query
    CLRConfig::ConfigDWORDInfo info{ name, defaultValue, CLRConfig::EEConfig_default };

    // Perform a CLRConfig look up on behalf of the JIT.
    return CLRConfig::GetConfigValue(info);
}

const wchar_t* JitHost::getStringConfigValue(const wchar_t* name)
{
    WRAPPER_NO_CONTRACT;

    // Translate JIT call into runtime configuration query
    CLRConfig::ConfigStringInfo info{ name, CLRConfig::EEConfig_default };

    // Perform a CLRConfig look up on behalf of the JIT.
    return CLRConfig::GetConfigValue(info);
}

void JitHost::freeStringConfigValue(const wchar_t* value)
{
    WRAPPER_NO_CONTRACT;

    CLRConfig::FreeConfigString(const_cast<wchar_t*>(value));
}

JitHost JitHost::theJitHost;
ICorJitHost* JitHost::getJitHost()
{
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;

    return &theJitHost;
}
