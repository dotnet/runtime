// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//

#include <inttypes.h>
#include <stdlib.h>
#include "minipal.h"

#if defined(HOST_OSX) && defined(HOST_ARM64)
#include <assert.h>
#include <pthread.h>

void VMToOSInterface::JitWriteProtect(bool writeEnable)
{
    thread_local int enabledCount = 0;
    if (writeEnable)
    {
        if (enabledCount++ == 0)
        {
            pthread_jit_write_protect_np(0);
        }
    }
    else
    {
        if (--enabledCount == 0)
        {
            pthread_jit_write_protect_np(1);
        }
        assert(enabledCount >= 0);
    }
}
#endif // HOST_OSX && HOST_ARM64

#ifdef TARGET_WASM
#include <errno.h>
#include <string.h>
#include <minipal/ospagesize.h>

static bool IsValidMemoryRange(void* address, size_t size)
{
    size_t pageSize = minipal_getpagesize();
    if (address == nullptr || size == 0 || size % pageSize != 0 ||
        reinterpret_cast<uintptr_t>(address) % pageSize != 0 ||
        size > SIZE_MAX - reinterpret_cast<uintptr_t>(address))
    {
        errno = EINVAL;
        return false;
    }

    return true;
}

void* VMToOSInterface::AllocateMemory(void* address, size_t size, MemoryProtection protection)
{
    size_t pageSize = minipal_getpagesize();
    if (size == 0 || size % pageSize != 0)
    {
        errno = EINVAL;
        return nullptr;
    }

    if (protection != MemoryProtection::NoAccess && protection != MemoryProtection::ReadWrite)
    {
        errno = ENOTSUP;
        return nullptr;
    }

    // WASM cannot honor address hints or enforce page protections.
    (void)address;
    void* memory = nullptr;
    int error = posix_memalign(&memory, pageSize, size);
    if (error != 0)
    {
        errno = error;
        return nullptr;
    }

    memset(memory, 0, size);
    return memory;
}

void* VMToOSInterface::ReserveMemoryWithinRange(void* rangeStart, void* rangeEnd, size_t size)
{
    uintptr_t start = reinterpret_cast<uintptr_t>(rangeStart);
    uintptr_t end = reinterpret_cast<uintptr_t>(rangeEnd);
    if (rangeStart == nullptr && rangeEnd == nullptr)
    {
        end = SIZE_MAX;
    }
    else if (end <= start)
    {
        errno = EINVAL;
        return nullptr;
    }

    void* memory = AllocateMemory(nullptr, size, MemoryProtection::NoAccess);
    if (memory == nullptr)
    {
        return nullptr;
    }

    uintptr_t base = reinterpret_cast<uintptr_t>(memory);
    if (base < start || base >= end || size > end - base)
    {
        free(memory);
        errno = ENOMEM;
        return nullptr;
    }

    return memory;
}

void* VMToOSInterface::CommitReservedMemory(void* address, size_t size, MemoryProtection protection)
{
    if (!IsValidMemoryRange(address, size))
    {
        return nullptr;
    }

    if (protection != MemoryProtection::ReadWrite)
    {
        errno = ENOTSUP;
        return nullptr;
    }

    return address;
}

bool VMToOSInterface::DecommitMemory(void* address, size_t size)
{
    if (!IsValidMemoryRange(address, size))
    {
        return false;
    }

    memset(address, 0, size);
    return true;
}

bool VMToOSInterface::ReleaseMemoryReservation(void* address, size_t size)
{
    if (!IsValidMemoryRange(address, size))
    {
        return false;
    }

    free(address);
    return true;
}
#endif // TARGET_WASM

void* VMToOSInterface::AlignedAllocate(size_t alignment, size_t size)
{
    void* memptr;
    if (posix_memalign(&memptr, alignment, size) == 0)
    {
        return memptr;
    }
    return NULL;
}

void VMToOSInterface::AlignedFree(void* memblock)
{
    free(memblock);
}
