// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WASM-specific GC OS interface implementation.
// Replaces gcenv.unix.cpp when targeting WebAssembly (browser or WASI).

#include <cstdint>
#include <cstddef>
#include <cstdio>
#include <cassert>
#include <cstring>
#include <cstdlib>

#include "config.gc.h"
#include "common.h"

#include "gcenv.structs.h"
#include "gcenv.base.h"
#include "gcenv.os.h"
#include "gcenv.ee.h"
#include "gcenv.unix.inl"
#include "gcconfig.h"

#include <unistd.h>

#ifdef FEATURE_MULTITHREADING
#include <errno.h>
#include <sched.h>
#include <time.h>
#endif

#include <minipal/thread.h>
#include <minipal/time.h>
#include <minipal/utils.h>
#include <minipal/ospagesize.h>

#include "globals.h"

#ifdef TARGET_BROWSER
#include <emscripten/heap.h>
#endif

// WASM memory.grow operates in 64KB pages. This is distinct from OS_PAGE_SIZE
// (the GC's page granularity), which we set to 16KB below.
static const size_t WasmPageSize = 64 * 1024;

// The cached total number of CPUs that can be used in the OS.
// WASM is single-threaded, so this is always 1.
uint32_t g_totalCpuCount = 1;

uint32_t g_pageSizeUnixInl = 0;

AffinitySet g_processAffinitySet;

// NUMA globals - WASM has no NUMA support but these are referenced by the GC.
extern "C" int g_highestNumaNode = 0;
extern "C" bool g_numaAvailable = false;

static int64_t g_totalPhysicalMemSize = 0;

// Forward declarations
static size_t GetRestrictedPhysicalMemoryLimit();
static bool GetPhysicalMemoryUsed(size_t* val);
static uint64_t GetAvailablePhysicalMemory();

// ============================================================================
// Initialization / Shutdown
// ============================================================================

bool GCToOSInterface::Initialize()
{
    g_pageSizeUnixInl = (uint32_t)minipal_getpagesize();

    // WASM is single-threaded
    g_totalCpuCount = 1;

    if (!g_processAffinitySet.Initialize(1))
    {
        return false;
    }

    g_processAffinitySet.Add(0);

    // Get the physical memory size
#ifdef TARGET_BROWSER
    g_totalPhysicalMemSize = (int64_t)emscripten_get_heap_max();
#else // TARGET_WASI
    // WASI doesn't have an API to query max memory.
    g_totalPhysicalMemSize = 2LL * 1024 * 1024 * 1024; // 2GB
#endif

    assert(g_totalPhysicalMemSize != 0);

    return true;
}

void GCToOSInterface::Shutdown()
{
}

// ============================================================================
// Thread / Process identification
// ============================================================================

uint64_t GCToOSInterface::GetCurrentThreadIdForLogging()
{
    return (uint64_t)minipal_get_current_thread_id();
}

uint32_t GCToOSInterface::GetCurrentProcessId()
{
    return getpid();
}

bool GCToOSInterface::SetCurrentThreadIdealAffinity(uint16_t srcProcNo, uint16_t dstProcNo)
{
    (void)srcProcNo;
    (void)dstProcNo;
    return true;
}

uint32_t GCToOSInterface::GetCurrentProcessorNumber()
{
    return 0;
}

bool GCToOSInterface::CanGetCurrentProcessorNumber()
{
    return true;
}

// ============================================================================
// Debugging / Sleeping / Yielding
// ============================================================================

void GCToOSInterface::DebugBreak()
{
#if __has_builtin(__builtin_debugtrap)
    __builtin_debugtrap();
#else
    abort();
#endif
}

void GCToOSInterface::Sleep(uint32_t sleepMSec)
{
#ifdef FEATURE_MULTITHREADING
    timespec requested =
    {
        static_cast<time_t>(sleepMSec / 1000),
        static_cast<long>((sleepMSec % 1000) * 1000000)
    };

    while (nanosleep(&requested, &requested) != 0 && errno == EINTR)
    {
    }
#else
    // On single-threaded WASM, nanosleep is either a no-op or stalls the
    // event loop. There are no other threads to wait for, and no signals
    // to deliver EINTR.
#endif
    (void)sleepMSec;
}

void GCToOSInterface::YieldThread(uint32_t switchCount)
{
#ifdef FEATURE_MULTITHREADING
    sched_yield();
#else
    // No-op on single-threaded WASM - there are no other threads to yield to.
#endif
    (void)switchCount;
}

// ============================================================================
// Virtual Memory - WASM-specific (posix_memalign / free)
// ============================================================================

