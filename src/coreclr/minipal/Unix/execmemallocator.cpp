// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file implements an allocator that opportunistically reserves a chunk of virtual memory located
// near this library (within a 2 GB range) during process startup, for later use by the JIT and other
// executable-memory consumers. Having executable memory close to this library lets generated code use
// IP-relative addressing instead of jump stubs, which can meaningfully improve performance.
//
// The memory handed out by this allocator is reserved only (never committed), and callers become the
// owner of whatever they are given; this allocator provides no way to give memory back.

#include "minipal.h"
#include <minipal/mutex.h>
#include <minipal/ospagesize.h>

#include <assert.h>
#include <dlfcn.h>
#include <errno.h>
#include <limits.h>
#include <stdint.h>
#include <stdlib.h>
#include <sys/mman.h>
#include <time.h>

#if !defined(TARGET_WASI)
#include <sys/resource.h>
#endif // !TARGET_WASI

#if !defined(MAP_ANON) && defined(MAP_ANONYMOUS)
#define MAP_ANON MAP_ANONYMOUS
#endif

namespace
{
    constexpr size_t Granularity64KB = 64 * 1024;

    // There is no portable way to query the size of a loaded library on Unix, so this constant is used
    // as an approximation of this library's size when computing an address that is within a 2 GB range
    // of it. Precise accuracy is not required for this calculation.
    constexpr int32_t CoreClrLibrarySize = 16 * 1024 * 1024;

    // The max size of the virtual memory that this allocator will try to reserve during initialization.
    // The goal is for all JIT-generated code and this library to be located within a 2 GB range on x86/x64.
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    // It is more difficult to reserve a 2 GB chunk on arm, so a smaller size is used instead.
    constexpr int32_t MaxExecutableMemorySize = 1024 * 1024 * 1024;
#else
    constexpr int32_t MaxExecutableMemorySize = 0x7FFF0000;
#endif
    constexpr int32_t MaxExecutableMemorySizeNearCoreClr = MaxExecutableMemorySize - CoreClrLibrarySize;

    // The mutex is constructed lazily (rather than only when InitializeExecutableMemoryAllocator runs)
    // because ReserveMemoryFromExecutableAllocatorWithinRange can be called by consumers that never
    // enable the allocator, and still needs a validly-initialized mutex to synchronize against.
    minipal_mutex* GetMutex()
    {
        static minipal_mutex s_mutex;
        static const bool initialized = minipal_mutex_init(&s_mutex);

        return initialized ? &s_mutex : nullptr;
    }

    // Start of the reserved virtual address space.
    void* s_startAddress;
    // Next available address within the reserved address space.
    void* s_nextFreeAddress;
    // Remaining size of the reserved virtual memory that can be used to satisfy allocation requests.
    int32_t s_remainingReservedMemory;
    // Preferred range to report back to callers for where this allocator will put code, which may
    // additionally include this library's own range if that range is nearby.
    void* s_preferredRangeStart;
    void* s_preferredRangeEnd;

#if defined(TARGET_APPLE)
    bool IsJitMappingRequired()
    {
#if defined(HOST_ARM64)
        return true;
#else
        return VMToOSInterface::IsMapJitFlagNeeded();
#endif // HOST_ARM64
    }
#endif // TARGET_APPLE

    // Reserves (but does not commit) size bytes of virtual memory from the OS. address is a hint; if
    // non-null and the OS is unable to honor it exactly, the reservation is released and nullptr is
    // returned instead of silently returning a different address.
    void* ReserveFromOS(void* address, size_t size)
    {
        int flags = MAP_ANON | MAP_PRIVATE;
#if defined(TARGET_APPLE)
        if (IsJitMappingRequired())
        {
            flags |= MAP_JIT;
        }
#endif // TARGET_APPLE

        void* result = mmap(address, size, PROT_NONE, flags, -1, 0);
        if (result == MAP_FAILED)
        {
            return nullptr;
        }

        if (address != nullptr && result != address)
        {
            munmap(result, size);
            return nullptr;
        }

#if defined(MADV_DONTDUMP)
        // This allocator never commits the memory it reserves; exclude it from core dumps.
        madvise(result, size, MADV_DONTDUMP);
#endif // MADV_DONTDUMP

        return result;
    }

