// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cstdint>
#include <cassert>
#include <cstddef>
#include <memory>
#include "windows.h"
#include "psapi.h"
#include "env/gcenv.structs.h"
#include "env/gcenv.base.h"
#include "env/gcenv.os.h"
#include "env/gcenv.ee.h"
#include "env/gcenv.windows.inl"
#include "env/volatile.h"
#include "gcconfig.h"

GCSystemInfo g_SystemInfo;

static size_t g_RestrictedPhysicalMemoryLimit = (size_t)UINTPTR_MAX;

static bool g_SeLockMemoryPrivilegeAcquired = false;

static AffinitySet g_processAffinitySet;

namespace {

static bool g_fEnableGCNumaAware;
static uint32_t g_nNodes;

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
        assert(group <= 0x3ff);
        assert(procIndex <= 0x3f);
    }

    uint16_t GetGroup() { return m_groupProc >> 6; }
    uint16_t GetProcIndex() { return m_groupProc & 0x3f; }
    uint16_t GetCombinedValue() { return m_groupProc; }
};

struct CPU_Group_Info
{
    WORD    nr_active;  // at most 64
    WORD    reserved[1];
    WORD    begin;
    WORD    end;
    DWORD_PTR   active_mask;
    DWORD   groupWeight;
    DWORD   activeThreadWeight;
};

static bool g_fEnableGCCPUGroups;
static bool g_fHadSingleProcessorAtStartup;
static DWORD  g_nGroups;
static DWORD g_nProcessors;
static CPU_Group_Info *g_CPUGroupInfoArray;

void InitNumaNodeInfo()
{
    ULONG highest = 0;

    g_fEnableGCNumaAware = false;

    if (!GCConfig::GetGCNumaAware())
        return;

    // fail to get the highest numa node number
    if (!GetNumaHighestNodeNumber(&highest) || (highest == 0))
        return;

    g_nNodes = highest + 1;
    g_fEnableGCNumaAware = true;
    return;
}

#if (defined(TARGET_AMD64) || defined(TARGET_ARM64))
// Calculate greatest common divisor
DWORD GCD(DWORD u, DWORD v)
{
    while (v != 0)
    {
        DWORD dwTemp = v;
        v = u % v;
        u = dwTemp;
    }

    return u;
}

// Calculate least common multiple
DWORD LCM(DWORD u, DWORD v)
{
    return u / GCD(u, v) * v;
}
#endif

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

bool InitCPUGroupInfoArray()
{
#if (defined(TARGET_AMD64) || defined(TARGET_ARM64))
    BYTE *bBuffer = NULL;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *pSLPIEx = NULL;
    SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *pRecord = NULL;
    DWORD cbSLPIEx = 0;
    DWORD byteOffset = 0;
    DWORD dwNumElements = 0;
    DWORD dwWeight = 1;

    if (GetLogicalProcessorInformationEx(RelationGroup, pSLPIEx, &cbSLPIEx) &&
                      GetLastError() != ERROR_INSUFFICIENT_BUFFER)
        return false;

    assert(cbSLPIEx);

    // Fail to allocate buffer
    bBuffer = new (std::nothrow) BYTE[ cbSLPIEx ];
    if (bBuffer == NULL)
        return false;

    pSLPIEx = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *)bBuffer;
    if (!GetLogicalProcessorInformationEx(RelationGroup, pSLPIEx, &cbSLPIEx))
    {
        delete[] bBuffer;
        return false;
    }

    pRecord = pSLPIEx;
    while (byteOffset < cbSLPIEx)
    {
        if (pRecord->Relationship == RelationGroup)
        {
            g_nGroups = pRecord->Group.ActiveGroupCount;
            break;
        }
        byteOffset += pRecord->Size;
        pRecord = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX *)(bBuffer + byteOffset);
    }

    g_CPUGroupInfoArray = new (std::nothrow) CPU_Group_Info[g_nGroups];
    if (g_CPUGroupInfoArray == NULL)
    {
        delete[] bBuffer;
        return false;
    }

    for (DWORD i = 0; i < g_nGroups; i++)
    {
        g_CPUGroupInfoArray[i].nr_active   = (WORD)pRecord->Group.GroupInfo[i].ActiveProcessorCount;
        g_CPUGroupInfoArray[i].active_mask = pRecord->Group.GroupInfo[i].ActiveProcessorMask;
        g_nProcessors += g_CPUGroupInfoArray[i].nr_active;
        dwWeight = LCM(dwWeight, (DWORD)g_CPUGroupInfoArray[i].nr_active);
    }

    // The number of threads per group that can be supported will depend on the number of CPU groups
    // and the number of LPs within each processor group. For example, when the number of LPs in
    // CPU groups is the same and is 64, the number of threads per group before weight overflow
    // would be 2^32/2^6 = 2^26 (64M threads)
    for (DWORD i = 0; i < g_nGroups; i++)
    {
        g_CPUGroupInfoArray[i].groupWeight = dwWeight / (DWORD)g_CPUGroupInfoArray[i].nr_active;
        g_CPUGroupInfoArray[i].activeThreadWeight = 0;
    }

    delete[] bBuffer;  // done with it; free it
    return true;
