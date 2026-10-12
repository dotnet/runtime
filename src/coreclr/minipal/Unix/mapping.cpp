// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "minipal.h"
#include <minipal/ospagesize.h>

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>
#include <sys/types.h>

typedef char16_t WCHAR;
typedef int32_t HRESULT;

#include <dn-stdio.h>

#if !defined(MAP_ANON) && defined(MAP_ANONYMOUS)
#define MAP_ANON MAP_ANONYMOUS
#endif

extern "C" void* PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(const void* beginAddress, const void* endAddress, size_t size, int storeAllocationInfo);
extern "C" int32_t VirtualFree(void* address, size_t size, uint32_t freeType);

#if defined(HOST_OSX) && defined(HOST_ARM64)
extern "C" void PAL_JitWriteProtect(bool writeEnable);
#endif // HOST_OSX && HOST_ARM64

#ifndef NDEBUG
static bool IsValidMemoryProtection(int nativeProtection)
{
    return (nativeProtection & ~(PROT_READ | PROT_WRITE | PROT_EXEC)) == 0 &&
        ((nativeProtection & PROT_WRITE) == 0 || (nativeProtection & PROT_READ) != 0);
}
#endif // !NDEBUG

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
    int nativeProtection = static_cast<int>(protection);
#if defined(TARGET_APPLE) && defined(HOST_ARM64)
    // Apple Silicon JIT mappings keep RWX page protection and enforce W^X per thread.
    if ((nativeProtection & PROT_EXEC) != 0)
    {
        nativeProtection = PROT_READ | PROT_WRITE | PROT_EXEC;
    }
#endif // TARGET_APPLE && HOST_ARM64
    return nativeProtection;
}

static void CopyMemoryToPlaceholder(void* address, const void* source, size_t size, bool executable)
{
#if defined(HOST_OSX) && defined(HOST_ARM64)
    if (executable)
    {
        PAL_JitWriteProtect(true);
    }
#endif // HOST_OSX && HOST_ARM64

    if (size != 0)
    {
        memcpy(address, source, size);
    }

#if defined(HOST_OSX) && defined(HOST_ARM64)
    if (executable)
    {
        PAL_JitWriteProtect(false);
    }
#endif // HOST_OSX && HOST_ARM64

#if defined(__GNUC__) || defined(__clang__)
    if (executable && size != 0)
    {
        char* begin = static_cast<char*>(address);
        __builtin___clear_cache(begin, begin + size);
    }
#endif // __GNUC__ || __clang__
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

int32_t VMToOSInterface::GetLastMemoryMappingError()
{
    return HRESULTFromErr(errno);
}

void* VMToOSInterface::MapMemory(MemoryMapHandle fd, uint64_t offset, void* address, size_t size, MemoryProtection protection, bool shared)
{
    if (fd < -1)
    {
        errno = EBADF;
        return nullptr;
    }

    int nativeProtection = static_cast<int>(protection);
    assert(IsValidMemoryProtection(nativeProtection));

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

void* VMToOSInterface::ReserveMemoryPlaceholder(void* address, size_t size)
{
    size_t pageSize = minipal_getpagesize();
    if (size % pageSize != 0 || !IsValidMemoryRange(address, size, pageSize))
    {
        errno = EINVAL;
        return nullptr;
    }

    if (address == nullptr)
    {
        constexpr size_t allocationGranularity = 64 * 1024;
        if (size > SIZE_MAX - (allocationGranularity - 1))
        {
            errno = ENOMEM;
            return nullptr;
        }

        size_t reservationSize = (size + allocationGranularity - 1) & ~(allocationGranularity - 1);
        void* reservation = PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(
            nullptr, reinterpret_cast<void*>(SIZE_MAX), reservationSize, 0);
        if (reservation != nullptr)
        {
            // The caller owns only the requested pages, not the allocator's 64 KiB rounding.
            if (reservationSize != size &&
                munmap(static_cast<char*>(reservation) + size, reservationSize - size) != 0)
            {
                int error = errno;
                munmap(reservation, reservationSize);
                errno = error;
                return nullptr;
            }

            return reservation;
        }
    }

    int flags = MAP_ANON | MAP_PRIVATE;
#ifdef TARGET_APPLE
    if (IsJitMappingRequired())
    {
        flags |= MAP_JIT;
    }
#endif // TARGET_APPLE

    // A preferred address is a hint; never replace memory the caller does not own.
    void* result = mmap(address, size, PROT_NONE, flags, -1, 0);
    return result == MAP_FAILED ? nullptr : result;
}

bool VMToOSInterface::SplitMemoryPlaceholder(void* address, size_t size)
{
    // Unix mappings can be replaced and unmapped in caller-tracked subranges.
    return IsValidPlaceholderRange(address, size);
}

void* VMToOSInterface::CommitMemoryPlaceholder(void* address, size_t size, MemoryProtection protection)
{
    assert(IsValidMemoryProtection(static_cast<int>(protection)));

    if (!IsValidPlaceholderRange(address, size))
    {
        return nullptr;
    }

#ifdef TARGET_APPLE
    if (IsJitMappingRequired())
    {
        // Replacing the reservation would lose MAP_JIT, even for initially non-executable pages.
        return mprotect(address, size, GetNativeProtection(protection)) == 0 ? address : nullptr;
    }
#endif // TARGET_APPLE

    return MapMemory(InvalidMemoryMapHandle, 0, address, size, protection, false);
}

bool VMToOSInterface::ReleaseMemoryReservation(void* address, size_t size)
{
    if (size != 0)
    {
        return IsValidPlaceholderRange(address, size) && munmap(address, size) == 0;
    }

    if (address == nullptr || reinterpret_cast<uintptr_t>(address) % minipal_getpagesize() != 0)
    {
        errno = EINVAL;
        return false;
    }

    // Size-zero parts belong to the legacy PAL VirtualAlloc copying fallback.
    constexpr uint32_t memRelease = 0x8000;
    if (!VirtualFree(address, 0, memRelease))
    {
        errno = EFAULT;
        return false;
    }

    return true;
}

bool VMToOSInterface::ProtectMemory(void* address, size_t size, MemoryProtection protection)
{
    assert(IsValidMemoryProtection(static_cast<int>(protection)));

    if (address == nullptr || !IsValidMemoryRange(address, size, minipal_getpagesize()))
    {
        errno = EINVAL;
        return false;
    }

    return mprotect(address, size, GetNativeProtection(protection)) == 0;
}

bool VMToOSInterface::PopulateMemoryPlaceholder(void* address, size_t allocationSize, const void* source, size_t copySize, MemoryProtection finalProtection)
{
    assert(IsValidMemoryProtection(static_cast<int>(finalProtection)));

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

    bool executable = (static_cast<int>(finalProtection) & PROT_EXEC) != 0;
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
