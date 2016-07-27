// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * gcenv.os.cpp
 *
 * GCToOSInterface implementation
 *

 *
 */

#include "common.h"
#include "gcenv.h"

#ifndef FEATURE_PAL
#include <Psapi.h>
#endif

#define MAX_PTR ((uint8_t*)(~(ptrdiff_t)0))

// Initialize the interface implementation
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::Initialize()
{
    LIMITED_METHOD_CONTRACT;
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

// Set ideal affinity for the current thread
// Parameters:
//  affinity - ideal processor affinity for the thread
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::SetCurrentThreadIdealAffinity(GCThreadAffinity* affinity)
{
    LIMITED_METHOD_CONTRACT;

    bool success = true;

#if !defined(FEATURE_CORESYSTEM)
    SetThreadIdealProcessor(GetCurrentThread(), (DWORD)affinity->Processor);
#elif !defined(FEATURE_PAL)
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
            proc.Number = (BYTE)affinity->Processor;
            success = !!SetThreadIdealProcessorEx(GetCurrentThread(), &proc, &proc);
        }        
    }
#endif

    return success;
}

// Get the number of the current processor
uint32_t GCToOSInterface::GetCurrentProcessorNumber()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(CanGetCurrentProcessorNumber());
    return ::GetCurrentProcessorNumber();
}

// Check if the OS supports getting current processor number
bool GCToOSInterface::CanGetCurrentProcessorNumber()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_PAL
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

