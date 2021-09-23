// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * gcenv.os.cpp
 *
 * GCToOSInterface implementation
 *

 *
 */

#include "common.h"
#include "gcenv.h"

#ifndef TARGET_UNIX
#include <Psapi.h>
#endif

#ifdef Sleep
#undef Sleep
#endif // Sleep

#include "../gc/env/gcenv.os.h"

#define MAX_PTR ((uint8_t*)(~(ptrdiff_t)0))

#ifdef TARGET_UNIX
uint32_t g_pageSizeUnixInl = 0;
#endif

static AffinitySet g_processAffinitySet;

class GroupProcNo
{
    uint16_t m_groupProc;

public:

    static const uint16_t NoGroup = 0;

    GroupProcNo(uint16_t groupProc) : m_groupProc(groupProc)
    {
    }

    GroupProcNo(uint16_t group, uint16_t procIndex) : m_groupProc((group << 6) | procIndex)
    {
        // Making this the same as the # of NUMA node we support.
        _ASSERTE(group < 0x40);
        _ASSERTE(procIndex <= 0x3f);
    }

    uint16_t GetGroup() { return m_groupProc >> 6; }
    uint16_t GetProcIndex() { return m_groupProc & 0x3f; }
    uint16_t GetCombinedValue() { return m_groupProc; }
};

#if !defined(TARGET_UNIX)

static bool g_SeLockMemoryPrivilegeAcquired = false;

bool InitLargePagesPrivilege()
{
    TOKEN_PRIVILEGES tp;
    LUID luid;
    if (!LookupPrivilegeValueW(nullptr, SE_LOCK_MEMORY_NAME, &luid))
    {
        return false;
    }

    tp.PrivilegeCount = 1;
    tp.Privileges[0].Luid = luid;
    tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

    HANDLE token;
    if (!OpenProcessToken(::GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES, &token))
    {
        return false;
    }

    BOOL retVal = AdjustTokenPrivileges(token, FALSE, &tp, 0, nullptr, 0);
    DWORD gls = GetLastError();
    CloseHandle(token);

    if (!retVal)
    {
        return false;
    }

    if (gls != 0)
    {
        return false;
    }

    return true;
}

#endif // TARGET_UNIX

static void GetProcessMemoryLoad(LPMEMORYSTATUSEX pMSEX)
{
    LIMITED_METHOD_CONTRACT;

    pMSEX->dwLength = sizeof(MEMORYSTATUSEX);
    BOOL fRet = GlobalMemoryStatusEx(pMSEX);
    _ASSERTE(fRet);
}

// Initialize the interface implementation
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::Initialize()
{
    LIMITED_METHOD_CONTRACT;

#ifdef TARGET_UNIX
    g_pageSizeUnixInl = GetOsPageSize();

    uint32_t currentProcessCpuCount = PAL_GetLogicalCpuCountFromOS();
    if (PAL_GetCurrentThreadAffinitySet(AffinitySet::BitsetDataSize, g_processAffinitySet.GetBitsetData()))
    {
        _ASSERTE(currentProcessCpuCount == g_processAffinitySet.Count());
    }
    else
    {
        // There is no way to get affinity on the current OS, set the affinity set to reflect all processors
        for (size_t i = 0; i < currentProcessCpuCount; i++)
        {
            g_processAffinitySet.Add(i);
        }
    }
#else // TARGET_UNIX
    if (CPUGroupInfo::CanEnableGCCPUGroups())
    {
        // When CPU groups are enabled, then the process is not bound by the process affinity set at process launch.
        // Set the initial affinity mask so that all processors are enabled.
        for (size_t i = 0; i < CPUGroupInfo::GetNumActiveProcessors(); i++)
        {
            g_processAffinitySet.Add(i);
        }
    }
    else
    {
        // When CPU groups are disabled, the process affinity mask specified at the process launch cannot be
        // escaped.
        uintptr_t pmask, smask;
        if (!!::GetProcessAffinityMask(::GetCurrentProcess(), (PDWORD_PTR)&pmask, (PDWORD_PTR)&smask))
        {
            pmask &= smask;

            for (size_t i = 0; i < 8 * sizeof(uintptr_t); i++)
            {
                if ((pmask & ((uintptr_t)1 << i)) != 0)
                {
                    g_processAffinitySet.Add(i);
                }
            }
        }
    }
#endif // TARGET_UNIX

    return true;
}

// Shutdown the interface implementation
void GCToOSInterface::Shutdown()
{
    LIMITED_METHOD_CONTRACT;
}

// Get numeric id of the current thread if possible on the
// current platform. It is indended for logging purposes only.
// Return:
//  Numeric id of the current thread or 0 if the
uint64_t GCToOSInterface::GetCurrentThreadIdForLogging()
{
    LIMITED_METHOD_CONTRACT;
    return ::GetCurrentThreadId();
}

// Get id of the process
// Return:
//  Id of the current process
uint32_t GCToOSInterface::GetCurrentProcessId()
{
    LIMITED_METHOD_CONTRACT;
    return ::GetCurrentProcessId();
}

