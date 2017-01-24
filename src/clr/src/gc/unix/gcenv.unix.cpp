// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cstdint>
#include <cstddef>
#include <cassert>
#include <memory>

// The CoreCLR PAL defines _POSIX_C_SOURCE to avoid calling non-posix pthread functions.
// This isn't something we want, because we're totally fine using non-posix functions.
#if defined(__APPLE__)
 #define _DARWIN_C_SOURCE
#endif // definfed(__APPLE__)

#include <pthread.h>
#include <signal.h>
#include "config.h"

// clang typedefs uint64_t to be unsigned long long, which clashes with
// PAL/MSVC's unsigned long, causing linker errors. This ugly hack
// will go away once the GC doesn't depend on PAL headers.
typedef unsigned long uint64_t_hack;
#define uint64_t uint64_t_hack
static_assert(sizeof(uint64_t) == 8, "unsigned long isn't 8 bytes");

#ifndef __out_z
#define __out_z
#endif // __out_z

#include "gcenv.structs.h"
#include "gcenv.base.h"
#include "gcenv.os.h"

#ifndef FEATURE_STANDALONE_GC
 #error "A GC-private implementation of GCToOSInterface should only be used with FEATURE_STANDALONE_GC"
#endif // FEATURE_STANDALONE_GC

#ifdef HAVE_SYS_TIME_H
 #include <sys/time.h>
#else
 #error "sys/time.h required by GC PAL for the time being"
#endif // HAVE_SYS_TIME_

#ifdef HAVE_SYS_MMAN_H
 #include <sys/mman.h>
#else
 #error "sys/mman.h required by GC PAL"
#endif // HAVE_SYS_MMAN_H

#ifdef __linux__
 #include <sys/syscall.h>
#endif // __linux__

#include <time.h> // nanosleep
#include <sched.h> // sched_yield
#include <errno.h>
#include <unistd.h> // sysconf

// The number of milliseconds in a second.
static const int tccSecondsToMilliSeconds = 1000;

// The number of microseconds in a second.
static const int tccSecondsToMicroSeconds = 1000000;

// The number of microseconds in a millisecond.
static const int tccMilliSecondsToMicroSeconds = 1000;

// The number of nanoseconds in a millisecond.
static const int tccMilliSecondsToNanoSeconds = 1000000;

// The cachced number of logical CPUs observed.
static uint32_t g_logicalCpuCount = 0;

// Helper memory page used by the FlushProcessWriteBuffers
static uint8_t g_helperPage[OS_PAGE_SIZE] __attribute__((aligned(OS_PAGE_SIZE)));

// Mutex to make the FlushProcessWriteBuffersMutex thread safe
static pthread_mutex_t g_flushProcessWriteBuffersMutex;

// Initialize the interface implementation
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::Initialize()
{
    // Calculate and cache the number of processors on this machine
    int cpuCount = sysconf(_SC_NPROCESSORS_ONLN);
    if (cpuCount == -1)
    {
        return false;
    }

    g_logicalCpuCount = cpuCount;

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

    return true;
}

