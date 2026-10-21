// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"

#ifdef HOST_WINDOWS
#include <windows.h>
#else
#include <errno.h>
#include <sys/mman.h>
#include <unistd.h>
#endif

size_t GCToOSInterface::GetPageSize()
{
#ifdef HOST_WINDOWS
    SYSTEM_INFO info;
    GetSystemInfo(&info);
    return info.dwPageSize;
#else
    return static_cast<size_t>(sysconf(_SC_PAGESIZE));
#endif
}

void* GCToOSInterface::VirtualReserveAt(void* address, size_t size, uint32_t flags, uint16_t node)
{
    (void)node;
#ifdef HOST_WINDOWS
    (void)flags;
    void* result = VirtualAlloc(address, size, MEM_RESERVE, PAGE_READWRITE);
    if ((result != nullptr) && (result != address))
    {
        VirtualFree(result, 0, MEM_RELEASE);
        return nullptr;
    }
    return result;
#else
    int mmapFlags = MAP_ANON | MAP_PRIVATE;
#ifdef MAP_NORESERVE
    if ((flags & VirtualReserveFlags::NoReserve) != 0)
    {
        mmapFlags |= MAP_NORESERVE;
    }
#endif
#ifdef MAP_FIXED_NOREPLACE
    void* result = mmap(address, size, PROT_NONE, mmapFlags | MAP_FIXED_NOREPLACE, -1, 0);
    if ((result == MAP_FAILED) && (errno == EINVAL))
    {
        result = mmap(address, size, PROT_NONE, mmapFlags, -1, 0);
    }
#else
    void* result = mmap(address, size, PROT_NONE, mmapFlags, -1, 0);
#endif
    if (result == MAP_FAILED)
    {
        return nullptr;
    }
    if (result != address)
    {
        munmap(result, size);
        return nullptr;
    }
    return result;
#endif
}

bool GCToOSInterface::VirtualCommit(void* address, size_t size, uint16_t node)
{
    (void)node;
#ifdef HOST_WINDOWS
    return VirtualAlloc(address, size, MEM_COMMIT, PAGE_READWRITE) != nullptr;
#else
    return mprotect(address, size, PROT_READ | PROT_WRITE) == 0;
#endif
}

bool GCToOSInterface::VirtualRelease(void* address, size_t size)
{
#ifdef HOST_WINDOWS
    (void)size;
    return VirtualFree(address, 0, MEM_RELEASE) != 0;
#else
    return munmap(address, size) == 0;
#endif
}