// Emscripten does not provide a complete implementation of mmap and munmap:
// munmap cannot unmap partial allocations, mmap(PROT_NONE) still consumes
// linear memory, and MAP_FIXED is broken.
// Emscripten does provide an implementation of posix_memalign which is used here.
//
// posix_memalign returns either freshly grown linear memory (zero by the WASM
// spec) or a recycled block from the allocator's free list (which may contain
// stale data from a previous VirtualDecommit -> free cycle). Since we cannot
// portably distinguish the two without relying on dlmalloc implementation
// details, we always zero the returned memory to match VirtualReserve's
// "memory starts zeroed" contract.

static void* VirtualReserveInner(size_t size, size_t alignment, uint32_t flags)
{
    assert(!(flags & VirtualReserveFlags::WriteWatch) && "WriteWatch not supported on WASM");
    if (alignment < OS_PAGE_SIZE)
    {
        alignment = OS_PAGE_SIZE;
    }

    void* pRetVal;
    int result = posix_memalign(&pRetVal, alignment, size);
    if (result != 0)
    {
        return nullptr;
    }

    memset(pRetVal, 0, size);

    return pRetVal;
}

void* GCToOSInterface::VirtualReserve(size_t size, size_t alignment, uint32_t flags, uint16_t node)
{
    (void)node;
    return VirtualReserveInner(size, alignment, flags);
}

bool GCToOSInterface::VirtualRelease(void* address, size_t size)
{
    (void)size;
    free(address);
    return true;
}

void* GCToOSInterface::VirtualReserveAndCommitLargePages(size_t size, uint16_t node)
{
    (void)node;
    // WASM has no large pages - just reserve+commit normally.
    return VirtualReserveInner(size, OS_PAGE_SIZE, 0);
}

bool GCToOSInterface::VirtualCommit(void* address, size_t size, uint16_t node)
{
    // The GC skips this for heap memory when never_decommit_p is true (which
    // it always is on WASM). This is still called for bookkeeping memory.
    // Memory is always zero here: either from VirtualReserveInner (initial
    // allocation) or from VirtualDecommit (which zeroes on decommit).
    (void)address;
    (void)size;
    (void)node;
    return true;
}

bool GCToOSInterface::VirtualDecommit(void* address, size_t size)
{
    // The GC skips this for heap memory when never_decommit_p is true (which
    // it always is on WASM). This is still called for bookkeeping memory.
    // On WASM, we cannot return memory to the OS or change page protection.
    // Zero the range so it is clean for any future VirtualCommit (which is a no-op).
    memset(address, 0, size);
    return true;
}

bool GCToOSInterface::VirtualReset(void* address, size_t size, bool unlock)
{
    // Return false to indicate reset is not supported.
    // This forces the GC to use the decommit+commit fallback path instead.
    // On WASM, madvise is a no-op so reset cannot discard pages.
    (void)address;
    (void)size;
    (void)unlock;
    return false;
}

// ============================================================================
// Write Watch (not supported on WASM)
// ============================================================================

bool GCToOSInterface::SupportsWriteWatch()
{
    return false;
}

void GCToOSInterface::ResetWriteWatch(void* address, size_t size)
{
    assert(!"should never call ResetWriteWatch on WASM");
}

bool GCToOSInterface::GetWriteWatch(bool resetState, void* address, size_t size, void** pageAddresses, uintptr_t* pageAddressesCount)
{
    assert(!"should never call GetWriteWatch on WASM");
    return false;
}

// ============================================================================
// Processor cache
// ============================================================================

size_t GCToOSInterface::GetCacheSizePerLogicalCpu(bool trueSize)
{
    (void)trueSize;
    // WASM doesn't expose cache topology.
    // Return a reasonable default (256 KB).
    return 256 * 1024;
}

// ============================================================================
// Thread affinity / priority
// ============================================================================

bool GCToOSInterface::SetThreadAffinity(uint16_t procNo)
{
    (void)procNo;
    // No thread affinity on WASM
    return false;
}

bool GCToOSInterface::BoostThreadPriority()
{
    // No thread priority on WASM
    return false;
}

const AffinitySet* GCToOSInterface::SetGCThreadsAffinitySet(uintptr_t configAffinityMask, const AffinitySet* configAffinitySet)
{
    (void)configAffinityMask;
    (void)configAffinitySet;
    return &g_processAffinitySet;
}

// ============================================================================
// Virtual / Physical Memory Limits
// ============================================================================

static uint64_t GetTotalPhysicalMemory()
{
#ifdef TARGET_BROWSER
    return emscripten_get_heap_max();
#else // TARGET_WASI
    // WASI doesn't have an API to query max memory.
    return 2ULL * 1024 * 1024 * 1024; // 2GB
#endif
}

size_t GCToOSInterface::GetVirtualMemoryLimit()
{
    return GetVirtualMemoryMaxAddress();
}

size_t GCToOSInterface::GetVirtualMemoryMaxAddress()
{
    // On WASM, linear-memory ceiling = max addressable. Both APIs return the
    // same value because there is no separate per-process virtual address space
    // limit beyond what the engine permits the linear memory to grow to.
    return GetTotalPhysicalMemory();
}