// Shutdown the interface implementation
void GCToOSInterface::Shutdown()
{
    int ret = munlock(g_helperPage, OS_PAGE_SIZE);
    assert(ret == 0);
    ret = pthread_mutex_destroy(&g_flushProcessWriteBuffersMutex);
    assert(ret == 0);
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

// Set ideal affinity for the current thread
// Parameters:
//  affinity - ideal processor affinity for the thread
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::SetCurrentThreadIdealAffinity(GCThreadAffinity* affinity)
{
    // TODO(segilles)
    return false;
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

// Get number of logical processors
uint32_t GCToOSInterface::GetLogicalCpuCount()
{
    return g_logicalCpuCount;
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
void* GCToOSInterface::VirtualReserve(size_t size, size_t alignment, uint32_t flags)
{
    assert(!(flags & VirtualReserveFlags::WriteWatch) && "WriteWatch not supported on Unix");
    if (alignment == 0)
    {
        alignment = OS_PAGE_SIZE;
    }

    size_t alignedSize = size + (alignment - OS_PAGE_SIZE);
    void * pRetVal = mmap(nullptr, alignedSize, PROT_NONE, MAP_ANON | MAP_PRIVATE, -1, 0);

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

// Commit virtual memory range. It must be part of a range reserved using VirtualReserve.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualCommit(void* address, size_t size)
{
    return mprotect(address, size, PROT_WRITE | PROT_READ) == 0;
}

// Decomit virtual memory range.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualDecommit(void* address, size_t size)
{
    return mprotect(address, size, PROT_NONE) == 0;
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
    // TODO(CoreCLR#1259) pipe to madvise?
    return false;
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
size_t GCToOSInterface::GetLargestOnDieCacheSize(bool trueSize)
{
    // TODO(segilles) processor detection
    return 0;
}

// Get affinity mask of the current process
// Parameters:
//  processMask - affinity mask for the specified process
//  systemMask  - affinity mask for the system
// Return:
//  true if it has succeeded, false if it has failed
// Remarks:
//  A process affinity mask is a bit vector in which each bit represents the processors that
//  a process is allowed to run on. A system affinity mask is a bit vector in which each bit
//  represents the processors that are configured into a system.
//  A process affinity mask is a subset of the system affinity mask. A process is only allowed
//  to run on the processors configured into a system. Therefore, the process affinity mask cannot
//  specify a 1 bit for a processor when the system affinity mask specifies a 0 bit for that processor.
bool GCToOSInterface::GetCurrentProcessAffinityMask(uintptr_t* processMask, uintptr_t* systemMask)
{
    // TODO(segilles) processor detection
    return false;
}

// Get number of processors assigned to the current process
// Return:
//  The number of processors
uint32_t GCToOSInterface::GetCurrentProcessCpuCount()
{
    return g_logicalCpuCount;
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
uint64_t GCToOSInterface::GetPhysicalMemoryLimit()
{
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

        // Get the physical memory in use - from it, we can get the physical memory available.
        // We do this only when we have the total physical memory available.
        if (total > 0)
        {
            available = sysconf(_SC_PHYS_PAGES) * sysconf(_SC_PAGE_SIZE);
            uint64_t used = total - available;
            load = (uint32_t)((used * 100) / total);
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

// Parameters of the GC thread stub
struct GCThreadStubParam
{
    GCThreadFunction GCThreadFunction;
    void* GCThreadParam;
};

// GC thread stub to convert GC thread function to an OS specific thread function
static void* GCThreadStub(void* param)
{
    GCThreadStubParam *stubParam = (GCThreadStubParam*)param;
    GCThreadFunction function = stubParam->GCThreadFunction;
    void* threadParam = stubParam->GCThreadParam;

    delete stubParam;

    function(threadParam);

    return NULL;
}

// Create a new thread for GC use
// Parameters:
//  function - the function to be executed by the thread
//  param    - parameters of the thread
//  affinity - processor affinity of the thread
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::CreateThread(GCThreadFunction function, void* param, GCThreadAffinity* affinity)
{
    std::unique_ptr<GCThreadStubParam> stubParam(new (std::nothrow) GCThreadStubParam());
    if (!stubParam)
    {
        return false;
    }

    stubParam->GCThreadFunction = function;
    stubParam->GCThreadParam = param;

    pthread_attr_t attrs;

    int st = pthread_attr_init(&attrs);
    assert(st == 0);

    // Create the thread as detached, that means not joinable
    st = pthread_attr_setdetachstate(&attrs, PTHREAD_CREATE_DETACHED);
    assert(st == 0);

    pthread_t threadId;
    st = pthread_create(&threadId, &attrs, GCThreadStub, stubParam.get());

    if (st == 0)
    {
        stubParam.release();
    }

    int st2 = pthread_attr_destroy(&attrs);
    assert(st2 == 0);

    return (st == 0);
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