// Set ideal processor for the current thread
// Parameters:
//  srcProcNo - processor number the thread currently runs on
//  dstProcNo - processor number the thread should be migrated to
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::SetCurrentThreadIdealAffinity(uint16_t srcProcNo, uint16_t dstProcNo)
{
    LIMITED_METHOD_CONTRACT;

    bool success = true;
#ifndef TARGET_UNIX
    GroupProcNo srcGroupProcNo(srcProcNo);
    GroupProcNo dstGroupProcNo(dstProcNo);

    PROCESSOR_NUMBER proc;

    if (CPUGroupInfo::CanEnableGCCPUGroups())
    {
        if (srcGroupProcNo.GetGroup() != dstGroupProcNo.GetGroup())
        {
            //only set ideal processor when srcProcNo and dstProcNo are in the same cpu
            //group. DO NOT MOVE THREADS ACROSS CPU GROUPS
            return true;
        }

        proc.Group = (WORD)dstGroupProcNo.GetGroup();
        proc.Number = (BYTE)dstGroupProcNo.GetProcIndex();
        proc.Reserved = 0;

        success = !!SetThreadIdealProcessorEx(GetCurrentThread (), &proc, NULL);
    }
    else
    {
        if (GetThreadIdealProcessorEx(GetCurrentThread(), &proc))
        {
            proc.Number = (BYTE)dstGroupProcNo.GetProcIndex();
            success = !!SetThreadIdealProcessorEx(GetCurrentThread(), &proc, &proc);
        }
    }

    return success;

#else // !TARGET_UNIX

    // There is no way to set a thread ideal processor on Unix, so do nothing.
    return true;

#endif // !TARGET_UNIX
}

bool GCToOSInterface::GetCurrentThreadIdealProc(uint16_t* procNo)
{
    LIMITED_METHOD_CONTRACT;
    bool success = false;
#ifndef TARGET_UNIX
    PROCESSOR_NUMBER proc;
    success = !!GetThreadIdealProcessorEx(GetCurrentThread(), &proc);
    if (success)
    {
        GroupProcNo groupProcNo(proc.Group, proc.Number);
        *procNo = groupProcNo.GetCombinedValue();
    }
#endif //TARGET_UNIX
    return success;
}

// Get the number of the current processor
uint32_t GCToOSInterface::GetCurrentProcessorNumber()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(CanGetCurrentProcessorNumber());

#ifndef TARGET_UNIX
    PROCESSOR_NUMBER proc_no_cpu_group;
    GetCurrentProcessorNumberEx(&proc_no_cpu_group);

    GroupProcNo groupProcNo(proc_no_cpu_group.Group, proc_no_cpu_group.Number);
    return groupProcNo.GetCombinedValue();
#else
    return ::GetCurrentProcessorNumber();
#endif //!TARGET_UNIX
}

// Check if the OS supports getting current processor number
bool GCToOSInterface::CanGetCurrentProcessorNumber()
{
    LIMITED_METHOD_CONTRACT;

#ifdef TARGET_UNIX
    return PAL_HasGetCurrentProcessorNumber();
#else
    // on all Windows platforms we support this API exists
    return true;
#endif
}

// Flush write buffers of processors that are executing threads of the current process
void GCToOSInterface::FlushProcessWriteBuffers()
{
    LIMITED_METHOD_CONTRACT;
    ::FlushProcessWriteBuffers();
}

// Break into a debugger
void GCToOSInterface::DebugBreak()
{
    LIMITED_METHOD_CONTRACT;
    ::DebugBreak();
}

// Causes the calling thread to sleep for the specified number of milliseconds
// Parameters:
//  sleepMSec   - time to sleep before switching to another thread
void GCToOSInterface::Sleep(uint32_t sleepMSec)
{
    LIMITED_METHOD_CONTRACT;
    __SwitchToThread(sleepMSec, 0);
}

// Causes the calling thread to yield execution to another thread that is ready to run on the current processor.
// Parameters:
//  switchCount - number of times the YieldThread was called in a loop
void GCToOSInterface::YieldThread(uint32_t switchCount)
{
    LIMITED_METHOD_CONTRACT;
    __SwitchToThread(0, switchCount);
}

// Reserve virtual memory range.
// Parameters:
//  address   - starting virtual address, it can be NULL to let the function choose the starting address
//  size      - size of the virtual memory range
//  alignment - requested memory alignment
//  flags     - flags to control special settings like write watching
//  node      - the NUMA node to reserve memory on
// Return:
//  Starting virtual address of the reserved range
void* GCToOSInterface::VirtualReserve(size_t size, size_t alignment, uint32_t flags, uint16_t node)
{
    LIMITED_METHOD_CONTRACT;

    DWORD memFlags = (flags & VirtualReserveFlags::WriteWatch) ? (MEM_RESERVE | MEM_WRITE_WATCH) : MEM_RESERVE;

    if (node == NUMA_NODE_UNDEFINED)
    {
        // This is not strictly necessary for a correctness standpoint. Windows already guarantees
        // allocation granularity alignment when using MEM_RESERVE, so aligning the size here has no effect.
        // However, ClrVirtualAlloc does expect the size to be aligned to the allocation granularity.
        size_t aligned_size = (size + g_SystemInfo.dwAllocationGranularity - 1) & ~static_cast<size_t>(g_SystemInfo.dwAllocationGranularity - 1);
        if (alignment == 0)
        {
            return ::ClrVirtualAlloc (0, aligned_size, memFlags, PAGE_READWRITE);
        }
        else
        {
            return ::ClrVirtualAllocAligned (0, aligned_size, memFlags, PAGE_READWRITE, alignment);
        }
    }
    else
    {
        return NumaNodeInfo::VirtualAllocExNuma (::GetCurrentProcess (), NULL, size, memFlags, PAGE_READWRITE, node);
    }
}

