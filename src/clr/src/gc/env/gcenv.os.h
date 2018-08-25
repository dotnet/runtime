// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Interface between GC and the OS specific functionality
//

#ifndef __GCENV_OS_H__
#define __GCENV_OS_H__

#ifdef Sleep
// This is a funny workaround for the fact that "common.h" defines Sleep to be
// Dont_Use_Sleep, with the hope of causing linker errors whenever someone tries to use sleep.
//
// However, GCToOSInterface defines a function called Sleep, which (due to this define) becomes
// "Dont_Use_Sleep", which the GC in turn happily uses. The symbol that GCToOSInterface actually
// exported was called "GCToOSInterface::Dont_Use_Sleep". While we progress in making the GC standalone,
// we'll need to break the dependency on common.h (the VM header) and this problem will become moot.
#undef Sleep
#endif // Sleep

#define NUMA_NODE_UNDEFINED UINT32_MAX

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

// An event is a synchronization object whose state can be set and reset
// indicating that an event has occured. It is used pervasively throughout
// the GC.
//
// Note that GCEvent deliberately leaks its contents by not having a non-trivial destructor.
// This is by design; since all uses of GCEvent have static lifetime, their destructors
// are run on process exit, potentially concurrently with other threads that may still be
// operating on the static event. To avoid these sorts of unsafety, GCEvent chooses to
// not have a destructor at all. The cost of this is leaking a small amount of memory, but
// this is not a problem since a majority of the uses of GCEvent are static. See CoreCLR#11111
// for more details on the hazards of static destructors.
class GCEvent {
private:
    class Impl;
    Impl *m_impl;

public:
    // Constructs a new uninitialized event.
    GCEvent();

    // Closes the event. Attempting to use the event past calling CloseEvent
    // is a logic error.
    void CloseEvent();

    // "Sets" the event, indicating that a particular event has occured. May
    // wake up other threads waiting on this event. Depending on whether or
    // not this event is an auto-reset event, the state of the event may
    // or may not be automatically reset after Set is called.
    void Set();

    // Resets the event, resetting it back to a non-signalled state. Auto-reset
    // events automatically reset once the event is set, while manual-reset
    // events do not reset until Reset is called. It is a no-op to call Reset
    // on an auto-reset event.
    void Reset();

    // Waits for some period of time for this event to be signalled. The
    // period of time may be infinite (if the timeout argument is INFINITE) or
    // it may be a specified period of time, in milliseconds.
    // Returns:
    //   One of three values, depending on how why this thread was awoken:
    //      WAIT_OBJECT_0 - This event was signalled and woke up this thread.
    //      WAIT_TIMEOUT  - The timeout interval expired without this event being signalled.
    //      WAIT_FAILED   - The wait failed.
    uint32_t Wait(uint32_t timeout, bool alertable);

    // Determines whether or not this event is valid.
    // Returns:
    //  true if this event is invalid (i.e. it has not yet been initialized or
    //  has already been closed), false otherwise
    bool IsValid() const
    {
        return m_impl != nullptr;
    }

    // Initializes this event to be a host-aware manual reset event with the
    // given initial state.
    // Returns:
    //   true if the initialization succeeded, false if it did not
    bool CreateManualEventNoThrow(bool initialState);

    // Initializes this event to be a host-aware auto-resetting event with the
    // given initial state.
    // Returns:
    //   true if the initialization succeeded, false if it did not
    bool CreateAutoEventNoThrow(bool initialState);

    // Initializes this event to be a host-unaware manual reset event with the
    // given initial state.
    // Returns:
    //   true if the initialization succeeded, false if it did not
    bool CreateOSManualEventNoThrow(bool initialState);