// Get number of logical processors
uint32_t GCToOSInterface::GetLogicalCpuCount()
{
    LIMITED_METHOD_CONTRACT;
    return ::GetLogicalCpuCount();
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
// Return:
//  Starting virtual address of the reserved range
void* GCToOSInterface::VirtualReserve(void* address, size_t size, size_t alignment, uint32_t flags)
{
    LIMITED_METHOD_CONTRACT;

    DWORD memFlags = (flags & VirtualReserveFlags::WriteWatch) ? (MEM_RESERVE | MEM_WRITE_WATCH) : MEM_RESERVE;
    if (alignment == 0)
    {
        return ::ClrVirtualAlloc(0, size, memFlags, PAGE_READWRITE);
    }
    else
    {
        return ::ClrVirtualAllocAligned(0, size, memFlags, PAGE_READWRITE, alignment);
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

// Commit virtual memory range. It must be part of a range reserved using VirtualReserve.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualCommit(void* address, size_t size)
{
    LIMITED_METHOD_CONTRACT;

    return ::ClrVirtualAlloc(address, size, MEM_COMMIT, PAGE_READWRITE) != NULL;
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
#ifndef FEATURE_PAL
    if (success && unlock)
    {
        // Remove the page range from the working set
        ::VirtualUnlock(address, size);
    }
#endif // FEATURE_PAL

    return success;
}

// Check if the OS supports write watching
bool GCToOSInterface::SupportsWriteWatch()
{
    LIMITED_METHOD_CONTRACT;

    bool writeWatchSupported = false;

    // check if the OS supports write-watch. 
    // Drawbridge does not support write-watch so we still need to do the runtime detection for them.
    // Otherwise, all currently supported OSes do support write-watch.
    void* mem = VirtualReserve (0, g_SystemInfo.dwAllocationGranularity, 0, VirtualReserveFlags::WriteWatch);
    if (mem != NULL)
    {
        VirtualRelease (mem, g_SystemInfo.dwAllocationGranularity);
        writeWatchSupported = true;
    }

    return writeWatchSupported;
}

// Reset the write tracking state for the specified virtual memory range.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
void GCToOSInterface::ResetWriteWatch(void* address, size_t size)
{
    LIMITED_METHOD_CONTRACT;

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
    LIMITED_METHOD_CONTRACT;

    uint32_t flags = resetState ? 1 : 0;
    ULONG granularity;

    bool success = ::GetWriteWatch(flags, address, size, pageAddresses, (ULONG_PTR*)pageAddressesCount, &granularity) == 0;
    _ASSERTE (granularity == OS_PAGE_SIZE);

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
    LIMITED_METHOD_CONTRACT;

    return ::GetLargestOnDieCacheSize(trueSize);
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
    LIMITED_METHOD_CONTRACT;

#ifndef FEATURE_PAL
    return !!::GetProcessAffinityMask(GetCurrentProcess(), (PDWORD_PTR)processMask, (PDWORD_PTR)systemMask);
#else
    return false;
#endif
}

// Get number of processors assigned to the current process
// Return:
//  The number of processors
uint32_t GCToOSInterface::GetCurrentProcessCpuCount()
{
    LIMITED_METHOD_CONTRACT;

    return ::GetCurrentProcessCpuCount();
}

// Return the size of the user-mode portion of the virtual address space of this process.
// Return:
//  non zero if it has succeeded, 0 if it has failed
size_t GCToOSInterface::GetVirtualMemoryLimit()
{
    LIMITED_METHOD_CONTRACT;

    MEMORYSTATUSEX memStatus;
    ::GetProcessMemoryLoad(&memStatus);

    return (size_t)memStatus.ullTotalVirtual;
}


#ifndef FEATURE_PAL

typedef BOOL (WINAPI *PGET_PROCESS_MEMORY_INFO)(HANDLE handle, PROCESS_MEMORY_COUNTERS* memCounters, uint32_t cb);
static PGET_PROCESS_MEMORY_INFO GCGetProcessMemoryInfo = 0;

static size_t g_RestrictedPhysicalMemoryLimit = (size_t)MAX_PTR;

typedef BOOL (WINAPI *PIS_PROCESS_IN_JOB)(HANDLE processHandle, HANDLE jobHandle, BOOL* result);
typedef BOOL (WINAPI *PQUERY_INFORMATION_JOB_OBJECT)(HANDLE jobHandle, JOBOBJECTINFOCLASS jobObjectInfoClass, void* lpJobObjectInfo, DWORD cbJobObjectInfoLength, LPDWORD lpReturnLength);

#ifdef FEATURE_CORECLR
// For coresys we need to look for an API in some apiset dll on win8 if we can't find it  
// in the traditional dll.
HINSTANCE LoadDllForAPI(WCHAR* dllTraditional, WCHAR* dllApiSet)
{
    HINSTANCE hinst = WszLoadLibrary(dllTraditional);

    if (!hinst)
    {
        if(RunningOnWin8())
            hinst = WszLoadLibrary(dllApiSet);
    }

    return hinst;
}
#endif

static size_t GetRestrictedPhysicalMemoryLimit()
{
    LIMITED_METHOD_CONTRACT;

    // The limit was cached already
    if (g_RestrictedPhysicalMemoryLimit != (size_t)MAX_PTR)
        return g_RestrictedPhysicalMemoryLimit;

    size_t job_physical_memory_limit = (size_t)MAX_PTR;
    BOOL in_job_p = FALSE;
#ifdef FEATURE_CORECLR
    HINSTANCE hinstApiSetPsapiOrKernel32 = 0;
    // these 2 modules will need to be freed no matter what as we only use them locally in this method.
    HINSTANCE hinstApiSetJob1OrKernel32 = 0;
    HINSTANCE hinstApiSetJob2OrKernel32 = 0;
#else
    HINSTANCE hinstPsapi = 0;
#endif

    PIS_PROCESS_IN_JOB GCIsProcessInJob = 0;
    PQUERY_INFORMATION_JOB_OBJECT GCQueryInformationJobObject = 0;

#ifdef FEATURE_CORECLR
    hinstApiSetJob1OrKernel32 = LoadDllForAPI(L"kernel32.dll", L"api-ms-win-core-job-l1-1-0.dll");
    if (!hinstApiSetJob1OrKernel32)
        goto exit;

    GCIsProcessInJob = (PIS_PROCESS_IN_JOB)GetProcAddress(hinstApiSetJob1OrKernel32, "IsProcessInJob");
    if (!GCIsProcessInJob)
        goto exit;
#else
    GCIsProcessInJob = &(::IsProcessInJob);
#endif

    if (!GCIsProcessInJob(GetCurrentProcess(), NULL, &in_job_p))
        goto exit;

    if (in_job_p)
    {
#ifdef FEATURE_CORECLR
        hinstApiSetPsapiOrKernel32 = LoadDllForAPI(L"kernel32.dll", L"api-ms-win-core-psapi-l1-1-0");
        if (!hinstApiSetPsapiOrKernel32)
            goto exit;

        GCGetProcessMemoryInfo = (PGET_PROCESS_MEMORY_INFO)GetProcAddress(hinstApiSetPsapiOrKernel32, "K32GetProcessMemoryInfo");
#else
        // We need a way to get the working set in a job object and GetProcessMemoryInfo 
        // is the way to get that. According to MSDN, we should use GetProcessMemoryInfo In order to 
        // compensate for the incompatibility that psapi.dll introduced we are getting this dynamically.
        hinstPsapi = WszLoadLibrary(L"psapi.dll");
        if (!hinstPsapi)
            return 0;
        GCGetProcessMemoryInfo = (PGET_PROCESS_MEMORY_INFO)GetProcAddress(hinstPsapi, "GetProcessMemoryInfo");
#endif

        if (!GCGetProcessMemoryInfo)
            goto exit;

#ifdef FEATURE_CORECLR
        hinstApiSetJob2OrKernel32 = LoadDllForAPI(L"kernel32.dll", L"api-ms-win-core-job-l2-1-0");
        if (!hinstApiSetJob2OrKernel32)
            goto exit;

        GCQueryInformationJobObject = (PQUERY_INFORMATION_JOB_OBJECT)GetProcAddress(hinstApiSetJob2OrKernel32, "QueryInformationJobObject");
#else
        GCQueryInformationJobObject = &(::QueryInformationJobObject);
#endif 

        if (!GCQueryInformationJobObject)
            goto exit;

        JOBOBJECT_EXTENDED_LIMIT_INFORMATION limit_info;
        if (GCQueryInformationJobObject (NULL, JobObjectExtendedLimitInformation, &limit_info, 
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

            job_physical_memory_limit = min (job_memory_limit, job_process_memory_limit);
            job_physical_memory_limit = min (job_physical_memory_limit, job_workingset_limit);

            MEMORYSTATUSEX ms;
            ::GetProcessMemoryLoad(&ms);

            // A sanity check in case someone set a larger limit than there is actual physical memory.
            job_physical_memory_limit = (size_t) min (job_physical_memory_limit, ms.ullTotalPhys);
        }
    }

exit:
#ifdef FEATURE_CORECLR
    if (hinstApiSetJob1OrKernel32)
        FreeLibrary(hinstApiSetJob1OrKernel32);
    if (hinstApiSetJob2OrKernel32)
        FreeLibrary(hinstApiSetJob2OrKernel32);
#endif

    if (job_physical_memory_limit == (size_t)MAX_PTR)
    {
        job_physical_memory_limit = 0;

#ifdef FEATURE_CORECLR
        FreeLibrary(hinstApiSetPsapiOrKernel32);
#else
        FreeLibrary(hinstPsapi);
#endif
    }

    VolatileStore(&g_RestrictedPhysicalMemoryLimit, job_physical_memory_limit);
    return g_RestrictedPhysicalMemoryLimit;
}

#endif // FEATURE_PAL


// Get the physical memory that this process can use.
// Return:
//  non zero if it has succeeded, 0 if it has failed
uint64_t GCToOSInterface::GetPhysicalMemoryLimit()
{
    LIMITED_METHOD_CONTRACT;

#ifndef FEATURE_PAL
    size_t restricted_limit = GetRestrictedPhysicalMemoryLimit();
    if (restricted_limit != 0)
        return restricted_limit;
#endif

    MEMORYSTATUSEX memStatus;
    ::GetProcessMemoryLoad(&memStatus);

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
void GCToOSInterface::GetMemoryStatus(uint32_t* memory_load, uint64_t* available_physical, uint64_t* available_page_file)
{
    LIMITED_METHOD_CONTRACT;

#ifndef FEATURE_PAL
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
#endif

    MEMORYSTATUSEX ms;
    ::GetProcessMemoryLoad(&ms);

    if (memory_load != NULL)
        *memory_load = ms.dwMemoryLoad;
    if (available_physical != NULL)
        *available_physical = ms.ullAvailPhys;
    if (available_page_file != NULL)
        *available_page_file = ms.ullAvailPageFile;
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

// Parameters of the GC thread stub
struct GCThreadStubParam
{
    GCThreadFunction GCThreadFunction;
    void* GCThreadParam;
};

// GC thread stub to convert GC thread function to an OS specific thread function
static DWORD GCThreadStub(void* param)
{
    WRAPPER_NO_CONTRACT;

    GCThreadStubParam *stubParam = (GCThreadStubParam*)param;
    GCThreadFunction function = stubParam->GCThreadFunction;
    void* threadParam = stubParam->GCThreadParam;

    delete stubParam;

    function(threadParam);

    return 0;
}

// Create a new thread
// Parameters:
//  function - the function to be executed by the thread
//  param    - parameters of the thread
//  affinity - processor affinity of the thread
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::CreateThread(GCThreadFunction function, void* param, GCThreadAffinity* affinity)
{
    LIMITED_METHOD_CONTRACT;

    uint32_t thread_id;

    NewHolder<GCThreadStubParam> stubParam = new (nothrow) GCThreadStubParam();
    if (stubParam == NULL)
    {
        return false;
    }

    stubParam->GCThreadFunction = function;
    stubParam->GCThreadParam = param;

    HANDLE gc_thread = Thread::CreateUtilityThread(Thread::StackSize_Medium, GCThreadStub, stubParam, CREATE_SUSPENDED, (DWORD*)&thread_id);

    if (!gc_thread)
    {
        return false;
    }

    stubParam.SuppressRelease();

    SetThreadPriority(gc_thread, /* THREAD_PRIORITY_ABOVE_NORMAL );*/ THREAD_PRIORITY_HIGHEST );

#ifndef FEATURE_PAL
    if (affinity->Group != GCThreadAffinity::None)
    {
        _ASSERTE(affinity->Processor != GCThreadAffinity::None);
        GROUP_AFFINITY ga;
        ga.Group = (WORD)affinity->Group;
        ga.Reserved[0] = 0; // reserve must be filled with zero
        ga.Reserved[1] = 0; // otherwise call may fail
        ga.Reserved[2] = 0;
        ga.Mask = (size_t)1 << affinity->Processor;

        CPUGroupInfo::SetThreadGroupAffinity(gc_thread, &ga, NULL);
    }
    else if (affinity->Processor != GCThreadAffinity::None)
    {
        SetThreadAffinityMask(gc_thread, (DWORD_PTR)1 << affinity->Processor);
    }
#endif // !FEATURE_PAL

    ResumeThread(gc_thread);
    CloseHandle(gc_thread);

    return true;
}

// Initialize the critical section
void CLRCriticalSection::Initialize()
{
    WRAPPER_NO_CONTRACT;
    UnsafeInitializeCriticalSection(&m_cs);
}

// Destroy the critical section
void CLRCriticalSection::Destroy()
{
    WRAPPER_NO_CONTRACT;
    UnsafeDeleteCriticalSection(&m_cs);
}

// Enter the critical section. Blocks until the section can be entered.
void CLRCriticalSection::Enter()
{
    WRAPPER_NO_CONTRACT;
    UnsafeEnterCriticalSection(&m_cs);
}

// Leave the critical section
void CLRCriticalSection::Leave()
{
    WRAPPER_NO_CONTRACT;
    UnsafeLeaveCriticalSection(&m_cs);
}
