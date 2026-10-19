// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>
#include <assert.h>
#include "minipal.h"

void* VMToOSInterface::AllocateMemory(void* address, size_t size, MemoryProtection protection)
{
    DWORD nativeProtection;
    switch (protection)
    {
        case MemoryProtection::NoAccess:
            nativeProtection = PAGE_NOACCESS;
            break;
        case MemoryProtection::ReadOnly:
            nativeProtection = PAGE_READONLY;
            break;
        case MemoryProtection::ReadWrite:
            nativeProtection = PAGE_READWRITE;
            break;
        case MemoryProtection::WriteCopy:
            nativeProtection = PAGE_WRITECOPY;
            break;
        case MemoryProtection::Execute:
            nativeProtection = PAGE_EXECUTE;
            break;
        case MemoryProtection::ExecuteRead:
            nativeProtection = PAGE_EXECUTE_READ;
            break;
        case MemoryProtection::ExecuteReadWrite:
            nativeProtection = PAGE_EXECUTE_READWRITE;
            break;
        default:
            assert(!"Unknown memory protection");
            nativeProtection = PAGE_NOACCESS;
            break;
    }

    return VirtualAlloc(address, size, MEM_RESERVE | MEM_COMMIT, nativeProtection);
}