    // This function is called during initialization. It opportunistically tries to reserve a large
    // chunk of virtual memory that can later be used to store JIT-generated code.
    void TryReserveInitialMemory(uint32_t initialExecMemoryPercent)
    {
        int32_t sizeOfAllocation = MaxExecutableMemorySizeNearCoreClr;
        int32_t initialReserveLimit = -1;

#if !defined(TARGET_WASI)
        // WASI has neither RLIMIT_AS nor RLIMIT_DATA nor RLIM_INFINITY; wasm32 has a hard 4 GiB limit
        // and no separate VM accounting, so this rlimit-based sizing is skipped there and
        // sizeOfAllocation stays at MaxExecutableMemorySizeNearCoreClr.
#if defined(RLIMIT_AS)
        int addressSpace = RLIMIT_AS;
#else
        int addressSpace = RLIMIT_DATA;
#endif
        rlimit addressSpaceLimit;
        if ((getrlimit(addressSpace, &addressSpaceLimit) == 0) && (addressSpaceLimit.rlim_cur != RLIM_INFINITY))
        {
            // By default, reserve at most 20% of the available virtual address space.
            initialReserveLimit = (int32_t)(addressSpaceLimit.rlim_cur * initialExecMemoryPercent / 100);
            if (initialReserveLimit < sizeOfAllocation)
            {
                sizeOfAllocation = initialReserveLimit;
            }
        }
#endif // !TARGET_WASI

#if defined(TARGET_ARM) || defined(TARGET_ARM64)
        // Smaller steps on arm because we try hard to find spare memory within a 128 MB distance from
        // this library, so that e.g. all calls from corelib to this library could use relocs.
        constexpr int32_t AddressProbingIncrement = 8 * 1024 * 1024;
#else
        constexpr int32_t AddressProbingIncrement = 128 * 1024 * 1024;
#endif
        constexpr int32_t SizeProbingDecrement = 128 * 1024 * 1024;

        // Try to find and reserve an available region of virtual memory that is located within a 2 GB
        // range (MaxExecutableMemorySizeNearCoreClr) of this library's location.
        //
        // First, determine the current address of this library. Depending on the OS, it is usually
        // loaded either at the end or at the start of the user address space. If it is loaded at low
        // addresses, try to reserve memory above it (avoiding reservations below 4 GB, which some
        // operating systems do not allow); if it is loaded at high addresses, try to reserve memory
        // below it.
        Dl_info info;
        uintptr_t coreclrLoadAddress = dladdr((void*)&TryReserveInitialMemory, &info) != 0
            ? (uintptr_t)info.dli_fbase
            : 0;

        uintptr_t preferredStartAddress;
        int32_t preferredStartAddressIncrement;
        if ((coreclrLoadAddress < 0xFFFFFFFF) || ((coreclrLoadAddress - MaxExecutableMemorySizeNearCoreClr) < 0xFFFFFFFF))
        {
            // Try to allocate above the location of this library.
            preferredStartAddress = coreclrLoadAddress + CoreClrLibrarySize;
            preferredStartAddressIncrement = AddressProbingIncrement;
        }
        else
        {
            // Try to allocate below the location of this library.
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
            // For the "high address" case on arm, it only makes sense to try to reserve 128 MB; if that
            // doesn't work, a full-sized region will be reserved at a random location instead.
            sizeOfAllocation = SizeProbingDecrement;
#endif
            preferredStartAddress = coreclrLoadAddress - sizeOfAllocation;
            preferredStartAddressIncrement = 0;
        }

        do
        {
            s_startAddress = ReserveFromOS((void*)preferredStartAddress, sizeOfAllocation);
            if (s_startAddress != nullptr)
            {
                break;
            }

            // Try to reserve a smaller region.
            sizeOfAllocation -= SizeProbingDecrement;
            preferredStartAddress += preferredStartAddressIncrement;
        } while (sizeOfAllocation >= SizeProbingDecrement);

        if (s_startAddress == nullptr)
        {
            // Reservation near this library failed; try to reserve approximately 2 GB of address space
            // anywhere. This sets aside address space usable for executable code, so that jumps/calls
            // between such code may continue to use short relative addresses instead of long absolute
            // addresses that would require jump stubs.
            sizeOfAllocation = (initialReserveLimit != -1) ? initialReserveLimit : MaxExecutableMemorySize;
            s_startAddress = ReserveFromOS(nullptr, sizeOfAllocation);
            if (s_startAddress == nullptr)
            {
                return;
            }

            s_preferredRangeStart = s_startAddress;
            s_preferredRangeEnd = (char*)s_startAddress + sizeOfAllocation;
        }
        else
        {
            // Memory close to this library was reserved; include its address range in the preferred
            // range to allow generated code to use IP-relative addressing.
            if ((char*)s_startAddress < (char*)coreclrLoadAddress)
            {
                s_preferredRangeStart = s_startAddress;
                s_preferredRangeEnd = (char*)coreclrLoadAddress + CoreClrLibrarySize;
            }
            else
            {
                s_preferredRangeStart = (void*)coreclrLoadAddress;
                s_preferredRangeEnd = (char*)s_startAddress + sizeOfAllocation;
            }

            assert((char*)s_preferredRangeEnd - (char*)s_preferredRangeStart <= INT_MAX);
        }

        // Memory has been successfully reserved.

        // Randomize the location at which allocation starts from within the reserved memory range.
        // Alignment to a 64 KB granularity should not be necessary, but see
        // ReserveMemoryFromExecutableAllocatorWithinRange() for why it is done anyway.
#if defined(__sun)
        // The upper limit of random() on SunOS-derived operating systems is not RAND_MAX, but 2^31-1.
        constexpr int32_t OffsetRandMax = 0x7FFFFFFF;
#else
        constexpr int32_t OffsetRandMax = RAND_MAX;
#endif
        constexpr int32_t MaxStartPageOffset = 64;
        srandom((unsigned int)time(nullptr));
        int32_t randomOffset = (int32_t)(MaxStartPageOffset * (int64_t)random() / OffsetRandMax) * (int32_t)minipal_getpagesize();

        s_nextFreeAddress = (void*)(((uintptr_t)s_startAddress + randomOffset + Granularity64KB - 1) & ~(Granularity64KB - 1));
        assert(sizeOfAllocation >= (int32_t)((uintptr_t)s_nextFreeAddress - (uintptr_t)s_startAddress));
        s_remainingReservedMemory =
            (int32_t)((sizeOfAllocation - ((uintptr_t)s_nextFreeAddress - (uintptr_t)s_startAddress)) & ~(Granularity64KB - 1));
    }
}

