// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cstdint>
#include <cstddef>
#include <cassert>
#include <memory>
#include <pthread.h>
#include <signal.h>

#include "config.h"
#include "common.h"

#include "gcenv.structs.h"
#include "gcenv.base.h"
#include "gcenv.os.h"
#include "gcenv.unix.inl"
#include "volatile.h"

#if HAVE_SYS_TIME_H
 #include <sys/time.h>
#else
 #error "sys/time.h required by GC PAL for the time being"
#endif // HAVE_SYS_TIME_

#if HAVE_SYS_MMAN_H
 #include <sys/mman.h>
#else
 #error "sys/mman.h required by GC PAL"
#endif // HAVE_SYS_MMAN_H

#ifdef __linux__
#include <sys/syscall.h> // __NR_membarrier
// Ensure __NR_membarrier is defined for portable builds.
# if !defined(__NR_membarrier)
#  if defined(__amd64__)
#   define __NR_membarrier  324
#  elif defined(__i386__)
#   define __NR_membarrier  375
#  elif defined(__arm__)
#   define __NR_membarrier  389
#  elif defined(__aarch64__)
#   define __NR_membarrier  283
#  elif
#   error Unknown architecture
#  endif
# endif
#endif

#if HAVE_PTHREAD_NP_H
#include <pthread_np.h>
#endif

#if HAVE_CPUSET_T
typedef cpuset_t cpu_set_t;
#endif

#include <time.h> // nanosleep
#include <sched.h> // sched_yield
#include <errno.h>
#include <unistd.h> // sysconf
#include "globals.h"
#include "cgroup.h"

#if HAVE_NUMA_H

#include <numa.h>
#include <numaif.h>
#include <dlfcn.h>

// List of all functions from the numa library that are used
#define FOR_ALL_NUMA_FUNCTIONS \
    PER_FUNCTION_BLOCK(mbind) \
    PER_FUNCTION_BLOCK(numa_available) \
    PER_FUNCTION_BLOCK(numa_max_node) \
    PER_FUNCTION_BLOCK(numa_node_of_cpu)

// Declare pointers to all the used numa functions
#define PER_FUNCTION_BLOCK(fn) extern decltype(fn)* fn##_ptr;
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK

// Redefine all calls to numa functions as calls through pointers that are set
// to the functions of libnuma in the initialization.
#define mbind(...) mbind_ptr(__VA_ARGS__)
#define numa_available() numa_available_ptr()
#define numa_max_node() numa_max_node_ptr()
#define numa_node_of_cpu(...) numa_node_of_cpu_ptr(__VA_ARGS__)

#endif // HAVE_NUMA_H

#if defined(_ARM_) || defined(_ARM64_)
#define SYSCONF_GET_NUMPROCS _SC_NPROCESSORS_CONF
#else
#define SYSCONF_GET_NUMPROCS _SC_NPROCESSORS_ONLN
#endif

// The cached total number of CPUs that can be used in the OS.
static uint32_t g_totalCpuCount = 0;

// The cached number of CPUs available for the current process.
static uint32_t g_currentProcessCpuCount = 0;

//
// Helper membarrier function
//
#ifdef __NR_membarrier
# define membarrier(...)  syscall(__NR_membarrier, __VA_ARGS__)
#else
# define membarrier(...)  -ENOSYS
#endif

enum membarrier_cmd
{
    MEMBARRIER_CMD_QUERY                                 = 0,
    MEMBARRIER_CMD_GLOBAL                                = (1 << 0),
    MEMBARRIER_CMD_GLOBAL_EXPEDITED                      = (1 << 1),
    MEMBARRIER_CMD_REGISTER_GLOBAL_EXPEDITED             = (1 << 2),
    MEMBARRIER_CMD_PRIVATE_EXPEDITED                     = (1 << 3),
    MEMBARRIER_CMD_REGISTER_PRIVATE_EXPEDITED            = (1 << 4),
    MEMBARRIER_CMD_PRIVATE_EXPEDITED_SYNC_CORE           = (1 << 5),
    MEMBARRIER_CMD_REGISTER_PRIVATE_EXPEDITED_SYNC_CORE  = (1 << 6)
};

