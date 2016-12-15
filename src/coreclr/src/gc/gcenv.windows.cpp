// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <cstdint>
#include <cassert>
#include <cstddef>
#include <memory>
#include "windows.h"
#include "psapi.h"
#include "env/gcenv.structs.h"
#include "env/gcenv.base.h"
#include "env/gcenv.os.h"

GCSystemInfo g_SystemInfo;

typedef BOOL (WINAPI *PGET_PROCESS_MEMORY_INFO)(HANDLE handle, PROCESS_MEMORY_COUNTERS* memCounters, uint32_t cb);
static PGET_PROCESS_MEMORY_INFO GCGetProcessMemoryInfo = 0;

static size_t g_RestrictedPhysicalMemoryLimit = (size_t)UINTPTR_MAX;

typedef BOOL (WINAPI *PIS_PROCESS_IN_JOB)(HANDLE processHandle, HANDLE jobHandle, BOOL* result);
typedef BOOL (WINAPI *PQUERY_INFORMATION_JOB_OBJECT)(HANDLE jobHandle, JOBOBJECTINFOCLASS jobObjectInfoClass, void* lpJobObjectInfo, DWORD cbJobObjectInfoLength, LPDWORD lpReturnLength);

namespace {

void GetProcessMemoryLoad(LPMEMORYSTATUSEX pMSEX)
{
    pMSEX->dwLength = sizeof(MEMORYSTATUSEX);
    BOOL fRet = ::GlobalMemoryStatusEx(pMSEX);
    assert(fRet);

    // If the machine has more RAM than virtual address limit, let us cap it.
    // Our GC can never use more than virtual address limit.
    if (pMSEX->ullAvailPhys > pMSEX->ullTotalVirtual)
    {
        pMSEX->ullAvailPhys = pMSEX->ullAvailVirtual;
    }
}

static size_t GetRestrictedPhysicalMemoryLimit()
{
    LIMITED_METHOD_CONTRACT;

    // The limit was cached already
    if (g_RestrictedPhysicalMemoryLimit != (size_t)UINTPTR_MAX)
        return g_RestrictedPhysicalMemoryLimit;

    size_t job_physical_memory_limit = (size_t)UINTPTR_MAX;
    BOOL in_job_p = FALSE;
    HINSTANCE hinstKernel32 = 0;

    PIS_PROCESS_IN_JOB GCIsProcessInJob = 0;
    PQUERY_INFORMATION_JOB_OBJECT GCQueryInformationJobObject = 0;

    hinstKernel32 = LoadLibraryEx(L"kernel32.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!hinstKernel32)
        goto exit;

    GCIsProcessInJob = (PIS_PROCESS_IN_JOB)GetProcAddress(hinstKernel32, "IsProcessInJob");
    if (!GCIsProcessInJob)
        goto exit;

    if (!GCIsProcessInJob(GetCurrentProcess(), NULL, &in_job_p))
        goto exit;

    if (in_job_p)
    {
        GCGetProcessMemoryInfo = (PGET_PROCESS_MEMORY_INFO)GetProcAddress(hinstKernel32, "K32GetProcessMemoryInfo");

        if (!GCGetProcessMemoryInfo)
            goto exit;

        GCQueryInformationJobObject = (PQUERY_INFORMATION_JOB_OBJECT)GetProcAddress(hinstKernel32, "QueryInformationJobObject");

        if (!GCQueryInformationJobObject)
            goto exit;

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limit_info;
        if (GCQueryInformationJobObject (NULL, JobObjectExtendedLimitInformation, &limit_info, 
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

            job_physical_memory_limit = min (job_memory_limit, job_process_memory_limit);
            job_physical_memory_limit = min (job_physical_memory_limit, job_workingset_limit);

            MEMORYSTATUSEX ms;
            ::GetProcessMemoryLoad(&ms);

            // A sanity check in case someone set a larger limit than there is actual physical memory.
            job_physical_memory_limit = (size_t) min (job_physical_memory_limit, ms.ullTotalPhys);
        }
    }

exit:
    if (job_physical_memory_limit == (size_t)UINTPTR_MAX)
    {
        job_physical_memory_limit = 0;

        FreeLibrary(hinstKernel32);
    }

    VolatileStore(&g_RestrictedPhysicalMemoryLimit, job_physical_memory_limit);
    return g_RestrictedPhysicalMemoryLimit;
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

// Set ideal affinity for the current thread
// Parameters:
//  affinity - ideal processor affinity for the thread
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::SetCurrentThreadIdealAffinity(GCThreadAffinity* affinity)
{
    bool success = true;

#if !defined(FEATURE_CORESYSTEM)
    SetThreadIdealProcessor(GetCurrentThread(), (DWORD)affinity->Processor);
#else
    PROCESSOR_NUMBER proc;

    if (affinity->Group != -1)
    {
        proc.Group = (WORD)affinity->Group;
        proc.Number = (BYTE)affinity->Processor;
        proc.Reserved = 0;

        success = !!SetThreadIdealProcessorEx(GetCurrentThread(), &proc, NULL);
    }
    else
    {
        if (GetThreadIdealProcessorEx(GetCurrentThread(), &proc))
        {
            proc.Number = affinity->Processor;
            success = !!SetThreadIdealProcessorEx(GetCurrentThread(), &proc, NULL);
        }
    }
#endif

    return success;
}

// Get the number of the current processor
uint32_t GCToOSInterface::GetCurrentProcessorNumber()
{
    assert(GCToOSInterface::CanGetCurrentProcessorNumber());
    return ::GetCurrentProcessorNumber();
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

// Get number of logical processors
uint32_t GCToOSInterface::GetLogicalCpuCount()
{
    // TODO(segilles) processor detection
    return 1;
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
//  address   - starting virtual address, it can be NULL to let the function choose the starting address
//  size      - size of the virtual memory range
//  alignment - requested memory alignment, 0 means no specific alignment requested
//  flags     - flags to control special settings like write watching
// Return:
//  Starting virtual address of the reserved range
void* GCToOSInterface::VirtualReserve(size_t size, size_t alignment, uint32_t flags)
{
    // Windows already ensures 64kb alignment on VirtualAlloc. The current CLR
    // implementation ignores it on Windows, other than making some sanity checks on it.
    UNREFERENCED_PARAMETER(alignment);
    assert((alignment & (alignment - 1)) == 0);
    assert(alignment <= 0x10000);
    DWORD memFlags = (flags & VirtualReserveFlags::WriteWatch) ? (MEM_RESERVE | MEM_WRITE_WATCH) : MEM_RESERVE;
    return ::VirtualAlloc(nullptr, size, memFlags, PAGE_READWRITE);
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

// Commit virtual memory range. It must be part of a range reserved using VirtualReserve.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualCommit(void* address, size_t size)
{
    return ::VirtualAlloc(address, size, MEM_COMMIT, PAGE_READWRITE) != nullptr;
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
size_t GCToOSInterface::GetLargestOnDieCacheSize(bool trueSize)
{
    // TODO(segilles) processor detection (see src/vm/util.cpp:1935)
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
    return !!::GetProcessAffinityMask(::GetCurrentProcess(), (PDWORD_PTR)processMask, (PDWORD_PTR)systemMask);
}

// Get number of processors assigned to the current process
// Return:
//  The number of processors
uint32_t GCToOSInterface::GetCurrentProcessCpuCount()
{
    // TODO(segilles) this does not take into account process affinity
    return g_SystemInfo.dwNumberOfProcessors;
}

// Return the size of the user-mode portion of the virtual address space of this process.
// Return:
//  non zero if it has succeeded, 0 if it has failed
size_t GCToOSInterface::GetVirtualMemoryLimit()
{
    MEMORYSTATUSEX memStatus;
    if (::GlobalMemoryStatusEx(&memStatus))
    {
        return (size_t)memStatus.ullAvailVirtual;
    }

    return 0;
}

// Get the physical memory that this process can use.
// Return:
//  non zero if it has succeeded, 0 if it has failed
// Remarks:
//  If a process runs with a restricted memory limit, it returns the limit. If there's no limit 
//  specified, it returns amount of actual physical memory.
uint64_t GCToOSInterface::GetPhysicalMemoryLimit()
{
    size_t restricted_limit = GetRestrictedPhysicalMemoryLimit();
    if (restricted_limit != 0)
        return restricted_limit;

    MEMORYSTATUSEX memStatus;
    if (::GlobalMemoryStatusEx(&memStatus))
    {
        return memStatus.ullTotalPhys;
    }

    return 0;
}

// Get memory status
// Parameters:
//  memory_load - A number between 0 and 100 that specifies the approximate percentage of physical memory
//      that is in use (0 indicates no memory use and 100 indicates full memory use).
//  available_physical - The amount of physical memory currently available, in bytes.
//  available_page_file - The maximum amount of memory the current process can commit, in bytes.
void GCToOSInterface::GetMemoryStatus(uint32_t* memory_load, uint64_t* available_physical, uint64_t* available_page_file)
{
    uint64_t restricted_limit = GetRestrictedPhysicalMemoryLimit();
    if (restricted_limit != 0)
    {
        PROCESS_MEMORY_COUNTERS pmc;
        if (GCGetProcessMemoryInfo(GetCurrentProcess(), &pmc, sizeof(pmc)))
        {
            if (memory_load)
                *memory_load = (uint32_t)((float)pmc.WorkingSetSize * 100.0 / (float)restricted_limit);
            if (available_physical)
                *available_physical = restricted_limit - pmc.WorkingSetSize;
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

    if (memory_load != nullptr)
        *memory_load = ms.dwMemoryLoad;
    if (available_physical != nullptr)
        *available_physical = ms.ullAvailPhys;
    if (available_page_file != nullptr)
        *available_page_file = ms.ullAvailPageFile;
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


// Create a new thread for GC use
// Parameters:
//  function - the function to be executed by the thread
//  param    - parameters of the thread
//  affinity - processor affinity of the thread
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::CreateThread(GCThreadFunction function, void* param, GCThreadAffinity* affinity)
{
    uint32_t thread_id;

    std::unique_ptr<GCThreadStubParam> stubParam(new (std::nothrow) GCThreadStubParam());
    if (!stubParam)
    {
        return false;
    }

    stubParam->GCThreadFunction = function;
    stubParam->GCThreadParam = param;

    HANDLE gc_thread = ::CreateThread(
        nullptr, 
        512 * 1024 /* Thread::StackSize_Medium */, 
        (LPTHREAD_START_ROUTINE)GCThreadStub, 
        stubParam.get(), 
        CREATE_SUSPENDED | STACK_SIZE_PARAM_IS_A_RESERVATION, 
        (DWORD*)&thread_id);

    if (!gc_thread)
    {
        return false;
    }

    stubParam.release();
    bool result = !!::SetThreadPriority(gc_thread, /* THREAD_PRIORITY_ABOVE_NORMAL );*/ THREAD_PRIORITY_HIGHEST );
    assert(result && "failed to set thread priority");  

    if (affinity->Group != GCThreadAffinity::None)
    {
        assert(affinity->Processor != GCThreadAffinity::None);
        GROUP_AFFINITY ga;
        ga.Group = (WORD)affinity->Group;
        ga.Reserved[0] = 0; // reserve must be filled with zero
        ga.Reserved[1] = 0; // otherwise call may fail
        ga.Reserved[2] = 0;
        ga.Mask = (size_t)1 << affinity->Processor;

        bool result = !!::SetThreadGroupAffinity(gc_thread, &ga, nullptr);
        assert(result && "failed to set thread affinity");
    }
    else if (affinity->Processor != GCThreadAffinity::None)
    {
        ::SetThreadAffinityMask(gc_thread, (DWORD_PTR)1 << affinity->Processor);
    }

    return true;
}

// Initialize the critical section
void CLRCriticalSection::Initialize()
{
    ::InitializeCriticalSection(&m_cs);
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
