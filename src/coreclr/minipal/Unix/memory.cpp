// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//

#include <assert.h>
#include <inttypes.h>
#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/stat.h>
#include <unistd.h>
#include "minipal.h"

typedef char16_t WCHAR;
typedef int32_t HRESULT;

#include "dn-stdio.h"

int32_t VMToOSInterface::GetLastMemoryMappingError()
{
    return HRESULTFromErr(errno);
}

VMToOSInterface::MemoryMapHandle VMToOSInterface::OpenFile(const char16_t* path)
{
    FILE* stream;
    int error = u16_fopen_s(&stream, path, u"rb");
    if (error != 0)
    {
        errno = error;
        return InvalidFileHandle;
    }

    int descriptor = fcntl(fileno(stream), F_DUPFD_CLOEXEC, 0);
    error = descriptor < 0 ? errno : 0;
    if (fclose(stream) != 0 && error == 0)
    {
        error = errno;
    }

    if (descriptor < 0)
    {
        errno = error;
        return InvalidFileHandle;
    }

    struct stat fileInfo;
    int result;
    do
    {
        result = fstat(descriptor, &fileInfo);
    } while (result != 0 && errno == EINTR);

    if (result != 0 || S_ISDIR(fileInfo.st_mode))
    {
        error = result != 0 ? errno : EISDIR;
        close(descriptor);
        errno = error;
        return InvalidFileHandle;
    }

    return descriptor;
}

bool VMToOSInterface::CloseFile(MemoryMapHandle fileHandle)
{
    return close(fileHandle) == 0;
}

bool VMToOSInterface::GetFileSize(MemoryMapHandle fileHandle, uint64_t* size)
{
    struct stat fileInfo;
    int result;
    do
    {
        result = fstat(fileHandle, &fileInfo);
    } while (result != 0 && errno == EINTR);

    if (result != 0 || fileInfo.st_size < 0)
    {
        return false;
    }

    *size = static_cast<uint64_t>(fileInfo.st_size);
    return true;
}

#ifdef TARGET_WASM
bool VMToOSInterface::ReadFile(MemoryMapHandle fileHandle, uint64_t offset, void* buffer, size_t size)
{
    uint8_t* destination = static_cast<uint8_t*>(buffer);
    size_t bytesRead = 0;
    while (bytesRead < size)
    {
        constexpr size_t MaxReadSize = 64 * 1024;
        size_t remaining = size - bytesRead;
        size_t readSize = remaining < MaxReadSize ? remaining : MaxReadSize;
        ssize_t result;
        do
        {
            result = pread(fileHandle, destination + bytesRead, readSize,
                static_cast<off_t>(offset + bytesRead));
        } while (result < 0 && errno == EINTR);

        if (result <= 0)
        {
            if (result == 0)
            {
                errno = EIO;
            }
            return false;
        }

        bytesRead += static_cast<size_t>(result);
    }

    return true;
}
#endif // TARGET_WASM

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

void* VMToOSInterface::AllocateMemory(void* address, size_t size, MemoryProtection protection, bool executableBacking)
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
    (void)executableBacking;
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

void* VMToOSInterface::ReserveMemoryWithinRange(const void* rangeStart, const void* rangeEnd, size_t size, bool executable)
{
    (void)executable;

    uintptr_t start = reinterpret_cast<uintptr_t>(rangeStart);
    uintptr_t end = reinterpret_cast<uintptr_t>(rangeEnd);
    if (rangeStart == nullptr && rangeEnd == nullptr)
    {
        return ReserveMemory(size);
    }

    if (end <= start)
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

void* VMToOSInterface::ReserveMemory(size_t size, bool executableBacking)
{
    (void)executableBacking;

    return AllocateMemory(nullptr, size, MemoryProtection::NoAccess);
}

void* VMToOSInterface::CommitReservedMemory(void* address, size_t size, MemoryProtection protection)
{
    assert(IsValidMemoryRange(address, size));
    assert(protection == MemoryProtection::ReadWrite);
    (void)size;
    (void)protection;

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