#else
    return false;
#endif
}

bool InitCPUGroupInfoRange()
{
#if (defined(TARGET_AMD64) || defined(TARGET_ARM64))
    WORD begin   = 0;
    WORD nr_proc = 0;

    for (WORD i = 0; i < g_nGroups; i++)
    {
        nr_proc += g_CPUGroupInfoArray[i].nr_active;
        g_CPUGroupInfoArray[i].begin = begin;
        g_CPUGroupInfoArray[i].end   = nr_proc - 1;
        begin = nr_proc;
    }

    return true;
#else
    return false;
#endif
}

void InitCPUGroupInfo()
{
    g_fEnableGCCPUGroups = false;

#if (defined(TARGET_AMD64) || defined(TARGET_ARM64))
    if (!GCConfig::GetGCCpuGroup())
        return;

    if (!InitCPUGroupInfoArray())
        return;

    if (!InitCPUGroupInfoRange())
        return;

    // only enable CPU groups if more than one group exists
    g_fEnableGCCPUGroups = g_nGroups > 1;
#endif // TARGET_AMD64 || TARGET_ARM64

    // Determine if the process is affinitized to a single processor (or if the system has a single processor)
    DWORD_PTR processAffinityMask, systemAffinityMask;
    if (::GetProcessAffinityMask(::GetCurrentProcess(), &processAffinityMask, &systemAffinityMask))
    {
        processAffinityMask &= systemAffinityMask;
        if (processAffinityMask != 0 && // only one CPU group is involved
            (processAffinityMask & (processAffinityMask - 1)) == 0) // only one bit is set
        {
            g_fHadSingleProcessorAtStartup = true;
        }
    }
}

void GetProcessMemoryLoad(LPMEMORYSTATUSEX pMSEX)
{
    pMSEX->dwLength = sizeof(MEMORYSTATUSEX);
    BOOL fRet = ::GlobalMemoryStatusEx(pMSEX);
    assert(fRet);
}