// Release virtual memory range previously reserved using VirtualReserve
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualRelease(void* address, size_t size)
{
    LIMITED_METHOD_CONTRACT;

    UNREFERENCED_PARAMETER(size);
    return !!::ClrVirtualFree(address, 0, MEM_RELEASE);
}

// Commit virtual memory range.
// Parameters:
//  size      - size of the virtual memory range
// Return:
//  Starting virtual address of the committed range
void* GCToOSInterface::VirtualReserveAndCommitLargePages(size_t size, uint16_t node)
{
    LIMITED_METHOD_CONTRACT;

#if !defined(TARGET_UNIX)
    if (!g_SeLockMemoryPrivilegeAcquired)
    {
        if (!InitLargePagesPrivilege())
        {
            return nullptr;
        }

        g_SeLockMemoryPrivilegeAcquired = true;
    }

    SIZE_T largePageMinimum = GetLargePageMinimum();
    size = (size + (largePageMinimum - 1)) & ~(largePageMinimum - 1);
#endif

    if (node == NUMA_NODE_UNDEFINED)
    {
        return ::ClrVirtualAlloc(nullptr, size, MEM_RESERVE | MEM_COMMIT | MEM_LARGE_PAGES, PAGE_READWRITE);
    }
    else
    {
        return NumaNodeInfo::VirtualAllocExNuma(::GetCurrentProcess(), nullptr, size, MEM_RESERVE | MEM_COMMIT | MEM_LARGE_PAGES, PAGE_READWRITE, node);
    }
}

// Commit virtual memory range. It must be part of a range reserved using VirtualReserve.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualCommit(void* address, size_t size, uint16_t node)
{
    LIMITED_METHOD_CONTRACT;

    if (node == NUMA_NODE_UNDEFINED)
    {
        return ::ClrVirtualAlloc(address, size, MEM_COMMIT, PAGE_READWRITE) != NULL;
    }
    else
    {
        return NumaNodeInfo::VirtualAllocExNuma(::GetCurrentProcess(), address, size, MEM_COMMIT, PAGE_READWRITE, node) != NULL;
    }
}

// Decomit virtual memory range.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualDecommit(void* address, size_t size)
{
    LIMITED_METHOD_CONTRACT;

    return !!::ClrVirtualFree(address, size, MEM_DECOMMIT);
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
    LIMITED_METHOD_CONTRACT;

    bool success = ::ClrVirtualAlloc(address, size, MEM_RESET, PAGE_READWRITE) != NULL;
#ifndef TARGET_UNIX
    if (success && unlock)
    {
        // Remove the page range from the working set
        ::VirtualUnlock(address, size);
    }
#endif // TARGET_UNIX

    return success;
}

// Check if the OS supports write watching
bool GCToOSInterface::SupportsWriteWatch()
{
    LIMITED_METHOD_CONTRACT;

#ifndef TARGET_UNIX
    bool writeWatchSupported = false;

    // check if the OS supports write-watch.
    // Drawbridge does not support write-watch so we still need to do the runtime detection for them.
    // Otherwise, all currently supported OSes do support write-watch.
    void* mem = VirtualReserve(g_SystemInfo.dwAllocationGranularity, 0, VirtualReserveFlags::WriteWatch);
    if (mem != NULL)
    {
        VirtualRelease (mem, g_SystemInfo.dwAllocationGranularity);
        writeWatchSupported = true;
    }

    return writeWatchSupported;
#else // TARGET_UNIX
    return false;
#endif // TARGET_UNIX
}

// Reset the write tracking state for the specified virtual memory range.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
void GCToOSInterface::ResetWriteWatch(void* address, size_t size)
{
    LIMITED_METHOD_CONTRACT;

#ifndef TARGET_UNIX
    ::ResetWriteWatch(address, size);
#endif // TARGET_UNIX
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
    LIMITED_METHOD_CONTRACT;

#ifndef TARGET_UNIX
    uint32_t flags = resetState ? 1 : 0;
    ULONG granularity;

    bool success = ::GetWriteWatch(flags, address, size, pageAddresses, (ULONG_PTR*)pageAddressesCount, &granularity) == 0;
    _ASSERTE (granularity == GetOsPageSize());

    return success;
#else // TARGET_UNIX
    *pageAddresses = NULL;
    *pageAddressesCount = 0;

    return true;
#endif // TARGET_UNIX
}