static size_t GetRestrictedPhysicalMemoryLimit()
{
    // No restricted-memory mode on WASM. The linear memory ceiling enforced by the
    // engine is the only hard cap; we don't auto-derive a GC heap_hard_limit from it.
    return 0;
}

static bool GetPhysicalMemoryUsed(size_t* val)
{
    // __builtin_wasm_memory_size(0) returns count of 64KB WASM pages, not GC pages.
    // On wasm32 the spec maximum is 65536 pages = exactly 4GiB, which overflows
    // size_t (UINT32_MAX) by one. No engine actually permits allocating the full
    // 4GiB, but compute in uint64_t and clamp to be safe.
    uint64_t bytesUsed = static_cast<uint64_t>(__builtin_wasm_memory_size(0)) * WasmPageSize;
    *val = (bytesUsed > SIZE_MAX) ? SIZE_MAX : static_cast<size_t>(bytesUsed);
    return true;
}

uint64_t GCToOSInterface::GetPhysicalMemoryLimit(bool* is_restricted)
{
    if (is_restricted)
        *is_restricted = false;

    size_t restricted_limit = GetRestrictedPhysicalMemoryLimit();
    if (restricted_limit != 0 && restricted_limit != SIZE_T_MAX)
    {
        if (is_restricted)
            *is_restricted = true;
        return restricted_limit;
    }

    return g_totalPhysicalMemSize;
}

static uint64_t GetAvailablePhysicalMemory()
{
#ifdef TARGET_BROWSER
    return emscripten_get_heap_max() - emscripten_get_heap_size();
#else // TARGET_WASI
    // Best approximation: total minus currently used.
    // __builtin_wasm_memory_size(0) returns count of 64KB WASM pages, not GC pages.
    // Compute in 64 bits so a legitimate 0-page memory is not conflated with wasm32 overflow.
    uint64_t used = static_cast<uint64_t>(__builtin_wasm_memory_size(0)) * static_cast<uint64_t>(WasmPageSize);
    uint64_t total = GetTotalPhysicalMemory();
    return (total > used) ? (total - used) : 0;
#endif
}

static uint64_t GetAvailablePageFile()
{
    // No swap on WASM
    return 0;
}

void GCToOSInterface::GetMemoryStatus(uint64_t restricted_limit, uint32_t* memory_load, uint64_t* available_physical, uint64_t* available_page_file)
{
    uint64_t available = 0;
    uint32_t load = 0;

    size_t used;
    if (restricted_limit != 0)
    {
        if (GetPhysicalMemoryUsed(&used))
        {
            available = restricted_limit > used ? restricted_limit - used : 0;
            load = (uint32_t)(((float)used * 100) / (float)restricted_limit);
        }
    }
    else
    {
        available = GetAvailablePhysicalMemory();

        if (memory_load != nullptr)
        {
            uint64_t total = g_totalPhysicalMemSize;

            if (total > available)
            {
                used = total - available;
                load = (uint32_t)(((float)used * 100) / (float)total);
            }
        }
    }

    if (available_physical != nullptr)
        *available_physical = available;

    if (memory_load != nullptr)
        *memory_load = load;

    if (available_page_file != nullptr)
        *available_page_file = 0; // No page file on wasm
}

// ============================================================================
// Time
// ============================================================================

int64_t GCToOSInterface::QueryPerformanceCounter()
{
    return minipal_hires_ticks();
}

int64_t GCToOSInterface::QueryPerformanceFrequency()
{
    return minipal_hires_tick_frequency();
}

uint64_t GCToOSInterface::GetLowPrecisionTimeStamp()
{
    return (uint64_t)minipal_lowres_ticks();
}

// ============================================================================
// Processor count / NUMA / CPU Groups
// ============================================================================

uint32_t GCToOSInterface::GetTotalProcessorCount()
{
    return g_totalCpuCount;
}

uint32_t GCToOSInterface::GetMaxProcessorCount()
{
    return (uint32_t)g_processAffinitySet.MaxCpuCount();
}

bool GCToOSInterface::CanEnableGCNumaAware()
{
    return false;
}

bool GCToOSInterface::CanEnableGCCPUGroups()
{
    return false;
}

bool GCToOSInterface::GetProcessorForHeap(uint16_t heap_number, uint16_t* proc_no, uint16_t* node_no)
{
    if (heap_number == 0)
    {
        *proc_no = 0;
        *node_no = NUMA_NODE_UNDEFINED;
        return true;
    }

    return false;
}

bool GCToOSInterface::ParseGCHeapAffinitizeRangesEntry(const char** config_string, size_t* start_index, size_t* end_index)
{
    return ParseIndexOrRange(config_string, start_index, end_index);
}
