// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "minipal.h"
#include <minipal/log.h>
#include <minipal/ospagesize.h>

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>
#include <sys/types.h>

#ifdef HOST_APPLE
#include <libkern/OSCacheControl.h>
#endif // HOST_APPLE

typedef char16_t WCHAR;
typedef int32_t HRESULT;

#include <dn-stdio.h>

#if !defined(MAP_ANON) && defined(MAP_ANONYMOUS)
#define MAP_ANON MAP_ANONYMOUS
#endif

static bool IsValidMemoryRange(void* address, size_t size, size_t pageSize)
{
    uintptr_t start = reinterpret_cast<uintptr_t>(address);
    if (size == 0 || start % pageSize != 0)
    {
        return false;
    }

    size_t remainder = size % pageSize;
    size_t padding = remainder == 0 ? 0 : pageSize - remainder;
    if (size > SIZE_MAX - padding)
    {
        return false;
    }

    return size + padding <= UINTPTR_MAX - start;
}

static bool IsValidPlaceholderRange(void* address, size_t size)
{
    size_t pageSize = minipal_getpagesize();
    if (address == nullptr || size % pageSize != 0 || !IsValidMemoryRange(address, size, pageSize))
    {
        errno = EINVAL;
        return false;
    }

    return true;
}

#ifdef TARGET_APPLE
static bool IsJitMappingRequired()
{
#ifdef HOST_ARM64
    return true;
#else // HOST_ARM64
    return VMToOSInterface::IsMapJitFlagNeeded();
#endif // HOST_ARM64
}
#endif // TARGET_APPLE

static int GetNativeProtection(VMToOSInterface::MemoryProtection protection)
{
    int nativeProtection;
    switch (protection)
    {
        case VMToOSInterface::MemoryProtection::NoAccess:
            nativeProtection = PROT_NONE;
            break;
        case VMToOSInterface::MemoryProtection::ReadOnly:
            nativeProtection = PROT_READ;
            break;
        case VMToOSInterface::MemoryProtection::ReadWrite:
        case VMToOSInterface::MemoryProtection::WriteCopy:
            nativeProtection = PROT_READ | PROT_WRITE;
            break;
        case VMToOSInterface::MemoryProtection::Execute:
            nativeProtection = PROT_EXEC;
            break;
        case VMToOSInterface::MemoryProtection::ExecuteRead:
            nativeProtection = PROT_READ | PROT_EXEC;
            break;
        case VMToOSInterface::MemoryProtection::ExecuteReadWrite:
            nativeProtection = PROT_READ | PROT_WRITE | PROT_EXEC;
            break;
        default:
            assert(!"Unknown memory protection");
            nativeProtection = PROT_NONE;
            break;
    }
#if defined(TARGET_APPLE) && defined(HOST_ARM64)
    // Apple Silicon JIT mappings keep RWX page protection and enforce W^X per thread.
    if ((nativeProtection & PROT_EXEC) != 0)
    {
        nativeProtection = PROT_READ | PROT_WRITE | PROT_EXEC;
    }
#endif // TARGET_APPLE && HOST_ARM64
    return nativeProtection;
}

static void IncludeMemoryInCoreDump(void* address, size_t size)
{
#if defined(MADV_DONTDUMP) && defined(MADV_DODUMP)
    madvise(address, size, MADV_DODUMP);
#endif // MADV_DONTDUMP && MADV_DODUMP
}

static void CopyMemoryToPlaceholder(void* address, const void* source, size_t size, bool executable)
{
#if defined(HOST_OSX) && defined(HOST_ARM64)
    if (executable)
    {
        VMToOSInterface::JitWriteProtect(true);
    }
#endif // HOST_OSX && HOST_ARM64

    if (size != 0)
    {
        memcpy(address, source, size);
    }

#if defined(HOST_OSX) && defined(HOST_ARM64)
    if (executable)
    {
        VMToOSInterface::JitWriteProtect(false);
    }
#endif // HOST_OSX && HOST_ARM64

    if (executable && size != 0)
    {
#ifdef HOST_APPLE
        sys_icache_invalidate(address, size);
#else
        char* begin = static_cast<char*>(address);
        __builtin___clear_cache(begin, begin + size);
#endif // HOST_APPLE
    }
}

VMToOSInterface::MemoryMapHandle VMToOSInterface::CreateMemoryMapping(MemoryMapHandle fileHandle, uint64_t, MemoryProtection)
{
    if (fileHandle < 0)
    {
        errno = EBADF;
        return InvalidMemoryMapHandle;
    }

    return fileHandle;
}

bool VMToOSInterface::CloseMemoryMapping(MemoryMapHandle)
{
    return true;
}