#ifdef TARGET_WINDOWS

// This function checks to see if GetLogicalProcessorInformation API is supported.
// On success, this function allocates a SLPI array, sets nEntries to number
// of elements in the SLPI array and returns a pointer to the SLPI array after filling it with information.
//
// Note: If successful, IsGLPISupported allocates memory for the SLPI array and expects the caller to
// free the memory once the caller is done using the information in the SLPI array.
//
// If the API is not supported or any failure, returns NULL
//
SYSTEM_LOGICAL_PROCESSOR_INFORMATION *IsGLPISupported( PDWORD nEntries )
{
    DWORD cbslpi = 0;
    DWORD dwNumElements = 0;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION *pslpi = NULL;

    // We setup the first call to GetLogicalProcessorInformation to fail so that we can obtain
    // the size of the buffer required to allocate for the SLPI array that is returned

    if (!GetLogicalProcessorInformation(pslpi, &cbslpi) &&
    GetLastError() != ERROR_INSUFFICIENT_BUFFER)
    {
        // If we fail with anything other than an ERROR_INSUFFICIENT_BUFFER here, we punt with failure.
        return NULL;
    }

    _ASSERTE(cbslpi);

    // compute the number of SLPI entries required to hold the information returned from GLPI

    dwNumElements = cbslpi / sizeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION);

    // allocate a buffer in the free heap to hold an array of SLPI entries from GLPI, number of elements in the array is dwNumElements

    pslpi = new (nothrow) SYSTEM_LOGICAL_PROCESSOR_INFORMATION[ dwNumElements ];

    if(pslpi == NULL)
    {
        // the memory allocation failed
        return NULL;
    }

    // Make call to GetLogicalProcessorInformation. Returns array of SLPI structures

    if (!GetLogicalProcessorInformation(pslpi, &cbslpi))
    {
        // GetLogicalProcessorInformation failed
        delete[] pslpi ; //Allocation was fine but the API call itself failed and so we are releasing the memory before the return NULL.
        return NULL ;
    }

    // GetLogicalProcessorInformation successful, set nEntries to number of entries in the SLPI array
    *nEntries  = dwNumElements;

    return pslpi;    // return pointer to SLPI array
}

// This function returns the size of highest level cache on the physical chip.   If it cannot
// determine the cachesize this function returns 0.
size_t GetLogicalProcessorCacheSizeFromOS()
{
    size_t cache_size = 0;
    DWORD nEntries = 0;

    // Try to use GetLogicalProcessorInformation API and get a valid pointer to the SLPI array if successful.  Returns NULL
    // if API not present or on failure.

    SYSTEM_LOGICAL_PROCESSOR_INFORMATION *pslpi = IsGLPISupported(&nEntries) ;

    if (pslpi == NULL)
    {
        // GetLogicalProcessorInformation not supported or failed.
        goto Exit;
    }

    // Crack the information. Iterate through all the SLPI array entries for all processors in system.
    // Will return the greatest of all the processor cache sizes or zero
    {
        size_t last_cache_size = 0;

        for (DWORD i=0; i < nEntries; i++)
        {
            if (pslpi[i].Relationship == RelationCache)
            {
                last_cache_size = max(last_cache_size, pslpi[i].Cache.Size);
            }
        }
        cache_size = last_cache_size;
    }

Exit:
    if(pslpi)
        delete[] pslpi;  // release the memory allocated for the SLPI array.

    return cache_size;
}

#endif // TARGET_WINDOWS

// Get size of the largest cache on the processor die
// Parameters:
//  trueSize - true to return true cache size, false to return scaled up size based on
//             the processor architecture
// Return:
//  Size of the cache
size_t GCToOSInterface::GetCacheSizePerLogicalCpu(bool trueSize)
{
    LIMITED_METHOD_CONTRACT;

    static volatile size_t s_maxSize;
    static volatile size_t s_maxTrueSize;

    size_t size = trueSize ? s_maxTrueSize : s_maxSize;
    if (size != 0)
        return size;

    size_t maxSize, maxTrueSize;

    maxSize = maxTrueSize = GetLogicalProcessorCacheSizeFromOS() ; // Returns the size of the highest level processor cache

#if defined(TARGET_ARM64)
    // Bigger gen0 size helps arm64 targets
    maxSize = maxTrueSize * 3;
#endif

    s_maxSize = maxSize;
    s_maxTrueSize = maxTrueSize;

    //    printf("GetCacheSizePerLogicalCpu returns %d, adjusted size %d\n", maxSize, maxTrueSize);
    return trueSize ? maxTrueSize : maxSize;
}