static size_t GetRestrictedPhysicalMemoryLimit()
{
    LIMITED_METHOD_CONTRACT;

    // The limit was cached already
    if (g_RestrictedPhysicalMemoryLimit != (size_t)UINTPTR_MAX)
        return g_RestrictedPhysicalMemoryLimit;

    size_t job_physical_memory_limit = (size_t)UINTPTR_MAX;
    uint64_t total_virtual = 0;
    uint64_t total_physical = 0;
    BOOL in_job_p = FALSE;

    if (!IsProcessInJob(GetCurrentProcess(), NULL, &in_job_p))
        goto exit;

    if (in_job_p)
    {
        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limit_info;
        if (QueryInformationJobObject (NULL, JobObjectExtendedLimitInformation, &limit_info,
            sizeof(limit_info), NULL))
        {
            size_t job_memory_limit = (size_t)UINTPTR_MAX;
            size_t job_process_memory_limit = (size_t)UINTPTR_MAX;
            size_t job_workingset_limit = (size_t)UINTPTR_MAX;

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

            if ((job_memory_limit != (size_t)UINTPTR_MAX) ||
                (job_process_memory_limit != (size_t)UINTPTR_MAX) ||
                (job_workingset_limit != (size_t)UINTPTR_MAX))
            {
                job_physical_memory_limit = min (job_memory_limit, job_process_memory_limit);
                job_physical_memory_limit = min (job_physical_memory_limit, job_workingset_limit);

                MEMORYSTATUSEX ms;
                ::GetProcessMemoryLoad(&ms);
                total_virtual = ms.ullTotalVirtual;
                total_physical = ms.ullAvailPhys;

                // A sanity check in case someone set a larger limit than there is actual physical memory.
                job_physical_memory_limit = (size_t) min (job_physical_memory_limit, ms.ullTotalPhys);
            }
        }
    }

exit:
    if (job_physical_memory_limit == (size_t)UINTPTR_MAX)
    {
        job_physical_memory_limit = 0;
    }

    // Check to see if we are limited by VM.
    if (total_virtual == 0)
    {
        MEMORYSTATUSEX ms;
        ::GetProcessMemoryLoad(&ms);

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

// This function checks to see if GetLogicalProcessorInformation API is supported.
// On success, this function allocates a SLPI array, sets nEntries to number
// of elements in the SLPI array and returns a pointer to the SLPI array after filling it with information.
//
// Note: If successful, GetLPI allocates memory for the SLPI array and expects the caller to
// free the memory once the caller is done using the information in the SLPI array.
SYSTEM_LOGICAL_PROCESSOR_INFORMATION *GetLPI(PDWORD nEntries)
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

    pslpi = new (std::nothrow) SYSTEM_LOGICAL_PROCESSOR_INFORMATION[ dwNumElements ];

    if (pslpi == NULL)
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

}//GetLPI

// This function returns the size of highest level cache on the physical chip.   If it cannot
// determine the cachesize this function returns 0.
size_t GetLogicalProcessorCacheSizeFromOS()
{
    size_t cache_size = 0;
    DWORD nEntries = 0;

    // Try to use GetLogicalProcessorInformation API and get a valid pointer to the SLPI array if successful.  Returns NULL
    // if API not present or on failure.

    SYSTEM_LOGICAL_PROCESSOR_INFORMATION *pslpi = GetLPI(&nEntries) ;

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

bool CanEnableGCCPUGroups()
{
    return g_fEnableGCCPUGroups;
}

// Get the CPU group for the specified processor
void GetGroupForProcessor(uint16_t processor_number, uint16_t* group_number, uint16_t* group_processor_number)
{
    assert(g_fEnableGCCPUGroups);

#if !defined(FEATURE_REDHAWK) && (defined(TARGET_AMD64) || defined(TARGET_ARM64))
    WORD bTemp = 0;
    WORD bDiff = processor_number - bTemp;

    for (WORD i=0; i < g_nGroups; i++)
    {
        bTemp += g_CPUGroupInfoArray[i].nr_active;
        if (bTemp > processor_number)
        {
            *group_number = i;
            *group_processor_number = bDiff;
            break;
        }
        bDiff = processor_number - bTemp;
    }
#else
    *group_number = 0;
    *group_processor_number = 0;
#endif
}

} // anonymous namespace

// Initialize the interface implementation
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::Initialize()
{
    SYSTEM_INFO systemInfo;
    GetSystemInfo(&systemInfo);

    g_SystemInfo.dwNumberOfProcessors = systemInfo.dwNumberOfProcessors;
    g_SystemInfo.dwPageSize = systemInfo.dwPageSize;
    g_SystemInfo.dwAllocationGranularity = systemInfo.dwAllocationGranularity;

    assert(systemInfo.dwPageSize == 0x1000);

    InitNumaNodeInfo();
    InitCPUGroupInfo();

    if (CanEnableGCCPUGroups())
    {
        // When CPU groups are enabled, then the process is not bound by the process affinity set at process launch.
        // Set the initial affinity mask so that all processors are enabled.
        for (size_t i = 0; i < g_nProcessors; i++)
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

    return true;
}

// Shutdown the interface implementation
void GCToOSInterface::Shutdown()
{
    // nothing to do.
}

// Get numeric id of the current thread if possible on the
// current platform. It is indended for logging purposes only.
// Return:
//  Numeric id of the current thread or 0 if the
uint64_t GCToOSInterface::GetCurrentThreadIdForLogging()
{
    return ::GetCurrentThreadId();
}

// Get id of the process
uint32_t GCToOSInterface::GetCurrentProcessId()
{
    return ::GetCurrentThreadId();
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

    GroupProcNo srcGroupProcNo(srcProcNo);
    GroupProcNo dstGroupProcNo(dstProcNo);

    PROCESSOR_NUMBER proc;

    if (CanEnableGCCPUGroups())
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

        success = !!SetThreadIdealProcessorEx(GetCurrentThread(), &proc, NULL);
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
}

bool GCToOSInterface::GetCurrentThreadIdealProc(uint16_t* procNo)
{
    PROCESSOR_NUMBER proc;

    bool success = GetThreadIdealProcessorEx (GetCurrentThread (), &proc);

    if (success)
    {
        GroupProcNo groupProcNo(proc.Group, proc.Number);
        *procNo = groupProcNo.GetCombinedValue();
    }

    return success;
}

// Get the number of the current processor
uint32_t GCToOSInterface::GetCurrentProcessorNumber()
{
    assert(GCToOSInterface::CanGetCurrentProcessorNumber());

    PROCESSOR_NUMBER proc_no_cpu_group;
    GetCurrentProcessorNumberEx(&proc_no_cpu_group);

    GroupProcNo groupProcNo(proc_no_cpu_group.Group, proc_no_cpu_group.Number);
    return groupProcNo.GetCombinedValue();
}

// Check if the OS supports getting current processor number
bool GCToOSInterface::CanGetCurrentProcessorNumber()
{
    // on all Windows platforms we support this API exists
    return true;
}

// Flush write buffers of processors that are executing threads of the current process
void GCToOSInterface::FlushProcessWriteBuffers()
{
    ::FlushProcessWriteBuffers();
}

// Break into a debugger
void GCToOSInterface::DebugBreak()
{
    ::DebugBreak();
}

// Causes the calling thread to sleep for the specified number of milliseconds
// Parameters:
//  sleepMSec   - time to sleep before switching to another thread
void GCToOSInterface::Sleep(uint32_t sleepMSec)
{
    // TODO(segilles) CLR implementation of __SwitchToThread spins for short sleep durations
    // to avoid context switches - is that interesting or useful here?
    if (sleepMSec > 0)
    {
        ::SleepEx(sleepMSec, FALSE);
    }
}

// Causes the calling thread to yield execution to another thread that is ready to run on the current processor.
// Parameters:
//  switchCount - number of times the YieldThread was called in a loop
void GCToOSInterface::YieldThread(uint32_t switchCount)
{
    UNREFERENCED_PARAMETER(switchCount);
    SwitchToThread();
}

// Reserve virtual memory range.
// Parameters:
//  size      - size of the virtual memory range
//  alignment - requested memory alignment, 0 means no specific alignment requested
//  flags     - flags to control special settings like write watching
//  node      - the NUMA node to reserve memory on
// Return:
//  Starting virtual address of the reserved range
void* GCToOSInterface::VirtualReserve(size_t size, size_t alignment, uint32_t flags, uint16_t node)
{
    // Windows already ensures 64kb alignment on VirtualAlloc. The current CLR
    // implementation ignores it on Windows, other than making some sanity checks on it.
    UNREFERENCED_PARAMETER(alignment);
    assert((alignment & (alignment - 1)) == 0);
    assert(alignment <= 0x10000);

    DWORD memFlags = (flags & VirtualReserveFlags::WriteWatch) ? (MEM_RESERVE | MEM_WRITE_WATCH) : MEM_RESERVE;
    if (node == NUMA_NODE_UNDEFINED)
    {
        return ::VirtualAlloc (nullptr, size, memFlags, PAGE_READWRITE);
    }
    else
    {
        return ::VirtualAllocExNuma (::GetCurrentProcess (), NULL, size, memFlags, PAGE_READWRITE, node);
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
    return !!::VirtualFree(address, 0, MEM_RELEASE);
}

// Commit virtual memory range.
// Parameters:
//  size      - size of the virtual memory range
// Return:
//  Starting virtual address of the committed range
void* GCToOSInterface::VirtualReserveAndCommitLargePages(size_t size, uint16_t node)
{
    void* pRetVal = nullptr;

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

    if (node == NUMA_NODE_UNDEFINED)
    {
        return ::VirtualAlloc(nullptr, size, MEM_RESERVE | MEM_COMMIT | MEM_LARGE_PAGES, PAGE_READWRITE);
    }
    else
    {
        return ::VirtualAllocExNuma(::GetCurrentProcess(), NULL, size, MEM_RESERVE | MEM_COMMIT | MEM_LARGE_PAGES, PAGE_READWRITE, node);
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
    if (node == NUMA_NODE_UNDEFINED)
    {
        return ::VirtualAlloc(address, size, MEM_COMMIT, PAGE_READWRITE) != nullptr;
    }
    else
    {
        assert(g_fEnableGCNumaAware);
        return ::VirtualAllocExNuma(::GetCurrentProcess(), address, size, MEM_COMMIT, PAGE_READWRITE, node) != nullptr;
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
    return !!::VirtualFree(address, size, MEM_DECOMMIT);
}

// Reset virtual memory range. Indicates that data in the memory range specified by address and size is no
// longer of interest, but it should not be decommitted.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
//  unlock  - true if the memory range should also be unlocked
// Return:
//  true if it has succeeded, false if it has failed. Returns false also if
//  unlocking was requested but the unlock failed.
bool GCToOSInterface::VirtualReset(void * address, size_t size, bool unlock)
{
    bool success = ::VirtualAlloc(address, size, MEM_RESET, PAGE_READWRITE) != nullptr;
    if (success && unlock)
    {
        ::VirtualUnlock(address, size);
    }

    return success;
}

// Check if the OS supports write watching
bool GCToOSInterface::SupportsWriteWatch()
{
    void* mem = GCToOSInterface::VirtualReserve(g_SystemInfo.dwAllocationGranularity, 0, VirtualReserveFlags::WriteWatch);
    if (mem != nullptr)
    {
        GCToOSInterface::VirtualRelease(mem, g_SystemInfo.dwAllocationGranularity);
        return true;
    }

    return false;
}

// Reset the write tracking state for the specified virtual memory range.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
void GCToOSInterface::ResetWriteWatch(void* address, size_t size)
{
    ::ResetWriteWatch(address, size);
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
    uint32_t flags = resetState ? 1 : 0;
    ULONG granularity;

    bool success = ::GetWriteWatch(flags, address, size, pageAddresses, (ULONG_PTR*)pageAddressesCount, &granularity) == 0;
    if (success)
    {
        assert(granularity == OS_PAGE_SIZE);
    }

    return success;
}

// Get size of the largest cache on the processor die
// Parameters:
//  trueSize - true to return true cache size, false to return scaled up size based on
//             the processor architecture
// Return:
//  Size of the cache
size_t GCToOSInterface::GetCacheSizePerLogicalCpu(bool trueSize)
{
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
    GroupProcNo groupProcNo(procNo);

    if (CanEnableGCCPUGroups())
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
    // When the configAffinitySet is not empty, enforce the cpu groups
    if (CanEnableGCCPUGroups())
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

    return &g_processAffinitySet;
}

// Return the size of the user-mode portion of the virtual address space of this process.
// Return:
//  non zero if it has succeeded, (size_t)-1 if not available
size_t GCToOSInterface::GetVirtualMemoryLimit()
{
    MEMORYSTATUSEX memStatus;
    GetProcessMemoryLoad(&memStatus);
    assert(memStatus.ullAvailVirtual != 0);
    return (size_t)memStatus.ullAvailVirtual;
}

// Get the physical memory that this process can use.
// Return:
//  non zero if it has succeeded, 0 if it has failed
// Remarks:
//  If a process runs with a restricted memory limit, it returns the limit. If there's no limit
//  specified, it returns amount of actual physical memory.
uint64_t GCToOSInterface::GetPhysicalMemoryLimit(bool* is_restricted)
{
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
    assert(memStatus.ullTotalPhys != 0);

    // For 32-bit processes the virtual address range could be smaller than the amount of physical
    // memory on the machine/in the container, we need to restrict by the VM.
    if (memStatus.ullTotalVirtual < memStatus.ullTotalPhys)
        return memStatus.ullTotalVirtual;

    return memStatus.ullTotalPhys;
}

// Get memory status
// Parameters:
//  restricted_limit - The amount of physical memory in bytes that the current process is being restricted to. If non-zero, it used to calculate
//      memory_load and available_physical. If zero, memory_load and available_physical is calculate based on all available memory.
//  memory_load - A number between 0 and 100 that specifies the approximate percentage of physical memory
//      that is in use (0 indicates no memory use and 100 indicates full memory use).
//  available_physical - The amount of physical memory currently available, in bytes.
//  available_page_file - The maximum amount of memory the current process can commit, in bytes.
void GCToOSInterface::GetMemoryStatus(uint64_t restricted_limit, uint32_t* memory_load, uint64_t* available_physical, uint64_t* available_page_file)
{
    if (restricted_limit != 0)
    {
        size_t workingSetSize;
        BOOL status = FALSE;

        PROCESS_MEMORY_COUNTERS pmc;
        status = GetProcessMemoryInfo(GetCurrentProcess(), &pmc, sizeof(pmc));
        workingSetSize = pmc.WorkingSetSize;

        if(status)
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
    ::GetProcessMemoryLoad(&ms);

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
    LARGE_INTEGER ts;
    if (!::QueryPerformanceCounter(&ts))
    {
        assert(false && "Failed to query performance counter");
    }

    return ts.QuadPart;
}

// Get a frequency of the high precision performance counter
// Return:
//  The counter frequency
int64_t GCToOSInterface::QueryPerformanceFrequency()
{
    LARGE_INTEGER ts;
    if (!::QueryPerformanceFrequency(&ts))
    {
        assert(false && "Failed to query performance counter");
    }

    return ts.QuadPart;
}

// Get a time stamp with a low precision
// Return:
//  Time stamp in milliseconds
uint32_t GCToOSInterface::GetLowPrecisionTimeStamp()
{
    return ::GetTickCount();
}

// Gets the total number of processors on the machine, not taking
// into account current process affinity.
// Return:
//  Number of processors on the machine
uint32_t GCToOSInterface::GetTotalProcessorCount()
{
    if (CanEnableGCCPUGroups())
    {
        return g_nProcessors;
    }
    else
    {
        return g_SystemInfo.dwNumberOfProcessors;
    }
}

bool GCToOSInterface::CanEnableGCNumaAware()
{
    return g_fEnableGCNumaAware;
}

bool GCToOSInterface::GetNumaInfo(uint16_t* total_nodes, uint32_t* max_procs_per_node)
{
    if (g_fEnableGCNumaAware)
    {
        DWORD currentProcsOnNode = 0;
        for (uint32_t i = 0; i < g_nNodes; i++)
        {
            GROUP_AFFINITY processorMask;
            if (GetNumaNodeProcessorMaskEx(i, &processorMask))
            {
                DWORD procsOnNode = 0;
                uintptr_t mask = (uintptr_t)processorMask.Mask;
                while (mask)
                {
                    procsOnNode++;
                    mask &= mask - 1;
                }

                currentProcsOnNode = max(currentProcsOnNode, procsOnNode);
            }
            *max_procs_per_node = currentProcsOnNode;
            *total_nodes = g_nNodes;
        }
        return true;
    }

    return false;
}

bool GCToOSInterface::CanEnableGCCPUGroups()
{
    return g_fEnableGCCPUGroups;
}

bool GCToOSInterface::GetCPUGroupInfo(uint16_t* total_groups, uint32_t* max_procs_per_group)
{
    if (g_fEnableGCCPUGroups)
    {
        *total_groups = (uint16_t)g_nGroups;
        DWORD currentProcsInGroup = 0;
        for (WORD i = 0; i < g_nGroups; i++)
        {
            currentProcsInGroup = max(currentProcsInGroup, g_CPUGroupInfoArray[i].nr_active);
        }
        *max_procs_per_group = currentProcsInGroup;
        return true;
    }

    return false;
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
    for (uint16_t i = 0; i < GCToOSInterface::GetTotalProcessorCount(); i++)
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
        WORD gn, gpn;

        if (CanEnableGCCPUGroups())
        {
            GetGroupForProcessor(procIndex, &gn, &gpn);
        }
        else
        {
            gn = GroupProcNo::NoGroup;
            gpn = procIndex;
        }

        GroupProcNo groupProcNo(gn, gpn);
        *proc_no = groupProcNo.GetCombinedValue();

        PROCESSOR_NUMBER procNumber;

        if (CanEnableGCCPUGroups())
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

            if (!GetNumaProcessorNodeEx(&procNumber, node_no))
            {
                *node_no = NUMA_NODE_UNDEFINED;
            }
        }
        else
        {   // no numa setting, each cpu group is treated as a node
            *node_no = procNumber.Group;
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
    assert(g_fEnableGCCPUGroups);

    char* number_end;
    size_t group_number = strtoul(*config_string, &number_end, 10);

    if ((number_end == *config_string) || (*number_end != ':'))
    {
        // No number or no colon after the number found, invalid format
        return false;
    }

    if (group_number >= g_nGroups)
    {
        // Group number out of range
        return false;
    }

    *config_string = number_end + 1;

    size_t start, end;
    if (!ParseIndexOrRange(config_string, &start, &end))
    {
        return false;
    }

    uint16_t group_processor_count = g_CPUGroupInfoArray[group_number].nr_active;
    if ((start >= group_processor_count) || (end >= group_processor_count))
    {
        // Invalid CPU index values or range
        return false;
    }

    uint16_t group_begin = g_CPUGroupInfoArray[group_number].begin;

    *start_index = group_begin + start;
    *end_index = group_begin + end;

    return true;
}

// Parameters of the GC thread stub
struct GCThreadStubParam
{
    GCThreadFunction GCThreadFunction;
    void* GCThreadParam;
};

// GC thread stub to convert GC thread function to an OS specific thread function
static DWORD GCThreadStub(void* param)
{
    GCThreadStubParam *stubParam = (GCThreadStubParam*)param;
    GCThreadFunction function = stubParam->GCThreadFunction;
    void* threadParam = stubParam->GCThreadParam;

    delete stubParam;

    function(threadParam);

    return 0;
}

// Initialize the critical section
bool CLRCriticalSection::Initialize()
{
    ::InitializeCriticalSection(&m_cs);
    return true;
}

// Destroy the critical section
void CLRCriticalSection::Destroy()
{
    ::DeleteCriticalSection(&m_cs);
}

// Enter the critical section. Blocks until the section can be entered.
void CLRCriticalSection::Enter()
{
    ::EnterCriticalSection(&m_cs);
}

// Leave the critical section
void CLRCriticalSection::Leave()
{
    ::LeaveCriticalSection(&m_cs);
}

// WindowsEvent is an implementation of GCEvent that forwards
// directly to Win32 APIs.
class GCEvent::Impl
{
private:
    HANDLE m_hEvent;

public:
    Impl() : m_hEvent(INVALID_HANDLE_VALUE) {}

    bool IsValid() const
    {
        return m_hEvent != INVALID_HANDLE_VALUE;
    }

    void Set()
    {
        assert(IsValid());
        BOOL result = SetEvent(m_hEvent);
        assert(result && "SetEvent failed");
    }

    void Reset()
    {
        assert(IsValid());
        BOOL result = ResetEvent(m_hEvent);
        assert(result && "ResetEvent failed");
    }

    uint32_t Wait(uint32_t timeout, bool alertable)
    {
        UNREFERENCED_PARAMETER(alertable);
        assert(IsValid());

        return WaitForSingleObject(m_hEvent, timeout);
    }

    void CloseEvent()
    {
        assert(IsValid());
        BOOL result = CloseHandle(m_hEvent);
        assert(result && "CloseHandle failed");
        m_hEvent = INVALID_HANDLE_VALUE;
    }

    bool CreateAutoEvent(bool initialState)
    {
        m_hEvent = CreateEvent(nullptr, false, initialState, nullptr);
        return IsValid();
    }

    bool CreateManualEvent(bool initialState)
    {
        m_hEvent = CreateEvent(nullptr, true, initialState, nullptr);
        return IsValid();
    }
};

GCEvent::GCEvent()
  : m_impl(nullptr)
{
}

void GCEvent::CloseEvent()
{
    assert(m_impl != nullptr);
    m_impl->CloseEvent();
}

void GCEvent::Set()
{
    assert(m_impl != nullptr);
    m_impl->Set();
}

void GCEvent::Reset()
{
    assert(m_impl != nullptr);
    m_impl->Reset();
}

uint32_t GCEvent::Wait(uint32_t timeout, bool alertable)
{
    assert(m_impl != nullptr);
    return m_impl->Wait(timeout, alertable);
}

bool GCEvent::CreateAutoEventNoThrow(bool initialState)
{
    // [DESKTOP TODO] The difference between events and OS events is
    // whether or not the hosting API is made aware of them. When (if)
    // we implement hosting support for Local GC, we will need to be
    // aware of the host here.
    return CreateOSAutoEventNoThrow(initialState);
}

bool GCEvent::CreateManualEventNoThrow(bool initialState)
{
    // [DESKTOP TODO] The difference between events and OS events is
    // whether or not the hosting API is made aware of them. When (if)
    // we implement hosting support for Local GC, we will need to be
    // aware of the host here.
    return CreateOSManualEventNoThrow(initialState);
}

bool GCEvent::CreateOSAutoEventNoThrow(bool initialState)
{
    assert(m_impl == nullptr);
    std::unique_ptr<GCEvent::Impl> event(new (std::nothrow) GCEvent::Impl());
    if (!event)
    {
        return false;
    }

    if (!event->CreateAutoEvent(initialState))
    {
        return false;
    }

    m_impl = event.release();
    return true;
}

bool GCEvent::CreateOSManualEventNoThrow(bool initialState)
{
    assert(m_impl == nullptr);
    std::unique_ptr<GCEvent::Impl> event(new (std::nothrow) GCEvent::Impl());
    if (!event)
    {
        return false;
    }

    if (!event->CreateManualEvent(initialState))
    {
        return false;
    }

    m_impl = event.release();
    return true;
}