void* VMToOSInterface::MapAnonymousMemory(size_t size, MemoryProtection protection, bool)
{
    return MapMemory(InvalidMemoryMapHandle, 0, nullptr, size, protection, false);
}

void* VMToOSInterface::AllocateMemory(void* address, size_t size, MemoryProtection protection, bool executableBacking)
{
    size_t pageSize = minipal_getpagesize();
    if (size % pageSize != 0 || !IsValidMemoryRange(address, size, pageSize))
    {
        errno = EINVAL;
        return nullptr;
    }

    int nativeProtection = GetNativeProtection(protection);
    bool executable = (nativeProtection & PROT_EXEC) != 0;
    int flags = MAP_ANON | MAP_PRIVATE;
#ifdef TARGET_APPLE
    bool jitMapping = (executable || executableBacking) && IsJitMappingRequired();
    if (jitMapping)
    {
        flags |= MAP_JIT;
    }
#endif // TARGET_APPLE

    if (address == nullptr && (executable || executableBacking))
    {
        constexpr size_t allocationGranularity = 64 * 1024;
        if (size > SIZE_MAX - (allocationGranularity - 1))
        {
            errno = ENOMEM;
            return nullptr;
        }

        size_t reservationSize = (size + allocationGranularity - 1) & ~(allocationGranularity - 1);
        void* allocation = VMToOSInterface::ReserveMemoryFromExecutableAllocatorWithinRange(
            nullptr, reinterpret_cast<void*>(SIZE_MAX), reservationSize);
        if (allocation != nullptr)
        {
            int error = 0;
            size_t ownedSize = reservationSize;
            // The caller owns only the requested pages, not the allocator's 64 KiB rounding.
            if (reservationSize != size)
            {
                if (munmap(static_cast<char*>(allocation) + size, reservationSize - size) != 0)
                    error = errno;
                else
                    ownedSize = size;
            }

            if (error == 0 && nativeProtection != PROT_NONE)
            {
#ifdef TARGET_APPLE
                if (jitMapping)
                {
                    // Keep the executable allocator's MAP_JIT backing.
                    if (mprotect(allocation, size, nativeProtection) != 0)
                        error = errno;
                }
                else
#endif // TARGET_APPLE
                if (mmap(allocation, size, nativeProtection, flags | MAP_FIXED, -1, 0) == MAP_FAILED)
                {
                    error = errno;
                }

                if (error == 0)
                {
                    IncludeMemoryInCoreDump(allocation, size);
                }
            }

            if (error != 0)
            {
                if (munmap(allocation, ownedSize) != 0)
                    minipal_log_print_error("Releasing failed memory allocation failed with error %d.\n", errno);
                errno = error;
                return nullptr;
            }

            return allocation;
        }
    }

    // A preferred address is a hint; never replace memory the caller does not own.
    void* result = mmap(address, size, nativeProtection, flags, -1, 0);
    return result == MAP_FAILED ? nullptr : result;
}

void* VMToOSInterface::MapMemory(MemoryMapHandle fd, uint64_t offset, void* address, size_t size, MemoryProtection protection, bool shared)
{
    if (fd < -1)
    {
        errno = EBADF;
        return nullptr;
    }

    int nativeProtection = GetNativeProtection(protection);

    size_t pageSize = minipal_getpagesize();
    if (!IsValidMemoryRange(address, size, pageSize) || offset % pageSize != 0 ||
        (shared && (nativeProtection & PROT_WRITE) != 0) ||
        (fd == -1 && (offset != 0 || shared)))
    {
        errno = EINVAL;
        return nullptr;
    }

    off_t fileOffset = static_cast<off_t>(offset);
    if (fileOffset < 0 || static_cast<uint64_t>(fileOffset) != offset)
    {
        errno = EOVERFLOW;
        return nullptr;
    }

#ifdef TARGET_APPLE
    if (fd != -1 && address != nullptr && (nativeProtection & PROT_EXEC) != 0 && IsJitMappingRequired())
    {
        // Executable file views must retain the reservation's MAP_JIT backing.
        void* source = mmap(nullptr, size, PROT_READ, MAP_PRIVATE, fd, fileOffset);
        if (source == MAP_FAILED)
        {
            return nullptr;
        }

#ifdef HOST_ARM64
        int copyProtection = PROT_READ | PROT_WRITE | PROT_EXEC;
#else // HOST_ARM64
        int copyProtection = PROT_READ | PROT_WRITE;
#endif // HOST_ARM64
        int error = 0;
        if (mprotect(address, size, copyProtection) != 0)
        {
            error = errno;
        }
        else
        {
            CopyMemoryToPlaceholder(address, source, size, true);
            if (!ProtectMemory(address, size, protection))
            {
                error = errno;
            }
        }

        if (munmap(source, size) != 0 && error == 0)
        {
            error = errno;
        }
        if (error != 0)
        {
            errno = error;
            return nullptr;
        }

        return address;
    }
#endif // TARGET_APPLE

    int flags = shared ? MAP_SHARED : MAP_PRIVATE;
    if (fd == -1)
    {
        flags |= MAP_ANON;
#ifdef TARGET_APPLE
        if ((nativeProtection & PROT_EXEC) != 0 && IsJitMappingRequired())
        {
            flags |= MAP_JIT;
            nativeProtection = GetNativeProtection(protection);
        }
#endif // TARGET_APPLE
    }
    if (address != nullptr)
    {
        flags |= MAP_FIXED;
    }

    void* result = mmap(address, size, nativeProtection, flags, fd, fileOffset);
    return result == MAP_FAILED ? nullptr : result;
}