// Sets the calling thread's affinity to only run on the processor specified
// Parameters:
//  procNo - The requested processor for the calling thread.
// Return:
//  true if setting the affinity was successful, false otherwise.
bool GCToOSInterface::SetThreadAffinity(uint16_t procNo)
{
    LIMITED_METHOD_CONTRACT;
#ifndef TARGET_UNIX
    GroupProcNo groupProcNo(procNo);

    if (CPUGroupInfo::CanEnableGCCPUGroups())
    {
        GROUP_AFFINITY ga;
        ga.Group = (WORD)groupProcNo.GetGroup();
        ga.Reserved[0] = 0; // reserve must be filled with zero
        ga.Reserved[1] = 0; // otherwise call may fail
        ga.Reserved[2] = 0;
        ga.Mask = (size_t)1 << groupProcNo.GetProcIndex();
        return !!SetThreadGroupAffinity(GetCurrentThread(), &ga, nullptr);
    }
    else
    {
        return !!SetThreadAffinityMask(GetCurrentThread(), (DWORD_PTR)1 << groupProcNo.GetProcIndex());
    }
#else //  TARGET_UNIX
    return PAL_SetCurrentThreadAffinity(procNo);
#endif //  TARGET_UNIX
}

// Boosts the calling thread's thread priority to a level higher than the default
// for new threads.
// Parameters:
//  None.
// Return:
//  true if the priority boost was successful, false otherwise.
bool GCToOSInterface::BoostThreadPriority()
{
    return !!SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_HIGHEST);
}

// Set the set of processors enabled for GC threads for the current process based on config specified affinity mask and set
// Parameters:
//  configAffinityMask - mask specified by the GCHeapAffinitizeMask config
//  configAffinitySet  - affinity set specified by the GCHeapAffinitizeRanges config
// Return:
//  set of enabled processors
const AffinitySet* GCToOSInterface::SetGCThreadsAffinitySet(uintptr_t configAffinityMask, const AffinitySet* configAffinitySet)
{
#ifndef TARGET_UNIX
    if (CPUGroupInfo::CanEnableGCCPUGroups())
#endif // !TARGET_UNIX
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
    }
#ifndef TARGET_UNIX
    else
    {
        if (configAffinityMask != 0)
        {
            // Update the process affinity set using the configured mask
            for (size_t i = 0; i < 8 * sizeof(uintptr_t); i++)
            {
                if (g_processAffinitySet.Contains(i) && ((configAffinityMask & ((uintptr_t)1 << i)) == 0))
                {
                    g_processAffinitySet.Remove(i);
                }
            }
        }
    }
#endif // !TARGET_UNIX

    return &g_processAffinitySet;
}

// Return the size of the user-mode portion of the virtual address space of this process.
// Return:
//  non zero if it has succeeded, (size_t)-1 if not available
size_t GCToOSInterface::GetVirtualMemoryLimit()
{
    LIMITED_METHOD_CONTRACT;

    MEMORYSTATUSEX memStatus;
    GetProcessMemoryLoad(&memStatus);

    return (size_t)memStatus.ullTotalVirtual;
}

static size_t g_RestrictedPhysicalMemoryLimit = (size_t)MAX_PTR;

#ifndef TARGET_UNIX

static size_t GetRestrictedPhysicalMemoryLimit()
{
    LIMITED_METHOD_CONTRACT;

    // The limit was cached already
    if (g_RestrictedPhysicalMemoryLimit != (size_t)MAX_PTR)
        return g_RestrictedPhysicalMemoryLimit;

    size_t job_physical_memory_limit = (size_t)MAX_PTR;
    uint64_t total_virtual = 0;
    uint64_t total_physical = 0;
    BOOL in_job_p = FALSE;

    if (!IsProcessInJob(GetCurrentProcess(), NULL, &in_job_p))
        goto exit;

    if (in_job_p)
    {
        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limit_info;
        if (QueryInformationJobObject(NULL, JobObjectExtendedLimitInformation, &limit_info,
            sizeof(limit_info), NULL))
        {
            size_t job_memory_limit = (size_t)MAX_PTR;
            size_t job_process_memory_limit = (size_t)MAX_PTR;
            size_t job_workingset_limit = (size_t)MAX_PTR;

            // Notes on the NT job object:
            //
            // You can specific a bigger process commit or working set limit than
            // job limit which is pointless so we use the smallest of all 3 as
            // to calculate our "physical memory load" or "available physical memory"
            // when running inside a job object, ie, we treat this as the amount of physical memory
            // our process is allowed to use.
            //
            // The commit limit is already reflected by default when you run in a
            // job but the physical memory load is not.
            //
            if ((limit_info.BasicLimitInformation.LimitFlags & JOB_OBJECT_LIMIT_JOB_MEMORY) != 0)
                job_memory_limit = limit_info.JobMemoryLimit;
            if ((limit_info.BasicLimitInformation.LimitFlags & JOB_OBJECT_LIMIT_PROCESS_MEMORY) != 0)
                job_process_memory_limit = limit_info.ProcessMemoryLimit;
            if ((limit_info.BasicLimitInformation.LimitFlags & JOB_OBJECT_LIMIT_WORKINGSET) != 0)
                job_workingset_limit = limit_info.BasicLimitInformation.MaximumWorkingSetSize;

            if ((job_memory_limit != (size_t)MAX_PTR) ||
                (job_process_memory_limit != (size_t)MAX_PTR) ||
                (job_workingset_limit != (size_t)MAX_PTR))
            {
                job_physical_memory_limit = min (job_memory_limit, job_process_memory_limit);
                job_physical_memory_limit = min (job_physical_memory_limit, job_workingset_limit);

                MEMORYSTATUSEX ms;
                GetProcessMemoryLoad(&ms);
                total_virtual = ms.ullTotalVirtual;
                total_physical = ms.ullAvailPhys;

                // A sanity check in case someone set a larger limit than there is actual physical memory.
                job_physical_memory_limit = (size_t) min (job_physical_memory_limit, ms.ullTotalPhys);
            }
        }
    }

exit:
    if (job_physical_memory_limit == (size_t)MAX_PTR)
    {
        job_physical_memory_limit = 0;
    }

    // Check to see if we are limited by VM.
    if (total_virtual == 0)
    {
        MEMORYSTATUSEX ms;
        GetProcessMemoryLoad(&ms);

        total_virtual = ms.ullTotalVirtual;
        total_physical = ms.ullTotalPhys;
    }

    if (job_physical_memory_limit != 0)
    {
        total_physical = job_physical_memory_limit;
    }

    if (total_virtual < total_physical)
    {
        // Limited by virtual address space
        job_physical_memory_limit = 0;
    }

    VolatileStore(&g_RestrictedPhysicalMemoryLimit, job_physical_memory_limit);
    return g_RestrictedPhysicalMemoryLimit;
}