bool VMToOSInterface::InitializeExecutableMemoryAllocator(uint32_t initialExecMemoryPercent)
{
    // Enabled on 64-bit hosts only; 32-bit hosts have limited virtual address space, and reserving a
    // large near-library chunk there would be counterproductive.
#if defined(HOST_64BIT)
    if (GetMutex() == nullptr)
    {
        return false;
    }

    TryReserveInitialMemory(initialExecMemoryPercent);
#endif // HOST_64BIT

    return true;
}

void* VMToOSInterface::ReserveMemoryFromExecutableAllocatorWithinRange(const void* rangeStart, const void* rangeEnd, size_t size)
{
#if defined(HOST_64BIT)
    assert(rangeStart <= rangeEnd);

    // Keep every bump allocation aligned to the executable allocator's 64 KB granularity.
    if (size > SIZE_MAX - (Granularity64KB - 1))
    {
        return nullptr;
    }

    size_t reservationSize = (size + Granularity64KB - 1) & ~(Granularity64KB - 1);
    minipal_mutex* mutex = GetMutex();
    if (mutex == nullptr)
    {
        errno = ENOMEM;
        return nullptr;
    }

    minipal::MutexHolder holder(*mutex);

    if (reservationSize == 0 || reservationSize > (size_t)s_remainingReservedMemory)
    {
        return nullptr;
    }

    void* address = s_nextFreeAddress;
    if (address < rangeStart)
    {
        return nullptr;
    }

    void* nextFreeAddress = (void*)((uintptr_t)address + reservationSize);
    if (nextFreeAddress > rangeEnd)
    {
        return nullptr;
    }

    assert(((uintptr_t)address & (minipal_getpagesize() - 1)) == 0);
    s_nextFreeAddress = nextFreeAddress;
    s_remainingReservedMemory -= (int32_t)reservationSize;
    return address;
#else // !HOST_64BIT
    (void)rangeStart;
    (void)rangeEnd;
    (void)size;
    return nullptr;
#endif // HOST_64BIT
}

void VMToOSInterface::GetExecutableMemoryAllocatorPreferredRange(void** start, void** end)
{
    *start = s_preferredRangeStart;
    *end = s_preferredRangeEnd;
}
