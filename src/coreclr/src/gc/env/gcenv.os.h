//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
// Interface between GC and the OS specific functionality
//

#ifndef __GCENV_OS_H__
#define __GCENV_OS_H__

// Critical section used by the GC
class CLRCriticalSection
{
    CRITICAL_SECTION m_cs;

public:
    // Initialize the critical section
    void Initialize();

    // Destroy the critical section
    void Destroy();

    // Enter the critical section. Blocks until the section can be entered.
    void Enter();

    // Leave the critical section
    void Leave();
};

// Flags for the GCToOSInterface::VirtualReserve method
struct VirtualReserveFlags
{
    enum
    {
        None = 0,
        WriteWatch = 1,
    };
};

// Affinity of a GC thread
struct GCThreadAffinity
{
    static const int None = -1;

    // Processor group index, None if no group is specified
    int Group;
    // Processor index, None if no affinity is specified
    int Processor;
};

// GC thread function prototype
typedef void (*GCThreadFunction)(void* param);

// Interface that the GC uses to invoke OS specific functionality
class GCToOSInterface
{
public:

    //
    // Initialization and shutdown of the interface
    //

    // Initialize the interface implementation
    // Return:
    //  true if it has succeeded, false if it has failed
    static bool Initialize();

    // Shutdown the interface implementation
    static void Shutdown();

    //
    // Virtual memory management
    //

    // Reserve virtual memory range.
    // Parameters:
    //  address   - starting virtual address, it can be NULL to let the function choose the starting address
    //  size      - size of the virtual memory range
    //  alignment - requested memory alignment
    //  flags     - flags to control special settings like write watching
    // Return:
    //  Starting virtual address of the reserved range
    static void* VirtualReserve(void *address, size_t size, size_t alignment, uint32_t flags);

    // Release virtual memory range previously reserved using VirtualReserve
    // Parameters:
    //  address - starting virtual address
    //  size    - size of the virtual memory range
    // Return:
    //  true if it has succeeded, false if it has failed
    static bool VirtualRelease(void *address, size_t size);

    // Commit virtual memory range. It must be part of a range reserved using VirtualReserve.
    // Parameters:
    //  address - starting virtual address
    //  size    - size of the virtual memory range
    // Return:
    //  true if it has succeeded, false if it has failed
    static bool VirtualCommit(void *address, size_t size);

    // Decomit virtual memory range.
    // Parameters:
    //  address - starting virtual address
    //  size    - size of the virtual memory range
    // Return:
    //  true if it has succeeded, false if it has failed
    static bool VirtualDecommit(void *address, size_t size);

    // Reset virtual memory range. Indicates that data in the memory range specified by address and size is no 
    // longer of interest, but it should not be decommitted.
    // Parameters:
    //  address - starting virtual address
    //  size    - size of the virtual memory range
    //  unlock  - true if the memory range should also be unlocked
    // Return:
    //  true if it has succeeded, false if it has failed
    static bool VirtualReset(void *address, size_t size, bool unlock);

    //
    // Write watching
    //

    // Check if the OS supports write watching
    static bool SupportsWriteWatch();

    // Reset the write tracking state for the specified virtual memory range.
    // Parameters:
    //  address - starting virtual address
    //  size    - size of the virtual memory range
    static void ResetWriteWatch(void *address, size_t size);

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
    static bool GetWriteWatch(bool resetState, void* address, size_t size, void** pageAddresses, uintptr_t* pageAddressesCount);

    //
    // Thread and process
    //

    // Create a new thread
    // Parameters:
    //  function - the function to be executed by the thread
    //  param    - parameters of the thread
    //  affinity - processor affinity of the thread
    // Return:
    //  true if it has succeeded, false if it has failed
    static bool CreateThread(GCThreadFunction function, void* param, GCThreadAffinity* affinity);

    // Causes the calling thread to sleep for the specified number of milliseconds
    // Parameters:
    //  sleepMSec   - time to sleep before switching to another thread
    static void Sleep(uint32_t sleepMSec);

    // Causes the calling thread to yield execution to another thread that is ready to run on the current processor.
    // Parameters:
    //  switchCount - number of times the YieldThread was called in a loop
    static void YieldThread(uint32_t switchCount);

    // Get the number of the current processor
    static uint32_t GetCurrentProcessorNumber();

    // Check if the OS supports getting current processor number
    static bool CanGetCurrentProcessorNumber();

    // Set ideal processor for the current thread
    // Parameters:
    //  processorIndex - index of the processor in the group
    //  affinity - ideal processor affinity for the thread
    // Return:
    //  true if it has succeeded, false if it has failed
    static bool SetCurrentThreadIdealAffinity(GCThreadAffinity* affinity);

    // Get numeric id of the current thread if possible on the
    // current platform. It is indended for logging purposes only.
    // Return:
    //  Numeric id of the current thread or 0 if the 
    static uint32_t GetCurrentThreadIdForLogging();

    // Get id of the current process
    // Return:
    //  Id of the current process
    static uint32_t GetCurrentProcessId();

    //
    // Processor topology
    //

    // Get number of logical processors
    static uint32_t GetLogicalCpuCount();

    // Get size of the largest cache on the processor die
    // Parameters:
    //  trueSize - true to return true cache size, false to return scaled up size based on
    //             the processor architecture
    // Return:
    //  Size of the cache
    static size_t GetLargestOnDieCacheSize(bool trueSize = true);

    // Get number of processors assigned to the current process
    // Return:
    //  The number of processors
    static uint32_t GetCurrentProcessCpuCount();

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
    static bool GetCurrentProcessAffinityMask(uintptr_t *processMask, uintptr_t *systemMask);

    //
    // Misc
    //

    // Get global memory status
    // Parameters:
    //  ms - pointer to the structure that will be filled in with the memory status
    static void GetMemoryStatus(GCMemoryStatus* ms);

    // Flush write buffers of processors that are executing threads of the current process
    static void FlushProcessWriteBuffers();

    // Break into a debugger
    static void DebugBreak();

    //
    // Time
    //

    // Get a high precision performance counter
    // Return:
    //  The counter value
    static int64_t QueryPerformanceCounter();

    // Get a frequency of the high precision performance counter
    // Return:
    //  The counter frequency
    static int64_t QueryPerformanceFrequency();

    // Get a time stamp with a low precision
    // Return:
    //  Time stamp in milliseconds
    static uint32_t GetLowPrecisionTimeStamp();

    //
    // File
    //

    // Open a file
    // Parameters:
    //  filename - name of the file to open
    //  mode     - mode to open the file in (like in the CRT fopen)
    // Return:
    //  FILE* of the opened file
    static FILE* OpenFile(const WCHAR* filename, const WCHAR* mode);
};

#endif // __GCENV_OS_H__