#else

static size_t GetRestrictedPhysicalMemoryLimit()
{
    LIMITED_METHOD_CONTRACT;

    // The limit was cached already
    if (g_RestrictedPhysicalMemoryLimit != (size_t)MAX_PTR)
        return g_RestrictedPhysicalMemoryLimit;

    size_t memory_limit = PAL_GetRestrictedPhysicalMemoryLimit();

    VolatileStore(&g_RestrictedPhysicalMemoryLimit, memory_limit);
    return g_RestrictedPhysicalMemoryLimit;
}
#endif // TARGET_UNIX

// Get the physical memory that this process can use.
// Return:
//  non zero if it has succeeded, 0 if it has failed
uint64_t GCToOSInterface::GetPhysicalMemoryLimit(bool* is_restricted)
{
    LIMITED_METHOD_CONTRACT;

    if (is_restricted)
        *is_restricted = false;

    size_t restricted_limit = GetRestrictedPhysicalMemoryLimit();
    if (restricted_limit != 0)
    {
        if (is_restricted)
            *is_restricted = true;

        return restricted_limit;
    }

    MEMORYSTATUSEX memStatus;
    GetProcessMemoryLoad(&memStatus);

#ifndef TARGET_UNIX
    // For 32-bit processes the virtual address range could be smaller than the amount of physical
    // memory on the machine/in the container, we need to restrict by the VM.
    if (memStatus.ullTotalVirtual < memStatus.ullTotalPhys)
        return memStatus.ullTotalVirtual;
#endif

    return memStatus.ullTotalPhys;
}

// Get memory status
// Parameters:
//  memory_load - A number between 0 and 100 that specifies the approximate percentage of physical memory
//      that is in use (0 indicates no memory use and 100 indicates full memory use).
//  available_physical - The amount of physical memory currently available, in bytes.
//  available_page_file - The maximum amount of memory the current process can commit, in bytes.
// Remarks:
//  Any parameter can be null.
void GCToOSInterface::GetMemoryStatus(uint64_t restricted_limit, uint32_t* memory_load, uint64_t* available_physical, uint64_t* available_page_file)
{
    LIMITED_METHOD_CONTRACT;

    if (restricted_limit != 0)
    {
        size_t workingSetSize;
        BOOL status = FALSE;
#ifndef TARGET_UNIX
        PROCESS_MEMORY_COUNTERS pmc;
        status = GetProcessMemoryInfo(GetCurrentProcess(), &pmc, sizeof(pmc));
        workingSetSize = pmc.WorkingSetSize;
#else
        status = PAL_GetPhysicalMemoryUsed(&workingSetSize);
#endif
        if (status)
        {
            if (memory_load)
                *memory_load = (uint32_t)((float)workingSetSize * 100.0 / (float)restricted_limit);
            if (available_physical)
            {
                if(workingSetSize > restricted_limit)
                    *available_physical = 0;
                else
                    *available_physical = restricted_limit - workingSetSize;
            }
            // Available page file doesn't mean much when physical memory is restricted since
            // we don't know how much of it is available to this process so we are not going to
            // bother to make another OS call for it.
            if (available_page_file)
                *available_page_file = 0;

            return;
        }
    }

    MEMORYSTATUSEX ms;
    GetProcessMemoryLoad(&ms);

#ifndef TARGET_UNIX
    // For 32-bit processes the virtual address range could be smaller than the amount of physical
    // memory on the machine/in the container, we need to restrict by the VM.
    if (ms.ullTotalVirtual < ms.ullTotalPhys)
    {
        if (memory_load != NULL)
            *memory_load = (uint32_t)((float)(ms.ullTotalVirtual - ms.ullAvailVirtual) * 100.0 / (float)ms.ullTotalVirtual);
        if (available_physical != NULL)
            *available_physical = ms.ullTotalVirtual;

        // Available page file isn't helpful when we are restricted by virtual memory
        // since the amount of memory we can reserve is less than the amount of
        // memory we can commit.
        if (available_page_file != NULL)
            *available_page_file = 0;
    }
    else
#endif //!TARGET_UNIX
    {
        if (memory_load != NULL)
            *memory_load = ms.dwMemoryLoad;
        if (available_physical != NULL)
            *available_physical = ms.ullAvailPhys;
        if (available_page_file != NULL)
            *available_page_file = ms.ullAvailPageFile;
    }
}