    // Initializes this event to be a host-unaware auto-resetting event with the
    // given initial state.
    // Returns:
    //   true if the initialization succeeded, false if it did not
    bool CreateOSAutoEventNoThrow(bool initialState);
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
    //  size      - size of the virtual memory range
    //  alignment - requested memory alignment
    //  flags     - flags to control special settings like write watching
    // Return:
    //  Starting virtual address of the reserved range
    // Notes:
    //  Previous uses of this API aligned the `size` parameter to the platform
    //  allocation granularity. This is not required by POSIX or Windows. Windows will
    //  round the size up to the nearest page boundary. POSIX does not specify what is done,
    //  but Linux probably also rounds up. If an implementation of GCToOSInterface needs to
    //  align to the allocation granularity, it will do so in its implementation.
    //
    //  Windows guarantees that the returned mapping will be aligned to the allocation
    //  granularity.
    static void* VirtualReserve(size_t size, size_t alignment, uint32_t flags);

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
    static bool VirtualCommit(void *address, size_t size, uint32_t node = NUMA_NODE_UNDEFINED);

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
    static uint64_t GetCurrentThreadIdForLogging();

    // Get id of the current process
    // Return:
    //  Id of the current process
    static uint32_t GetCurrentProcessId();

    //
    // Processor topology
    //

    // Get size of the on die cache per logical processor
    // Parameters:
    //  trueSize - true to return true cache size, false to return scaled up size based on
    //             the processor architecture
    // Return:
    //  Size of the cache
    static size_t GetCacheSizePerLogicalCpu(bool trueSize = true);

    // Get number of processors assigned to the current process
    // Return:
    //  The number of processors
    static uint32_t GetCurrentProcessCpuCount();

    // Sets the calling thread's affinity to only run on the processor specified
    // in the GCThreadAffinity structure.
    // Parameters:
    //  affinity - The requested affinity for the calling thread. At most one processor
    //             can be provided.
    // Return:
    //  true if setting the affinity was successful, false otherwise.
    static bool SetThreadAffinity(GCThreadAffinity* affinity);

    // Boosts the calling thread's thread priority to a level higher than the default
    // for new threads.
    // Parameters:
    //  None.
    // Return:
    //  true if the priority boost was successful, false otherwise.
    static bool BoostThreadPriority();

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
    // Global memory info
    //

    // Return the size of the user-mode portion of the virtual address space of this process.
    // Return:
    //  non zero if it has succeeded, 0 if it has failed
    static size_t GetVirtualMemoryLimit();

    // Get the physical memory that this process can use.
    // Return:
    //  non zero if it has succeeded, 0 if it has failed
    // Remarks:
    //  If a process runs with a restricted memory limit, it returns the limit. If there's no limit 
    //  specified, it returns amount of actual physical memory.
    static uint64_t GetPhysicalMemoryLimit();

    // Get memory status
    // Parameters:
    //  memory_load - A number between 0 and 100 that specifies the approximate percentage of physical memory
    //      that is in use (0 indicates no memory use and 100 indicates full memory use).
    //  available_physical - The amount of physical memory currently available, in bytes.
    //  available_page_file - The maximum amount of memory the current process can commit, in bytes.
    // Remarks:
    //  Any parameter can be null.
    static void GetMemoryStatus(uint32_t* memory_load, uint64_t* available_physical, uint64_t* available_page_file);

    // Get size of an OS memory page
    static uint32_t GetPageSize();

    //
    // Misc
    //

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

    // Gets the total number of processors on the machine, not taking
    // into account current process affinity.
    // Return:
    //  Number of processors on the machine
    static uint32_t GetTotalProcessorCount();

    // Is NUMA support available
    static bool CanEnableGCNumaAware();

    // Gets the NUMA node for the processor
    static bool GetNumaProcessorNode(PPROCESSOR_NUMBER proc_no, uint16_t *node_no);

    // Are CPU groups enabled
    static bool CanEnableGCCPUGroups();

    // Get the CPU group for the specified processor
    static void GetGroupForProcessor(uint16_t processor_number, uint16_t* group_number, uint16_t* group_processor_number);

};

#endif // __GCENV_OS_H__
