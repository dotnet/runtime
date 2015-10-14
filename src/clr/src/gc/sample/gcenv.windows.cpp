//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

//
// Implementation of the GC environment
//

#include "common.h"

#include "windows.h"

#include "gcenv.h"
#include "gc.h"

static LARGE_INTEGER performanceFrequency;

MethodTable * g_pFreeObjectMethodTable;

int32_t g_TrapReturningThreads;

bool g_fFinalizerRunOnShutDown;

GCSystemInfo g_SystemInfo;

// Initialize the interface implementation
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::Initialize()
{
    if (!::QueryPerformanceFrequency(&performanceFrequency))
    {
        return false;
    }

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
}

// Get numeric id of the current thread if possible on the
// current platform. It is indended for logging purposes only.
// Return:
//  Numeric id of the current thread or 0 if the 
uint32_t GCToOSInterface::GetCurrentThreadIdForLogging()
{
    return ::GetCurrentThreadId();
}

// Get id of the process
// Return:
//  Id of the current process
uint32_t GCToOSInterface::GetCurrentProcessId()
{
    return ::GetCurrentProcessId();
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
    _ASSERTE(GCToOSInterface::CanGetCurrentProcessorNumber());
    return ::GetCurrentProcessorNumber();
}

// Check if the OS supports getting current processor number
bool GCToOSInterface::CanGetCurrentProcessorNumber()
{
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
    return g_SystemInfo.dwNumberOfProcessors;
}

// Causes the calling thread to sleep for the specified number of milliseconds
// Parameters:
//  sleepMSec   - time to sleep before switching to another thread
void GCToOSInterface::Sleep(uint32_t sleepMSec)
{
    ::Sleep(sleepMSec);
}

// Causes the calling thread to yield execution to another thread that is ready to run on the current processor.
// Parameters:
//  switchCount - number of times the YieldThread was called in a loop
void GCToOSInterface::YieldThread(uint32_t switchCount)
{
    SwitchToThread();
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
    DWORD memFlags = (flags & VirtualReserveFlags::WriteWatch) ? (MEM_RESERVE | MEM_WRITE_WATCH) : MEM_RESERVE;
    return ::VirtualAlloc(0, size, memFlags, PAGE_READWRITE);
}

// Release virtual memory range previously reserved using VirtualReserve
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
// Return:
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualRelease(void* address, size_t size)
{
    UNREFERENCED_PARAMETER(size);
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
    return ::VirtualAlloc(address, size, MEM_COMMIT, PAGE_READWRITE) != NULL;
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
//  true if it has succeeded, false if it has failed
bool GCToOSInterface::VirtualReset(void * address, size_t size, bool unlock)
{
    bool success = ::VirtualAlloc(address, size, MEM_RESET, PAGE_READWRITE) != NULL;
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
    return false;
}

// Reset the write tracking state for the specified virtual memory range.
// Parameters:
//  address - starting virtual address
//  size    - size of the virtual memory range
void GCToOSInterface::ResetWriteWatch(void* address, size_t size)
{
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
    // TODO: implement
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
    return false;
}

// Get number of processors assigned to the current process
// Return:
//  The number of processors
uint32_t GCToOSInterface::GetCurrentProcessCpuCount()
{
    return g_SystemInfo.dwNumberOfProcessors;
}

// Get global memory status
// Parameters:
//  ms - pointer to the structure that will be filled in with the memory status
void GCToOSInterface::GetMemoryStatus(GCMemoryStatus* ms)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    MEMORYSTATUSEX memStatus;

    memStatus.dwLength = sizeof(MEMORYSTATUSEX);
    BOOL fRet = GlobalMemoryStatusEx(&memStatus);
    _ASSERTE (fRet);

    // If the machine has more RAM than virtual address limit, let us cap it.
    // Our GC can never use more than virtual address limit.
    if (memStatus.ullAvailPhys > memStatus.ullTotalVirtual)
    {
        memStatus.ullAvailPhys = memStatus.ullAvailVirtual;
    }

    // Convert Windows struct to abstract struct
    ms->dwMemoryLoad              = memStatus.dwMemoryLoad           ;
    ms->ullTotalPhys              = memStatus.ullTotalPhys           ;
    ms->ullAvailPhys              = memStatus.ullAvailPhys           ;
    ms->ullTotalPageFile          = memStatus.ullTotalPageFile       ;
    ms->ullAvailPageFile          = memStatus.ullAvailPageFile       ;
    ms->ullTotalVirtual           = memStatus.ullTotalVirtual        ;
    ms->ullAvailVirtual           = memStatus.ullAvailVirtual        ;
}

// Get a high precision performance counter
// Return:
//  The counter value
int64_t GCToOSInterface::QueryPerformanceCounter()
{
    LARGE_INTEGER ts;
    if (!::QueryPerformanceCounter(&ts))
    {
        _ASSERTE(!"Fatal Error - cannot query performance counter.");
        abort();
    }

    return ts.QuadPart;
}

// Get a frequency of the high precision performance counter
// Return:
//  The counter frequency
int64_t GCToOSInterface::QueryPerformanceFrequency()
{
    LARGE_INTEGER frequency;
    if (!::QueryPerformanceFrequency(&frequency))
    {
        _ASSERTE(!"Fatal Error - cannot query performance counter.");
        abort();
    }

    return frequency.QuadPart;
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
    stubParam->GCThreadFunction(stubParam->GCThreadParam);

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
    DWORD thread_id;
    GCThreadStubParam stubParam;

    stubParam.GCThreadFunction = function;
    stubParam.GCThreadParam = param;

    HANDLE gc_thread = ::CreateThread(NULL, 0, GCThreadStub, &stubParam, CREATE_SUSPENDED, &thread_id);

    if (!gc_thread)
    {
        return false;
    }

    SetThreadPriority(gc_thread, /* THREAD_PRIORITY_ABOVE_NORMAL );*/ THREAD_PRIORITY_HIGHEST );

    ResumeThread(gc_thread);

    CloseHandle(gc_thread);

    return true;
}

// Open a file
// Parameters:
//  filename - name of the file to open
//  mode     - mode to open the file in (like in the CRT fopen)
// Return:
//  FILE* of the opened file
FILE* GCToOSInterface::OpenFile(const WCHAR* filename, const WCHAR* mode)
{
    return _wfopen(filename, mode);
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

void DestroyThread(Thread * pThread)
{
    // TODO: implement
}
