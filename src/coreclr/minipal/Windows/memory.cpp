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

void* VMToOSInterface::AlignedAllocate(size_t alignment, size_t size)
{
    return _aligned_malloc(size, alignment);
}

void VMToOSInterface::AlignedFree(void *memblock)
{
    _aligned_free(memblock);
}