int VMToOSInterface::UnmapMemory(void* address, size_t size)
{
    if (address == nullptr || !IsValidMemoryRange(address, size, minipal_getpagesize()))
    {
        errno = EINVAL;
        return -1;
    }

    return munmap(address, size);
}

bool VMToOSInterface::SupportsPlaceholderMapping()
{
    return true;
}

void* VMToOSInterface::ReserveMemoryPlaceholder(void* address, size_t size, bool executableBacking)
{
    return AllocateMemory(address, size, MemoryProtection::NoAccess, executableBacking);
}

bool VMToOSInterface::SplitMemoryPlaceholder(void* address, size_t size)
{
    // Unix mappings can be replaced and unmapped in caller-tracked subranges.
    return IsValidPlaceholderRange(address, size);
}

void* VMToOSInterface::CommitMemoryPlaceholder(void* address, size_t size, MemoryProtection protection)
{
    if (!IsValidPlaceholderRange(address, size))
    {
        return nullptr;
    }

#ifdef TARGET_APPLE
    if (IsJitMappingRequired())
    {
        // Replacing the reservation would lose MAP_JIT, even for initially non-executable pages.
        if (mprotect(address, size, GetNativeProtection(protection)) != 0)
        {
            return nullptr;
        }

        return address;
    }
#endif // TARGET_APPLE

    return MapMemory(InvalidMemoryMapHandle, 0, address, size, protection, false);
}

bool VMToOSInterface::ReleaseMemoryReservation(void* address, size_t size)
{
    return IsValidPlaceholderRange(address, size) && munmap(address, size) == 0;
}

void* VMToOSInterface::ReserveMemory(size_t size, bool executableBacking)
{
    return AllocateMemory(nullptr, size, MemoryProtection::NoAccess, executableBacking);
}

void* VMToOSInterface::ReserveMemoryWithinRange(const void* rangeStart, const void* rangeEnd, size_t size, bool executable)
{
    size_t pageSize = minipal_getpagesize();
    if (size == 0 || size % pageSize != 0)
    {
        errno = EINVAL;
        return nullptr;
    }

    if (rangeStart == nullptr && rangeEnd == nullptr)
    {
        return ReserveMemory(size, executable);
    }

    uintptr_t start = reinterpret_cast<uintptr_t>(rangeStart);
    uintptr_t end = reinterpret_cast<uintptr_t>(rangeEnd);
    if (end <= start)
    {
        errno = EINVAL;
        return nullptr;
    }

    constexpr size_t granularity = 64 * 1024;
    if (size > SIZE_MAX - (granularity - 1))
    {
        errno = ENOMEM;
        return nullptr;
    }

    size_t reservationSize = (size + granularity - 1) & ~(granularity - 1);
    if (executable)
    {
        void* allocation = VMToOSInterface::ReserveMemoryFromExecutableAllocatorWithinRange(rangeStart, rangeEnd, reservationSize);
        if (allocation != nullptr)
        {
            if (reservationSize != size && munmap(static_cast<char*>(allocation) + size, reservationSize - size) != 0)
            {
                int error = errno;
                if (munmap(allocation, reservationSize) != 0)
                    minipal_log_print_error("Releasing failed memory reservation failed with error %d.\n", errno);
                errno = error;
                return nullptr;
            }

            return allocation;
        }
    }

    if (start > UINTPTR_MAX - (granularity - 1))
    {
        errno = ENOMEM;
        return nullptr;
    }

    uintptr_t tryAddress = (start + granularity - 1) & ~(granularity - 1);
    int flags = MAP_ANON | MAP_PRIVATE;
#ifdef TARGET_APPLE
    if (executable && IsJitMappingRequired())
    {
        flags |= MAP_JIT;
    }
#endif // TARGET_APPLE

    while (tryAddress <= end && size <= end - tryAddress)
    {
        // A non-fixed mmap hint is honored only when the kernel can satisfy it exactly;
        // otherwise it silently returns a different address, which we treat as "unavailable"
        // and release rather than risk clobbering memory outside the requested range.
        void* result = mmap(reinterpret_cast<void*>(tryAddress), size, PROT_NONE, flags, -1, 0);
        if (result == MAP_FAILED)
        {
            break;
        }

        if (reinterpret_cast<uintptr_t>(result) == tryAddress)
        {
            return result;
        }

        munmap(result, size);
        if (tryAddress > UINTPTR_MAX - granularity)
        {
            break;
        }
        tryAddress += granularity;
    }

    errno = ENOMEM;
    return nullptr;
}