//
// Tracks if the OS supports FlushProcessWriteBuffers using membarrier
//
static int s_flushUsingMemBarrier = 0;

// Helper memory page used by the FlushProcessWriteBuffers
static uint8_t* g_helperPage = 0;

// Mutex to make the FlushProcessWriteBuffersMutex thread safe
static pthread_mutex_t g_flushProcessWriteBuffersMutex;

size_t GetRestrictedPhysicalMemoryLimit();
bool GetPhysicalMemoryUsed(size_t* val);
bool GetCpuLimit(uint32_t* val);

static size_t g_RestrictedPhysicalMemoryLimit = 0;

uint32_t g_pageSizeUnixInl = 0;

AffinitySet g_processAffinitySet;

// The highest NUMA node available
int g_highestNumaNode = 0;
// Is numa available
bool g_numaAvailable = false;

void* g_numaHandle = nullptr;

#if HAVE_NUMA_H
#define PER_FUNCTION_BLOCK(fn) decltype(fn)* fn##_ptr;
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK
#endif // HAVE_NUMA_H


// Initialize data structures for getting and setting thread affinities to processors and
// querying NUMA related processor information.
// On systems with no NUMA support, it behaves as if there was a single NUMA node with
// a single group of processors.
void NUMASupportInitialize()
{
#if HAVE_NUMA_H
    g_numaHandle = dlopen("libnuma.so", RTLD_LAZY);
    if (g_numaHandle == 0)
    {
        g_numaHandle = dlopen("libnuma.so.1", RTLD_LAZY);
    }
    if (g_numaHandle != 0)
    {
        dlsym(g_numaHandle, "numa_allocate_cpumask");
#define PER_FUNCTION_BLOCK(fn) \
    fn##_ptr = (decltype(fn)*)dlsym(g_numaHandle, #fn); \
    if (fn##_ptr == NULL) { fprintf(stderr, "Cannot get symbol " #fn " from libnuma\n"); abort(); }
FOR_ALL_NUMA_FUNCTIONS
#undef PER_FUNCTION_BLOCK

        if (numa_available() == -1)
        {
            dlclose(g_numaHandle);
        }
        else
        {
            g_numaAvailable = true;
            g_highestNumaNode = numa_max_node();
        }
    }
#endif // HAVE_NUMA_H
    if (!g_numaAvailable)
    {
        // No NUMA
        g_highestNumaNode = 0;
    }
}

// Cleanup of the NUMA support data structures
void NUMASupportCleanup()
{
#if HAVE_NUMA_H
    if (g_numaAvailable)
    {
        dlclose(g_numaHandle);
    }
#endif // HAVE_NUMA_H
}

// Initialize the interface implementation
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::Initialize()
{
    int pageSize = sysconf( _SC_PAGE_SIZE );

    g_pageSizeUnixInl = uint32_t((pageSize > 0) ? pageSize : 0x1000);

    // Calculate and cache the number of processors on this machine
    int cpuCount = sysconf(SYSCONF_GET_NUMPROCS);
    if (cpuCount == -1)
    {
        return false;
    }

    g_totalCpuCount = cpuCount;

    //
    // support for FlusProcessWriteBuffers
    //

    assert(s_flushUsingMemBarrier == 0);

    // Starting with Linux kernel 4.14, process memory barriers can be generated
    // using MEMBARRIER_CMD_PRIVATE_EXPEDITED.
    int mask = membarrier(MEMBARRIER_CMD_QUERY, 0);
    if (mask >= 0 &&
        mask & MEMBARRIER_CMD_PRIVATE_EXPEDITED &&
        // Register intent to use the private expedited command.
        membarrier(MEMBARRIER_CMD_REGISTER_PRIVATE_EXPEDITED, 0) == 0)
    {
        s_flushUsingMemBarrier = TRUE;
    }
    else
    {
        assert(g_helperPage == 0);

        g_helperPage = static_cast<uint8_t*>(mmap(0, OS_PAGE_SIZE, PROT_READ | PROT_WRITE, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0));

        if (g_helperPage == MAP_FAILED)
        {
            return false;
        }

        // Verify that the s_helperPage is really aligned to the g_SystemInfo.dwPageSize
        assert((((size_t)g_helperPage) & (OS_PAGE_SIZE - 1)) == 0);

        // Locking the page ensures that it stays in memory during the two mprotect
        // calls in the FlushProcessWriteBuffers below. If the page was unmapped between
        // those calls, they would not have the expected effect of generating IPI.
        int status = mlock(g_helperPage, OS_PAGE_SIZE);

        if (status != 0)
        {
            return false;
        }

        status = pthread_mutex_init(&g_flushProcessWriteBuffersMutex, NULL);
        if (status != 0)
        {
            munlock(g_helperPage, OS_PAGE_SIZE);
            return false;
        }
    }

#if HAVE_MACH_ABSOLUTE_TIME
    kern_return_t machRet;
    if ((machRet = mach_timebase_info(&g_TimebaseInfo)) != KERN_SUCCESS)
    {
        return false;
    }
#endif // HAVE_MACH_ABSOLUTE_TIME

    InitializeCGroup();

#if HAVE_SCHED_GETAFFINITY

    g_currentProcessCpuCount = 0;

    cpu_set_t cpuSet;
    int st = sched_getaffinity(0, sizeof(cpu_set_t), &cpuSet);

    if (st == 0)
    {
        for (size_t i = 0; i < g_totalCpuCount; i++)
        {
            if (CPU_ISSET(i, &cpuSet))
            {
                g_currentProcessCpuCount++;
                g_processAffinitySet.Add(i);
            }
        }
    }
    else
    {
        // We should not get any of the errors that the sched_getaffinity can return since none
        // of them applies for the current thread, so this is an unexpected kind of failure.
        assert(false);
    }

#else // HAVE_SCHED_GETAFFINITY

    g_currentProcessCpuCount = g_totalCpuCount;

    for (size_t i = 0; i < g_totalCpuCount; i++)
    {
        g_processAffinitySet.Add(i);
    }

#endif // HAVE_SCHED_GETAFFINITY

    uint32_t cpuLimit;
    if (GetCpuLimit(&cpuLimit) && cpuLimit < g_currentProcessCpuCount)
    {
        g_currentProcessCpuCount = cpuLimit;
    }

    NUMASupportInitialize();

    return true;
}

// Shutdown the interface implementation
void GCToOSInterface::Shutdown()
{
    int ret = munlock(g_helperPage, OS_PAGE_SIZE);
    assert(ret == 0);
    ret = pthread_mutex_destroy(&g_flushProcessWriteBuffersMutex);
    assert(ret == 0);

    munmap(g_helperPage, OS_PAGE_SIZE);

    CleanupCGroup();
    NUMASupportCleanup();
}

// Get numeric id of the current thread if possible on the
// current platform. It is indended for logging purposes only.
// Return:
//  Numeric id of the current thread, as best we can retrieve it.
uint64_t GCToOSInterface::GetCurrentThreadIdForLogging()
{
#if defined(__linux__)
    return (uint64_t)syscall(SYS_gettid);
#elif HAVE_PTHREAD_GETTHREADID_NP
    return (uint64_t)pthread_getthreadid_np();
#elif HAVE_PTHREAD_THREADID_NP
    unsigned long long tid;
    pthread_threadid_np(pthread_self(), &tid);
    return (uint64_t)tid;
#else
    // Fallback in case we don't know how to get integer thread id on the current platform
    return (uint64_t)pthread_self();
#endif
}

// Get the process ID of the process.
uint32_t GCToOSInterface::GetCurrentProcessId()
{
    return getpid();
}

// Set ideal processor for the current thread
// Parameters:
//  srcProcNo - processor number the thread currently runs on
//  dstProcNo - processor number the thread should be migrated to
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::SetCurrentThreadIdealAffinity(uint16_t srcProcNo, uint16_t dstProcNo)
{
    // There is no way to set a thread ideal processor on Unix, so do nothing.
    return true;
}

// Get the number of the current processor
uint32_t GCToOSInterface::GetCurrentProcessorNumber()
{
#if HAVE_SCHED_GETCPU
    int processorNumber = sched_getcpu();
    assert(processorNumber != -1);
    return processorNumber;
#else
    return 0;
#endif
}

// Check if the OS supports getting current processor number
bool GCToOSInterface::CanGetCurrentProcessorNumber()
{
    return HAVE_SCHED_GETCPU;
}

// Flush write buffers of processors that are executing threads of the current process
void GCToOSInterface::FlushProcessWriteBuffers()
{
    if (s_flushUsingMemBarrier)
    {
        int status = membarrier(MEMBARRIER_CMD_PRIVATE_EXPEDITED, 0);
        assert(status == 0 && "Failed to flush using membarrier");
    }
    else
    {
        int status = pthread_mutex_lock(&g_flushProcessWriteBuffersMutex);
        assert(status == 0 && "Failed to lock the flushProcessWriteBuffersMutex lock");

        // Changing a helper memory page protection from read / write to no access
        // causes the OS to issue IPI to flush TLBs on all processors. This also
        // results in flushing the processor buffers.
        status = mprotect(g_helperPage, OS_PAGE_SIZE, PROT_READ | PROT_WRITE);
        assert(status == 0 && "Failed to change helper page protection to read / write");

        // Ensure that the page is dirty before we change the protection so that
        // we prevent the OS from skipping the global TLB flush.
        __sync_add_and_fetch((size_t*)g_helperPage, 1);

        status = mprotect(g_helperPage, OS_PAGE_SIZE, PROT_NONE);
        assert(status == 0 && "Failed to change helper page protection to no access");

        status = pthread_mutex_unlock(&g_flushProcessWriteBuffersMutex);
        assert(status == 0 && "Failed to unlock the flushProcessWriteBuffersMutex lock");
    }
}

// Break into a debugger. Uses a compiler intrinsic if one is available,
// otherwise raises a SIGTRAP.
void GCToOSInterface::DebugBreak()
{
    // __has_builtin is only defined by clang. GCC doesn't have a debug
    // trap intrinsic anyway.
#ifndef __has_builtin
 #define __has_builtin(x) 0
#endif // __has_builtin

#if __has_builtin(__builtin_debugtrap)
    __builtin_debugtrap();
#else
    raise(SIGTRAP);
#endif
}

// Causes the calling thread to sleep for the specified number of milliseconds
// Parameters:
//  sleepMSec   - time to sleep before switching to another thread
void GCToOSInterface::Sleep(uint32_t sleepMSec)
{
    if (sleepMSec == 0)
    {
        return;
    }

    timespec requested;
    requested.tv_sec = sleepMSec / tccSecondsToMilliSeconds;
    requested.tv_nsec = (sleepMSec - requested.tv_sec * tccSecondsToMilliSeconds) * tccMilliSecondsToNanoSeconds;

    timespec remaining;
    while (nanosleep(&requested, &remaining) == EINTR)
    {
        requested = remaining;
    }
}

// Causes the calling thread to yield execution to another thread that is ready to run on the current processor.
// Parameters:
//  switchCount - number of times the YieldThread was called in a loop
void GCToOSInterface::YieldThread(uint32_t switchCount)
{
    int ret = sched_yield();

    // sched_yield never fails on Linux, unclear about other OSes
    assert(ret == 0);
}

// Reserve virtual memory range.
// Parameters:
//  size      - size of the virtual memory range
//  alignment - requested memory alignment, 0 means no specific alignment requested
//  flags     - flags to control special settings like write watching
// Return:
//  Starting virtual address of the reserved range
static void* VirtualReserveInner(size_t size, size_t alignment, uint32_t flags, uint32_t hugePagesFlag = 0)
{
    assert(!(flags & VirtualReserveFlags::WriteWatch) && "WriteWatch not supported on Unix");
    if (alignment == 0)
    {
        alignment = OS_PAGE_SIZE;
    }

    size_t alignedSize = size + (alignment - OS_PAGE_SIZE);
    void * pRetVal = mmap(nullptr, alignedSize, PROT_NONE, MAP_ANON | MAP_PRIVATE | hugePagesFlag, -1, 0);

    if (pRetVal != NULL)
    {
        void * pAlignedRetVal = (void *)(((size_t)pRetVal + (alignment - 1)) & ~(alignment - 1));
        size_t startPadding = (size_t)pAlignedRetVal - (size_t)pRetVal;
        if (startPadding != 0)
        {
            int ret = munmap(pRetVal, startPadding);
            assert(ret == 0);
        }

        size_t endPadding = alignedSize - (startPadding + size);
        if (endPadding != 0)
        {
            int ret = munmap((void *)((size_t)pAlignedRetVal + size), endPadding);
            assert(ret == 0);
        }

        pRetVal = pAlignedRetVal;
    }

    return pRetVal;
}

// Reserve virtual memory range.
// Parameters:
//  size      - size of the virtual memory range
//  alignment - requested memory alignment, 0 means no specific alignment requested
//  flags     - flags to control special settings like write watching
// Return:
//  Starting virtual address of the reserved range
void* GCToOSInterface::VirtualReserve(size_t size, size_t alignment, uint32_t flags)
{
    return VirtualReserveInner(size, alignment, flags);
}

// Release virtual memory range previously reserved using VirtualReserve
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualRelease(void* address, size_t size)
{
    int ret = munmap(address, size);

    return (ret == 0);
}

// Commit virtual memory range.
// Parameters:
//  size      - size of the virtual memory range
// Return:
//  Starting virtual address of the committed range
void* GCToOSInterface::VirtualReserveAndCommitLargePages(size_t size)
{
#if HAVE_MAP_HUGETLB
    uint32_t largePagesFlag = MAP_HUGETLB;
#elif HAVE_VM_FLAGS_SUPERPAGE_SIZE_ANY
    uint32_t largePagesFlag = VM_FLAGS_SUPERPAGE_SIZE_ANY;
#else
    uint32_t largePagesFlag = 0;
#endif

    void* pRetVal = VirtualReserveInner(size, OS_PAGE_SIZE, 0, largePagesFlag);
    if (VirtualCommit(pRetVal, size, NUMA_NODE_UNDEFINED))
    {
        return pRetVal;
    }

    return nullptr;
}

// Commit virtual memory range. It must be part of a range reserved using VirtualReserve.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualCommit(void* address, size_t size, uint16_t node)
{
    bool success = mprotect(address, size, PROT_WRITE | PROT_READ) == 0;

#if HAVE_NUMA_H
    if (success && g_numaAvailable && (node != NUMA_NODE_UNDEFINED))
    {
        if ((int)node <= g_highestNumaNode)
        {
            int usedNodeMaskBits = g_highestNumaNode + 1;
            int nodeMaskLength = (usedNodeMaskBits + sizeof(unsigned long) - 1) / sizeof(unsigned long);
            unsigned long nodeMask[nodeMaskLength];
            memset(nodeMask, 0, sizeof(nodeMask));

            int index = node / sizeof(unsigned long);
            nodeMask[index] = ((unsigned long)1) << (node & (sizeof(unsigned long) - 1));

            int st = mbind(address, size, MPOL_PREFERRED, nodeMask, usedNodeMaskBits, 0);
            assert(st == 0);
            // If the mbind fails, we still return the allocated memory since the node is just a hint
        }
    }
#endif // HAVE_NUMA_H

    return success;
}

// Decomit virtual memory range.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualDecommit(void* address, size_t size)
{
    // TODO: This can fail, however the GC does not handle the failure gracefully
    // Explicitly calling mmap instead of mprotect here makes it
    // that much more clear to the operating system that we no
    // longer need these pages. Also, GC depends on re-commited pages to
    // be zeroed-out.
    return mmap(address, size, PROT_NONE, MAP_FIXED | MAP_ANON | MAP_PRIVATE, -1, 0) != NULL;
}

// Reset virtual memory range. Indicates that data in the memory range specified by address and size is no
// longer of interest, but it should not be decommitted.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
//  unlock  - true if the memory range should also be unlocked
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualReset(void * address, size_t size, bool unlock)
{
    int st;
#if HAVE_MADV_FREE
    // Try to use MADV_FREE if supported. It tells the kernel that the application doesn't
    // need the pages in the range. Freeing the pages can be delayed until a memory pressure
    // occurs.
    st = madvise(address, size, MADV_FREE);
    if (st != 0)
#endif    
    {
        // In case the MADV_FREE is not supported, use MADV_DONTNEED
        st = madvise(address, size, MADV_DONTNEED);
    }

    return (st == 0);
}

// Check if the OS supports write watching
bool GCToOSInterface::SupportsWriteWatch()
{
    return false;
}

// Reset the write tracking state for the specified virtual memory range.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
void GCToOSInterface::ResetWriteWatch(void* address, size_t size)
{
    assert(!"should never call ResetWriteWatch on Unix");
}

// Retrieve addresses of the pages that are written to in a region of virtual memory
// Parameters:
//  resetState         - true indicates to reset the write tracking state
//  address            - starting virtual address
//  size               - size of the virtual memory range
//  pageAddresses      - buffer that receives an array of page addresses in the memory region
//  pageAddressesCount - on input, size of the lpAddresses array, in array elements
//                       on output, the number of page addresses that are returned in the array.
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::GetWriteWatch(bool resetState, void* address, size_t size, void** pageAddresses, uintptr_t* pageAddressesCount)
{
    assert(!"should never call GetWriteWatch on Unix");
    return false;
}

// Get size of the largest cache on the processor die
// Parameters:
//  trueSize - true to return true cache size, false to return scaled up size based on
//             the processor architecture
// Return:
//  Size of the cache
size_t GCToOSInterface::GetCacheSizePerLogicalCpu(bool trueSize)
{
    // TODO(segilles) processor detection
    return 0;
}

// Sets the calling thread's affinity to only run on the processor specified
// Parameters:
//  procNo - The requested processor for the calling thread.
// Return:
//  true if setting the affinity was successful, false otherwise.
bool GCToOSInterface::SetThreadAffinity(uint16_t procNo)
{
#if HAVE_PTHREAD_GETAFFINITY_NP
    cpu_set_t cpuSet;
    CPU_ZERO(&cpuSet);
    CPU_SET((int)procNo, &cpuSet);

    int st = pthread_setaffinity_np(pthread_self(), sizeof(cpu_set_t), &cpuSet);

    return (st == 0);

#else  // HAVE_PTHREAD_GETAFFINITY_NP
    // There is no API to manage thread affinity, so let's ignore the request
    return false;
#endif // HAVE_PTHREAD_GETAFFINITY_NP
}

// Boosts the calling thread's thread priority to a level higher than the default
// for new threads.
// Parameters:
//  None.
// Return:
//  true if the priority boost was successful, false otherwise.
bool GCToOSInterface::BoostThreadPriority()
{
    // [LOCALGC TODO] Thread priority for unix
    return false;
}

// Set the set of processors enabled for GC threads for the current process based on config specified affinity mask and set
// Parameters:
//  configAffinityMask - mask specified by the GCHeapAffinitizeMask config
//  configAffinitySet  - affinity set specified by the GCHeapAffinitizeRanges config
// Return:
//  set of enabled processors
const AffinitySet* GCToOSInterface::SetGCThreadsAffinitySet(uintptr_t configAffinityMask, const AffinitySet* configAffinitySet)
{
    if (!configAffinitySet->IsEmpty())
    {
        // Update the process affinity set using the configured set
        for (size_t i = 0; i < MAX_SUPPORTED_CPUS; i++)
        {
            if (g_processAffinitySet.Contains(i) && !configAffinitySet->Contains(i))
            {
                g_processAffinitySet.Remove(i);
            }
        }
    }

    return &g_processAffinitySet;
}

// Get number of processors assigned to the current process
// Return:
//  The number of processors
uint32_t GCToOSInterface::GetCurrentProcessCpuCount()
{
    return g_currentProcessCpuCount;
}

// Return the size of the user-mode portion of the virtual address space of this process.
// Return:
//  non zero if it has succeeded, 0 if it has failed
size_t GCToOSInterface::GetVirtualMemoryLimit()
{
#ifdef BIT64
    // There is no API to get the total virtual address space size on
    // Unix, so we use a constant value representing 128TB, which is
    // the approximate size of total user virtual address space on
    // the currently supported Unix systems.
    static const uint64_t _128TB = (1ull << 47);
    return _128TB;
#else
    return (size_t)-1;
#endif
}

// Get the physical memory that this process can use.
// Return:
//  non zero if it has succeeded, 0 if it has failed
// Remarks:
//  If a process runs with a restricted memory limit, it returns the limit. If there's no limit 
//  specified, it returns amount of actual physical memory.
uint64_t GCToOSInterface::GetPhysicalMemoryLimit(bool* is_restricted)
{
    size_t restricted_limit;
    if (is_restricted)
        *is_restricted = false;

    // The limit was not cached
    if (g_RestrictedPhysicalMemoryLimit == 0)
    {
        restricted_limit = GetRestrictedPhysicalMemoryLimit();
        VolatileStore(&g_RestrictedPhysicalMemoryLimit, restricted_limit);
    }
    restricted_limit = g_RestrictedPhysicalMemoryLimit;

    if (restricted_limit != 0 && restricted_limit != SIZE_T_MAX)
    {
        if (is_restricted)
            *is_restricted = true;
        return restricted_limit;
    }

    long pages = sysconf(_SC_PHYS_PAGES);
    if (pages == -1) 
    {
        return 0;
    }

    long pageSize = sysconf(_SC_PAGE_SIZE);
    if (pageSize == -1)
    {
        return 0;
    }

    return pages * pageSize;
}

// Get memory status
// Parameters:
//  memory_load - A number between 0 and 100 that specifies the approximate percentage of physical memory
//      that is in use (0 indicates no memory use and 100 indicates full memory use).
//  available_physical - The amount of physical memory currently available, in bytes.
//  available_page_file - The maximum amount of memory the current process can commit, in bytes.
void GCToOSInterface::GetMemoryStatus(uint32_t* memory_load, uint64_t* available_physical, uint64_t* available_page_file)
{
    if (memory_load != nullptr || available_physical != nullptr)
    {
        uint64_t total = GetPhysicalMemoryLimit();

        uint64_t available = 0;
        uint32_t load = 0;
        size_t used;

        // Get the physical memory in use - from it, we can get the physical memory available.
        // We do this only when we have the total physical memory available.
        if (total > 0 && GetPhysicalMemoryUsed(&used))
        {
            available = total > used ? total-used : 0; 
            load = (uint32_t)(((float)used * 100) / (float)total);
        }

        if (memory_load != nullptr)
            *memory_load = load;
        if (available_physical != nullptr)
            *available_physical = available;
    }

    if (available_page_file != nullptr)
        *available_page_file = 0;
}

// Get a high precision performance counter
// Return:
//  The counter value
int64_t GCToOSInterface::QueryPerformanceCounter()
{
    // TODO: This is not a particularly efficient implementation - we certainly could
    // do much more specific platform-dependent versions if we find that this method
    // runs hot. However, most likely it does not.
    struct timeval tv;
    if (gettimeofday(&tv, NULL) == -1)
    {
        assert(!"gettimeofday() failed");
        // TODO (segilles) unconditional asserts
        return 0;
    }
    return (int64_t) tv.tv_sec * (int64_t) tccSecondsToMicroSeconds + (int64_t) tv.tv_usec;
}

// Get a frequency of the high precision performance counter
// Return:
//  The counter frequency
int64_t GCToOSInterface::QueryPerformanceFrequency()
{
    // The counter frequency of gettimeofday is in microseconds.
    return tccSecondsToMicroSeconds;
}

// Get a time stamp with a low precision
// Return:
//  Time stamp in milliseconds
uint32_t GCToOSInterface::GetLowPrecisionTimeStamp()
{
    // TODO(segilles) this is pretty naive, we can do better
    uint64_t retval = 0;
    struct timeval tv;
    if (gettimeofday(&tv, NULL) == 0)
    {
        retval = (tv.tv_sec * tccSecondsToMilliSeconds) + (tv.tv_usec / tccMilliSecondsToMicroSeconds);
    }
    else
    {
        assert(!"gettimeofday() failed\n");
    }

    return retval;
}

// Gets the total number of processors on the machine, not taking
// into account current process affinity.
// Return:
//  Number of processors on the machine
uint32_t GCToOSInterface::GetTotalProcessorCount()
{
    // Calculated in GCToOSInterface::Initialize using
    // sysconf(_SC_NPROCESSORS_ONLN)
    return g_totalCpuCount;
}

bool GCToOSInterface::CanEnableGCNumaAware()
{
    return g_numaAvailable;
}

// Get processor number and optionally its NUMA node number for the specified heap number
// Parameters:
//  heap_number - heap number to get the result for
//  proc_no     - set to the selected processor number
//  node_no     - set to the NUMA node of the selected processor or to NUMA_NODE_UNDEFINED
// Return:
//  true if it succeeded
bool GCToOSInterface::GetProcessorForHeap(uint16_t heap_number, uint16_t* proc_no, uint16_t* node_no)
{
    bool success = false;

    uint16_t availableProcNumber = 0;
    for (size_t procNumber = 0; procNumber < g_totalCpuCount; procNumber++)
    {
        if (g_processAffinitySet.Contains(procNumber))
        {
            if (availableProcNumber == heap_number)
            {
                *proc_no = procNumber;
#if HAVE_NUMA_H
                if (GCToOSInterface::CanEnableGCNumaAware())
                {
                    int result = numa_node_of_cpu(procNumber);
                    *node_no = (result >= 0) ? (uint16_t)result : NUMA_NODE_UNDEFINED;
                }
                else
#endif // HAVE_NUMA_H
                {
                    *node_no = NUMA_NODE_UNDEFINED;
                }

                success = true;
                break;
            }
            availableProcNumber++;
        }
    }

    return success;
}

// Parse the confing string describing affinitization ranges and update the passed in affinitySet accordingly
// Parameters:
//  config_string - string describing the affinitization range, platform specific
//  start_index  - the range start index extracted from the config_string
//  end_index    - the range end index extracted from the config_string, equal to the start_index if only an index and not a range was passed in
// Return:
//  true if the configString was successfully parsed, false if it was not correct
bool GCToOSInterface::ParseGCHeapAffinitizeRangesEntry(const char** config_string, size_t* start_index, size_t* end_index)
{
    return ParseIndexOrRange(config_string, start_index, end_index);
}

// Initialize the critical section
void CLRCriticalSection::Initialize()
{
    int st = pthread_mutex_init(&m_cs.mutex, NULL);
    assert(st == 0);
}

// Destroy the critical section
void CLRCriticalSection::Destroy()
{
    int st = pthread_mutex_destroy(&m_cs.mutex);
    assert(st == 0);
}

// Enter the critical section. Blocks until the section can be entered.
void CLRCriticalSection::Enter()
{
    pthread_mutex_lock(&m_cs.mutex);
}

// Leave the critical section
void CLRCriticalSection::Leave()
{
    pthread_mutex_unlock(&m_cs.mutex);
}
