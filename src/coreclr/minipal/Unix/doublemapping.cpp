// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stddef.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <fcntl.h>
#include <unistd.h>
#include <inttypes.h>
#include <stdio.h>
#include <string.h>
#include <assert.h>
#include <limits.h>
#include <errno.h>
#if defined(TARGET_LINUX) && !defined(MFD_CLOEXEC)
#include <linux/memfd.h>
#include <sys/syscall.h> // __NR_memfd_create
#define memfd_create(...) syscall(__NR_memfd_create, __VA_ARGS__)
#endif // TARGET_LINUX && !MFD_CLOEXEC
#include "minipal.h"

#if defined(TARGET_OSX) && defined(TARGET_AMD64)
#include <mach/mach.h>
#endif // TARGET_OSX && TARGET_AMD64

#ifndef TARGET_OSX

#ifdef TARGET_64BIT
static const off_t MaxDoubleMappedSize = 2048ULL*1024*1024*1024;
#else
static const off_t MaxDoubleMappedSize = UINT_MAX;
#endif

#endif // TARGET_OSX

bool VMToOSInterface::CreateDoubleMemoryMapper(void** pHandle, size_t *pMaxExecutableCodeSize)
{
#ifndef TARGET_OSX

#ifdef TARGET_FREEBSD
    int fd = shm_open(SHM_ANON, O_RDWR | O_CREAT, S_IRWXU);
#elif defined(TARGET_SUNOS) // has POSIX implementation
    char name[24];
    sprintf(name, "/shm-dotnet-%d", getpid());
    name[sizeof(name) - 1] = '\0';
    shm_unlink(name);
    int fd = shm_open(name, O_RDWR | O_CREAT | O_EXCL | O_NOFOLLOW, 0600);
#else // TARGET_FREEBSD
    int fd = memfd_create("doublemapper", MFD_CLOEXEC);
#endif // TARGET_FREEBSD

    if (fd == -1)
    {
        return false;
    }

    if (ftruncate(fd, MaxDoubleMappedSize) == -1)
    {
        close(fd);
        return false;
    }

    *pMaxExecutableCodeSize = MaxDoubleMappedSize;
    *pHandle = (void*)(size_t)fd;
#else // !TARGET_OSX
    *pMaxExecutableCodeSize = SIZE_MAX;
    *pHandle = NULL;
#endif // !TARGET_OSX

    return true;
}

void VMToOSInterface::DestroyDoubleMemoryMapper(void *mapperHandle)
{
#ifndef TARGET_OSX
    close((int)(size_t)mapperHandle);
#endif
}

extern "C" void* PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(const void* lpBeginAddress, const void* lpEndAddress, size_t dwSize, int fStoreAllocationInfo);

#ifdef TARGET_OSX
bool IsMapJitFlagNeeded()
{
    static volatile int isMapJitFlagNeeded = -1;

    if (isMapJitFlagNeeded == -1)
    {
        int mapJitFlagCheckResult = 0;
        int pageSize = sysconf(_SC_PAGE_SIZE);
        // Try to map a page with read-write-execute protection. It should fail on Mojave hardened runtime and higher.
        void* testPage = mmap(NULL, pageSize, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0);
        if (testPage == MAP_FAILED && (errno == EACCES))
        {
            // The mapping has failed with EACCES, check if making the same mapping with MAP_JIT flag works
            testPage = mmap(NULL, pageSize, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_ANONYMOUS | MAP_PRIVATE | MAP_JIT, -1, 0);
            if (testPage != MAP_FAILED)
            {
                mapJitFlagCheckResult = 1;
            }
        }

        if (testPage != MAP_FAILED)
        {
            munmap(testPage, pageSize);
        }

        isMapJitFlagNeeded = mapJitFlagCheckResult;
    }

    return (bool)isMapJitFlagNeeded;
}
#endif // TARGET_OSX

void* VMToOSInterface::ReserveDoubleMappedMemory(void *mapperHandle, size_t offset, size_t size, const void *rangeStart, const void* rangeEnd)
{
    int fd = (int)(size_t)mapperHandle;

    bool isUnlimitedRange = (rangeStart == NULL) && (rangeEnd == NULL);

    if (isUnlimitedRange)
    {
        rangeEnd = (void*)SIZE_MAX;
    }

    void* result = PAL_VirtualReserveFromExecutableMemoryAllocatorWithinRange(rangeStart, rangeEnd, size, 0 /* fStoreAllocationInfo */);
#ifndef TARGET_OSX
    if (result != NULL)
    {
        // Map the shared memory over the range reserved from the executable memory allocator.
        result = mmap(result, size, PROT_NONE, MAP_SHARED | MAP_FIXED, fd, offset);
        if (result == MAP_FAILED)
        {
            assert(false);
            result = NULL;
        }
    }
#endif // TARGET_OSX

    // For requests with limited range, don't try to fall back to reserving at any address
    if ((result != NULL) || !isUnlimitedRange)
    {
        return result;
    }

#ifndef TARGET_OSX
    result = mmap(NULL, size, PROT_NONE, MAP_SHARED, fd, offset);
#else
    int mmapFlags = MAP_ANON | MAP_PRIVATE;
    if (IsMapJitFlagNeeded())
    {
        mmapFlags |= MAP_JIT;
    }
    result = mmap(NULL, size, PROT_NONE, mmapFlags, -1, 0);
#endif    
    if (result == MAP_FAILED)
    {
        assert(false);
        result = NULL;
    }
    return result;
}

void *VMToOSInterface::CommitDoubleMappedMemory(void* pStart, size_t size, bool isExecutable)
{
    if (mprotect(pStart, size, isExecutable ? (PROT_READ | PROT_EXEC) : (PROT_READ | PROT_WRITE)) == -1)
    {
        return NULL;
    }

    return pStart;
}

bool VMToOSInterface::ReleaseDoubleMappedMemory(void *mapperHandle, void* pStart, size_t offset, size_t size)
{
#ifndef TARGET_OSX
    int fd = (int)(size_t)mapperHandle;
    mmap(pStart, size, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_FIXED, fd, offset);
    memset(pStart, 0, size);
#endif // TARGET_OSX
    return munmap(pStart, size) != -1;
}

void* VMToOSInterface::GetRWMapping(void *mapperHandle, void* pStart, size_t offset, size_t size)
{
#ifndef TARGET_OSX
    int fd = (int)(size_t)mapperHandle;
    return mmap(NULL, size, PROT_READ | PROT_WRITE, MAP_SHARED, fd, offset);
#else // TARGET_OSX
#ifdef TARGET_AMD64
    vm_address_t startRW;
    vm_prot_t curProtection, maxProtection;
    kern_return_t kr = vm_remap(mach_task_self(), &startRW, size, 0, VM_FLAGS_ANYWHERE | VM_FLAGS_RANDOM_ADDR,
                                mach_task_self(), (vm_address_t)pStart, FALSE, &curProtection, &maxProtection, VM_INHERIT_NONE);

    if (kr != KERN_SUCCESS)
    {
        return NULL;
    }

    int st = mprotect((void*)startRW, size, PROT_READ | PROT_WRITE);
    if (st == -1)
    {
        munmap((void*)startRW, size);
        return NULL;
    }

    return (void*)startRW;
#else // TARGET_AMD64
    // This method should not be called on OSX ARM64
    assert(false);
    return NULL;
#endif // TARGET_AMD64
#endif // TARGET_OSX
}

bool VMToOSInterface::ReleaseRWMapping(void* pStart, size_t size)
{
    return munmap(pStart, size) != -1;
}
