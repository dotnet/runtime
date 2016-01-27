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
uint32_t GCToOSInterface::GetCurrentThreadIdForLogging()
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
            success = !!SetThreadIdealProcessorEx(GetCurrentThread(), &proc, NULL);
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

#ifndef FEATURE_CORECLR
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

// Get global memory status
// Parameters:
//  ms - pointer to the structure that will be filled in with the memory status
void GCToOSInterface::GetMemoryStatus(GCMemoryStatus* ms)
{
    LIMITED_METHOD_CONTRACT;

    MEMORYSTATUSEX msEx;
    msEx.dwLength = sizeof(MEMORYSTATUSEX);

    ::GetProcessMemoryLoad(&msEx);

    // Convert Windows struct to abstract struct
    ms->dwMemoryLoad = msEx.dwMemoryLoad;
    ms->ullTotalPhys = msEx.ullTotalPhys;
    ms->ullAvailPhys = msEx.ullAvailPhys;
    ms->ullTotalPageFile = msEx.ullTotalPageFile;
    ms->ullAvailPageFile = msEx.ullAvailPageFile;
    ms->ullTotalVirtual = msEx.ullTotalVirtual;
    ms->ullAvailVirtual = msEx.ullAvailVirtual;
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

#ifndef FEATURE_CORECLR
    if (affinity->Group != -1)
    {
        _ASSERTE(affinity->Processor != -1);
        GROUP_AFFINITY ga;
        ga.Group = (WORD)affinity->Group;
        ga.Reserved[0] = 0; // reserve must be filled with zero
        ga.Reserved[1] = 0; // otherwise call may fail
        ga.Reserved[2] = 0;
        ga.Mask = 1 << affinity->Processor;

        CPUGroupInfo::SetThreadGroupAffinity(gc_thread, &ga, NULL);
    }
    else if (affinity->Processor != -1)
    {
        SetThreadAffinityMask(gc_thread, 1 << affinity->Processor);
    }
#endif // !FEATURE_CORECLR

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