void* VMToOSInterface::CommitReservedMemory(void* address, size_t size, MemoryProtection protection)
{
    if (!IsValidPlaceholderRange(address, size))
    {
        return nullptr;
    }

    // Anonymous reservations acquire physical pages on demand when accessed. Committing on Unix
    // only makes the reservation accessible with the requested protection.
    if (mprotect(address, size, GetNativeProtection(protection)) != 0)
    {
        return nullptr;
    }

    IncludeMemoryInCoreDump(address, size);
    return address;
}

bool VMToOSInterface::DecommitMemory(void* address, size_t size)
{
    size_t pageSize = minipal_getpagesize();
    if (!IsValidMemoryRange(address, size, pageSize) || size % pageSize != 0)
    {
        errno = EINVAL;
        return false;
    }

    // Re-establish a fresh, zero-filled anonymous mapping over the range rather than using
    // madvise(MADV_DONTNEED): the pages must become immediately non-resident and the range
    // must keep its PROT_NONE-equivalent "decommitted" semantics until recommitted.
    void* result = mmap(address, size, PROT_NONE, MAP_ANON | MAP_PRIVATE | MAP_FIXED, -1, 0);
    return result != MAP_FAILED;
}

bool VMToOSInterface::ProtectMemory(void* address, size_t size, MemoryProtection protection)
{
    uintptr_t start = reinterpret_cast<uintptr_t>(address);
    size_t pageSize = minipal_getpagesize();
    uintptr_t pageStart = start & ~(pageSize - 1);
    size_t offsetInPage = start - pageStart;
    if (address == nullptr || size == 0 || size > SIZE_MAX - offsetInPage ||
        !IsValidMemoryRange(reinterpret_cast<void*>(pageStart), size + offsetInPage, pageSize))
    {
        errno = EINVAL;
        return false;
    }

    return mprotect(reinterpret_cast<void*>(pageStart), size + offsetInPage, GetNativeProtection(protection)) == 0;
}

bool VMToOSInterface::PopulateMemoryPlaceholder(void* address, size_t allocationSize, const void* source, size_t copySize, MemoryProtection finalProtection)
{
    if (!IsValidPlaceholderRange(address, allocationSize))
    {
        return false;
    }

    if (copySize > allocationSize ||
        (copySize != 0 && (source == nullptr || copySize > UINTPTR_MAX - reinterpret_cast<uintptr_t>(source))))
    {
        errno = EINVAL;
        return false;
    }

    bool executable = (GetNativeProtection(finalProtection) & PROT_EXEC) != 0;
    MemoryProtection initialProtection = MemoryProtection::ReadWrite;
#ifdef TARGET_APPLE
    if (executable && IsJitMappingRequired())
    {
        initialProtection = MemoryProtection::ExecuteReadWrite;
    }
#endif // TARGET_APPLE

    if (CommitMemoryPlaceholder(address, allocationSize, initialProtection) == nullptr)
    {
        return false;
    }

    CopyMemoryToPlaceholder(address, source, copySize, executable);
    return ProtectMemory(address, allocationSize, finalProtection);
}

bool VMToOSInterface::UseSharedReadOnlyMapping(size_t sectionAlignment)
{
    if (sectionAlignment <= minipal_getpagesize())
    {
        return false;
    }

    const char* configuration = getenv("PAL_MAP_READONLY_PE_HUGE_PAGE_AS_SHARED");
    return configuration != nullptr && strcmp(configuration, "1") == 0;
}

int VMToOSInterface::MarkMemoryAsNotNeeded(void* address, size_t size)
{
#ifdef TARGET_ANDROID
    return 0;
#else // TARGET_ANDROID
    // Linux madvise(MADV_DONTNEED) would discard modified private pages.
    return posix_madvise(address, size, POSIX_MADV_DONTNEED);
#endif // TARGET_ANDROID
}