// Get a high precision performance counter
// Return:
//  The counter value
int64_t GCToOSInterface::QueryPerformanceCounter()
{
    LIMITED_METHOD_CONTRACT;

    LARGE_INTEGER ts;
    if (!::QueryPerformanceCounter(&ts))
    {
        DebugBreak();
        _ASSERTE(!"Fatal Error - cannot query performance counter.");
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);        // TODO: fatal error
    }

    return ts.QuadPart;
}

// Get a frequency of the high precision performance counter
// Return:
//  The counter frequency
int64_t GCToOSInterface::QueryPerformanceFrequency()
{
    LIMITED_METHOD_CONTRACT;

    LARGE_INTEGER frequency;
    if (!::QueryPerformanceFrequency(&frequency))
    {
        DebugBreak();
        _ASSERTE(!"Fatal Error - cannot query performance counter.");
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);        // TODO: fatal error
    }

    return frequency.QuadPart;
}

// Get a time stamp with a low precision
// Return:
//  Time stamp in milliseconds
uint32_t GCToOSInterface::GetLowPrecisionTimeStamp()
{
    LIMITED_METHOD_CONTRACT;

    return ::GetTickCount();
}

uint32_t GCToOSInterface::GetTotalProcessorCount()
{
    LIMITED_METHOD_CONTRACT;

    return ::GetTotalProcessorCount();
}

bool GCToOSInterface::CanEnableGCNumaAware()
{
    LIMITED_METHOD_CONTRACT;

    return NumaNodeInfo::CanEnableGCNumaAware() != FALSE;
}

bool GCToOSInterface::GetNumaInfo(uint16_t* total_nodes, uint32_t* max_procs_per_node)
{
#ifndef TARGET_UNIX
    return NumaNodeInfo::GetNumaInfo(total_nodes, (DWORD*)max_procs_per_node);
#else
    return false;
#endif //!TARGET_UNIX
}

bool GCToOSInterface::GetCPUGroupInfo(uint16_t* total_groups, uint32_t* max_procs_per_group)
{
#ifndef TARGET_UNIX
    return CPUGroupInfo::GetCPUGroupInfo(total_groups, (DWORD*)max_procs_per_group);
#else
    return false;
#endif //!TARGET_UNIX
}

