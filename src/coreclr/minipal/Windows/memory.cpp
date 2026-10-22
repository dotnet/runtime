// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <windows.h>
#include <inttypes.h>
#include <assert.h>
#include <malloc.h>
#include "minipal.h"

VMToOSInterface::MemoryMapHandle VMToOSInterface::OpenFile(const char16_t* path)
{
    return CreateFileW(reinterpret_cast<LPCWSTR>(path),
        GENERIC_READ | GENERIC_EXECUTE,
        FILE_SHARE_READ | FILE_SHARE_DELETE,
        nullptr,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);
}

bool VMToOSInterface::CloseFile(MemoryMapHandle fileHandle)
{
    return CloseHandle(fileHandle) != FALSE;
}

bool VMToOSInterface::GetFileSize(MemoryMapHandle fileHandle, uint64_t* size)
{
    LARGE_INTEGER fileSize;
    if (!GetFileSizeEx(fileHandle, &fileSize) || fileSize.QuadPart < 0)
    {
        return false;
    }

    *size = static_cast<uint64_t>(fileSize.QuadPart);
    return true;
}

void* VMToOSInterface::AllocateMemory(void* address, size_t size, MemoryProtection protection, bool)
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

void* VMToOSInterface::AlignedAllocate(size_t alignment, size_t size)
{
    return _aligned_malloc(size, alignment);
}

void VMToOSInterface::AlignedFree(void *memblock)
{
    _aligned_free(memblock);
}
