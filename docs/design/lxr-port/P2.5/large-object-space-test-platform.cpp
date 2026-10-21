// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include <atomic>

namespace
{
    std::atomic<int64_t> CommitFailureAfter{-1};
}

void LxrTestFailCommitAfter(int64_t calls)
{
    CommitFailureAfter.store(calls, std::memory_order_relaxed);
}

#ifdef HOST_WINDOWS
#include <windows.h>
#else
#include <errno.h>
#include <sched.h>
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

void* GCToOSInterface::VirtualReserve(
    size_t size,
    size_t alignment,
    uint32_t flags,
    uint16_t node)
{
    (void)flags;
    (void)node;
#ifdef HOST_WINDOWS
    return VirtualAlloc(nullptr, size, MEM_RESERVE, PAGE_READWRITE);
#else
    size_t page = GetPageSize();
    if (alignment < page)
    {
        alignment = page;
    }
    if ((alignment & (alignment - 1)) != 0 || size > SIZE_MAX - alignment)
    {
        return nullptr;
    }
    int mmapFlags = MAP_ANON | MAP_PRIVATE;
#ifdef MAP_NORESERVE
    if ((flags & VirtualReserveFlags::NoReserve) != 0)
    {
        mmapFlags |= MAP_NORESERVE;
    }
#endif
    size_t reserved = size + alignment;
    void* result = mmap(nullptr, reserved, PROT_NONE, mmapFlags, -1, 0);
    if (result == MAP_FAILED)
    {
        return nullptr;
    }
    uintptr_t start = reinterpret_cast<uintptr_t>(result);
    uintptr_t aligned = (start + alignment - 1) & ~(alignment - 1);
    size_t prefix = aligned - start;
    size_t suffix = reserved - prefix - size;
    if (prefix != 0)
    {
        munmap(result, prefix);
    }
    if (suffix != 0)
    {
        munmap(reinterpret_cast<void*>(aligned + size), suffix);
    }
    return reinterpret_cast<void*>(aligned);
#endif
}

void* GCToOSInterface::VirtualReserveAt(
    void* address,
    size_t size,
    uint32_t flags,
    uint16_t node)
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
    int64_t remaining = CommitFailureAfter.load(std::memory_order_relaxed);
    if (remaining >= 0)
    {
        if (remaining == 0)
        {
            CommitFailureAfter.store(-1, std::memory_order_relaxed);
            return false;
        }
        CommitFailureAfter.fetch_sub(1, std::memory_order_relaxed);
    }
#ifdef HOST_WINDOWS
    return VirtualAlloc(address, size, MEM_COMMIT, PAGE_READWRITE) != nullptr;
#else
    return mprotect(address, size, PROT_READ | PROT_WRITE) == 0;
#endif
}

bool GCToOSInterface::VirtualDecommit(void* address, size_t size)
{
#ifdef HOST_WINDOWS
    return VirtualFree(address, size, MEM_DECOMMIT) != 0;
#else
    return (madvise(address, size, MADV_DONTNEED) == 0) &&
        (mprotect(address, size, PROT_NONE) == 0);
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

void GCToOSInterface::YieldThread(uint32_t switchCount)
{
    (void)switchCount;
#ifdef HOST_WINDOWS
    SwitchToThread();
#else
    sched_yield();
#endif
}