bool GCToOSInterface::CanEnableGCCPUGroups()
{
    LIMITED_METHOD_CONTRACT;
#ifndef TARGET_UNIX
    return CPUGroupInfo::CanEnableGCCPUGroups() != FALSE;
#else
    return false;
#endif //!TARGET_UNIX
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

    // Locate heap_number-th available processor
    uint16_t procIndex;
    size_t cnt = heap_number;
    for (uint16_t i = 0; i < MAX_SUPPORTED_CPUS; i++)
    {
        if (g_processAffinitySet.Contains(i))
        {
            if (cnt == 0)
            {
                procIndex = i;
                success = true;
                break;
            }

            cnt--;
        }
    }

    if (success)
    {
#ifndef TARGET_UNIX
        WORD gn, gpn;

        if (CPUGroupInfo::CanEnableGCCPUGroups())
        {
            CPUGroupInfo::GetGroupForProcessor(procIndex, &gn, &gpn);
        }
        else
        {
            gn = GroupProcNo::NoGroup;
            gpn = procIndex;
        }

        GroupProcNo groupProcNo(gn, gpn);
        *proc_no = groupProcNo.GetCombinedValue();

        PROCESSOR_NUMBER procNumber;

        if (CPUGroupInfo::CanEnableGCCPUGroups())
        {
            procNumber.Group = gn;
        }
        else
        {
            // Get the current processor group
            GetCurrentProcessorNumberEx(&procNumber);
        }

        if (GCToOSInterface::CanEnableGCNumaAware())
        {
            procNumber.Number   = (BYTE)gpn;
            procNumber.Reserved = 0;

            if (!NumaNodeInfo::GetNumaProcessorNodeEx(&procNumber, node_no))
            {
                *node_no = NUMA_NODE_UNDEFINED;
            }
        }
        else
        {   // no numa setting, each cpu group is treated as a node
            *node_no = procNumber.Group;
        }
#else // !TARGET_UNIX
        *proc_no = procIndex;
        if (!GCToOSInterface::CanEnableGCNumaAware() || !NumaNodeInfo::GetNumaProcessorNodeEx(procIndex, (WORD*)node_no))
        {
            *node_no = NUMA_NODE_UNDEFINED;
        }
#endif // !TARGET_UNIX
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
    size_t index_offset = 0;

    char* number_end;
#ifndef TARGET_UNIX
    size_t group_number = strtoul(*config_string, &number_end, 10);

    if ((number_end == *config_string) || (*number_end != ':'))
    {
        // No number or no colon after the number found, invalid format
        return false;
    }

    WORD group_begin;
    WORD group_size;
    if (!CPUGroupInfo::GetCPUGroupRange((WORD)group_number, &group_begin, &group_size))
    {
        // group number out of range
        return false;
    }

    index_offset = group_begin;
    *config_string = number_end + 1;
#endif // !TARGET_UNIX

    size_t start, end;
    if (!ParseIndexOrRange(config_string, &start, &end))
    {
        return false;
    }

#ifndef TARGET_UNIX
    if ((start >= group_size) || (end >= group_size))
    {
        // Invalid CPU index values or range
        return false;
    }
#endif // !TARGET_UNIX

    *start_index = index_offset + start;
    *end_index = index_offset + end;

    return true;
}

// Initialize the critical section
bool CLRCriticalSection::Initialize()
{
    WRAPPER_NO_CONTRACT;
    InitializeCriticalSection(&m_cs);

    return true;
}

// Destroy the critical section
void CLRCriticalSection::Destroy()
{
    WRAPPER_NO_CONTRACT;
    DeleteCriticalSection(&m_cs);
}

// Enter the critical section. Blocks until the section can be entered.
void CLRCriticalSection::Enter()
{
    WRAPPER_NO_CONTRACT;
    EnterCriticalSection(&m_cs);
}

// Leave the critical section
void CLRCriticalSection::Leave()
{
    WRAPPER_NO_CONTRACT;
    LeaveCriticalSection(&m_cs);
}

// An implementatino of GCEvent that delegates to
// a CLREvent, which in turn delegates to the PAL. This event
// is also host-aware.
class GCEvent::Impl
{
private:
    CLREvent m_event;

public:
    Impl() = default;

    bool IsValid()
    {
        WRAPPER_NO_CONTRACT;

        return !!m_event.IsValid();
    }

    void CloseEvent()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(m_event.IsValid());
        m_event.CloseEvent();
    }

    void Set()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(m_event.IsValid());
        m_event.Set();
    }

    void Reset()
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(m_event.IsValid());
        m_event.Reset();
    }

    uint32_t Wait(uint32_t timeout, bool alertable)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(m_event.IsValid());
        return m_event.Wait(timeout, alertable);
    }

    bool CreateAutoEvent(bool initialState)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        return !!m_event.CreateAutoEventNoThrow(initialState);
    }

    bool CreateManualEvent(bool initialState)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        return !!m_event.CreateManualEventNoThrow(initialState);
    }

    bool CreateOSAutoEvent(bool initialState)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        return !!m_event.CreateOSAutoEventNoThrow(initialState);
    }

    bool CreateOSManualEvent(bool initialState)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
        } CONTRACTL_END;

        return !!m_event.CreateOSManualEventNoThrow(initialState);
    }
};

GCEvent::GCEvent()
  : m_impl(nullptr)
{
}

void GCEvent::CloseEvent()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_impl != nullptr);
    m_impl->CloseEvent();
}

void GCEvent::Set()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_impl != nullptr);
    m_impl->Set();
}

void GCEvent::Reset()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_impl != nullptr);
    m_impl->Reset();
}

uint32_t GCEvent::Wait(uint32_t timeout, bool alertable)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_impl != nullptr);
    return m_impl->Wait(timeout, alertable);
}

bool GCEvent::CreateManualEventNoThrow(bool initialState)
{
    CONTRACTL {
      NOTHROW;
      GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(m_impl == nullptr);
    NewHolder<GCEvent::Impl> event = new (nothrow) GCEvent::Impl();
    if (!event)
    {
        return false;
    }

    event->CreateManualEvent(initialState);
    m_impl = event.Extract();
    return true;
}

bool GCEvent::CreateAutoEventNoThrow(bool initialState)
{
    CONTRACTL {
      NOTHROW;
      GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(m_impl == nullptr);
    NewHolder<GCEvent::Impl> event = new (nothrow) GCEvent::Impl();
    if (!event)
    {
        return false;
    }

    event->CreateAutoEvent(initialState);
    m_impl = event.Extract();
    return IsValid();
}

bool GCEvent::CreateOSAutoEventNoThrow(bool initialState)
{
    CONTRACTL {
      NOTHROW;
      GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(m_impl == nullptr);
    NewHolder<GCEvent::Impl> event = new (nothrow) GCEvent::Impl();
    if (!event)
    {
        return false;
    }

    event->CreateOSAutoEvent(initialState);
    m_impl = event.Extract();
    return IsValid();
}

bool GCEvent::CreateOSManualEventNoThrow(bool initialState)
{
    CONTRACTL {
      NOTHROW;
      GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(m_impl == nullptr);
    NewHolder<GCEvent::Impl> event = new (nothrow) GCEvent::Impl();
    if (!event)
    {
        return false;
    }

    event->CreateOSManualEvent(initialState);
    m_impl = event.Extract();
    return IsValid();
}

