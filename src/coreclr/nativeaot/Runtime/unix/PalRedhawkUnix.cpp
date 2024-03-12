// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implementation of the Redhawk Platform Abstraction Layer (PAL) library when Unix is the platform.
//

#include <stdio.h>
#include <errno.h>
#include <cwchar>
#include <sal.h>
#include "config.h"
#include "UnixHandle.h"
#include <pthread.h>
#include "gcenv.h"
#include "gcenv.ee.h"
#include "gcconfig.h"
#include "holder.h"
#include "UnixSignals.h"
#include "UnixContext.h"
#include "HardwareExceptions.h"
#include "PalCreateDump.h"
#include "cgroupcpu.h"
#include "threadstore.h"
#include "thread.h"
#include "threadstore.inl"

#define _T(s) s
#include "RhConfig.h"

#include <unistd.h>
#include <sched.h>
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/syscall.h>
#include <dlfcn.h>
#include <dirent.h>
#include <string.h>
#include <ctype.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/time.h>
#include <cstdarg>
#include <signal.h>

#if HAVE_PTHREAD_GETTHREADID_NP
#include <pthread_np.h>
#endif

#if HAVE_LWP_SELF
#include <lwp.h>
#endif

#if HAVE_CLOCK_GETTIME_NSEC_NP
#include <time.h>
#endif

#ifdef TARGET_APPLE
#include <mach/mach.h>
#endif

using std::nullptr_t;

#define PalRaiseFailFastException RaiseFailFastException

#define INVALID_HANDLE_VALUE    ((HANDLE)(intptr_t)-1)

#define PAGE_NOACCESS           0x01
#define PAGE_READWRITE          0x04
#define PAGE_EXECUTE_READ       0x20
#define PAGE_EXECUTE_READWRITE  0x40

#define WAIT_OBJECT_0           0
#define WAIT_TIMEOUT            258
#define WAIT_FAILED             0xFFFFFFFF

static const int tccSecondsToMilliSeconds = 1000;
static const int tccSecondsToMicroSeconds = 1000000;
static const int tccSecondsToNanoSeconds = 1000000000;
static const int tccMilliSecondsToMicroSeconds = 1000;
static const int tccMilliSecondsToNanoSeconds = 1000000;
static const int tccMicroSecondsToNanoSeconds = 1000;

extern "C" void RaiseFailFastException(PEXCEPTION_RECORD arg1, PCONTEXT arg2, uint32_t arg3)
{
    // Causes creation of a crash dump if enabled
    PalCreateCrashDumpIfEnabled();

    // Aborts the process
    abort();
}

static void TimeSpecAdd(timespec* time, uint32_t milliseconds)
{
    uint64_t nsec = time->tv_nsec + (uint64_t)milliseconds * tccMilliSecondsToNanoSeconds;
    if (nsec >= tccSecondsToNanoSeconds)
    {
        time->tv_sec += nsec / tccSecondsToNanoSeconds;
        nsec %= tccSecondsToNanoSeconds;
    }

    time->tv_nsec = nsec;
}

// Convert nanoseconds to the timespec structure
// Parameters:
//  nanoseconds - time in nanoseconds to convert
//  t           - the target timespec structure
static void NanosecondsToTimeSpec(uint64_t nanoseconds, timespec* t)
{
    t->tv_sec = nanoseconds / tccSecondsToNanoSeconds;
    t->tv_nsec = nanoseconds % tccSecondsToNanoSeconds;
}

void ReleaseCondAttr(pthread_condattr_t* condAttr)
{
    int st = pthread_condattr_destroy(condAttr);
    ASSERT_MSG(st == 0, "Failed to destroy pthread_condattr_t object");
}

class PthreadCondAttrHolder : public Wrapper<pthread_condattr_t*, DoNothing, ReleaseCondAttr, nullptr>
{
public:
    PthreadCondAttrHolder(pthread_condattr_t* attrs)
    : Wrapper<pthread_condattr_t*, DoNothing, ReleaseCondAttr, nullptr>(attrs)
    {
    }
};

class UnixEvent
{
    pthread_cond_t m_condition;
    pthread_mutex_t m_mutex;
    bool m_manualReset;
    bool m_state;
    bool m_isValid;

public:

    UnixEvent(bool manualReset, bool initialState)
    : m_manualReset(manualReset),
      m_state(initialState),
      m_isValid(false)
    {
    }

    bool Initialize()
    {
        pthread_condattr_t attrs;
        int st = pthread_condattr_init(&attrs);
        if (st != 0)
        {
            ASSERT_UNCONDITIONALLY("Failed to initialize UnixEvent condition attribute");
            return false;
        }

        PthreadCondAttrHolder attrsHolder(&attrs);

#if HAVE_PTHREAD_CONDATTR_SETCLOCK && !HAVE_CLOCK_GETTIME_NSEC_NP
        // Ensure that the pthread_cond_timedwait will use CLOCK_MONOTONIC
        st = pthread_condattr_setclock(&attrs, CLOCK_MONOTONIC);
        if (st != 0)
        {
            ASSERT_UNCONDITIONALLY("Failed to set UnixEvent condition variable wait clock");
            return false;
        }
#endif // HAVE_PTHREAD_CONDATTR_SETCLOCK && !HAVE_CLOCK_GETTIME_NSEC_NP

        st = pthread_mutex_init(&m_mutex, NULL);
        if (st != 0)
        {
            ASSERT_UNCONDITIONALLY("Failed to initialize UnixEvent mutex");
            return false;
        }

        st = pthread_cond_init(&m_condition, &attrs);
        if (st != 0)
        {
            ASSERT_UNCONDITIONALLY("Failed to initialize UnixEvent condition variable");

            st = pthread_mutex_destroy(&m_mutex);
            ASSERT_MSG(st == 0, "Failed to destroy UnixEvent mutex");
            return false;
        }

        m_isValid = true;

        return true;
    }

    bool Destroy()
    {
        bool success = true;

        if (m_isValid)
        {
            int st = pthread_mutex_destroy(&m_mutex);
            ASSERT_MSG(st == 0, "Failed to destroy UnixEvent mutex");
            success = success && (st == 0);

            st = pthread_cond_destroy(&m_condition);
            ASSERT_MSG(st == 0, "Failed to destroy UnixEvent condition variable");
            success = success && (st == 0);
        }

        return success;
    }

    uint32_t Wait(uint32_t milliseconds)
    {
        timespec endTime;
#if HAVE_CLOCK_GETTIME_NSEC_NP
        uint64_t endNanoseconds;
        if (milliseconds != INFINITE)
        {
            uint64_t nanoseconds = (uint64_t)milliseconds * tccMilliSecondsToNanoSeconds;
            NanosecondsToTimeSpec(nanoseconds, &endTime);
            endNanoseconds = clock_gettime_nsec_np(CLOCK_UPTIME_RAW) + nanoseconds;
        }
#elif HAVE_PTHREAD_CONDATTR_SETCLOCK
        if (milliseconds != INFINITE)
        {
            clock_gettime(CLOCK_MONOTONIC, &endTime);
            TimeSpecAdd(&endTime, milliseconds);
        }
#else
#error "Don't know how to perform timed wait on this platform"
#endif

        int st = 0;

        pthread_mutex_lock(&m_mutex);
        while (!m_state)
        {
            if (milliseconds == INFINITE)
            {
                st = pthread_cond_wait(&m_condition, &m_mutex);
            }
            else
            {
#if HAVE_CLOCK_GETTIME_NSEC_NP
                // Since OSX doesn't support CLOCK_MONOTONIC, we use relative variant of the
                // timed wait and we need to handle spurious wakeups properly.
                st = pthread_cond_timedwait_relative_np(&m_condition, &m_mutex, &endTime);
                if ((st == 0) && !m_state)
                {
                    uint64_t currentNanoseconds = clock_gettime_nsec_np(CLOCK_UPTIME_RAW);
                    if (currentNanoseconds < endNanoseconds)
                    {
                        // The wake up was spurious, recalculate the relative endTime
                        uint64_t remainingNanoseconds = (endNanoseconds - currentNanoseconds);
                        NanosecondsToTimeSpec(remainingNanoseconds, &endTime);
                    }
                    else
                    {
                        // Although the timed wait didn't report a timeout, time calculated from the
                        // mach time shows we have already reached the end time. It can happen if
                        // the wait was spuriously woken up right before the timeout.
                        st = ETIMEDOUT;
                    }
                }
#else // HAVE_CLOCK_GETTIME_NSEC_NP
                st = pthread_cond_timedwait(&m_condition, &m_mutex, &endTime);
#endif // HAVE_CLOCK_GETTIME_NSEC_NP
            }

            if (st != 0)
            {
                // wait failed or timed out
                break;
            }
        }

        if ((st == 0) && !m_manualReset)
        {
            // Clear the state for auto-reset events so that only one waiter gets released
            m_state = false;
        }

        pthread_mutex_unlock(&m_mutex);

        uint32_t waitStatus;

        if (st == 0)
        {
            waitStatus = WAIT_OBJECT_0;
        }
        else if (st == ETIMEDOUT)
        {
            waitStatus = WAIT_TIMEOUT;
        }
        else
        {
            waitStatus = WAIT_FAILED;
        }

        return waitStatus;
    }

    void Set()
    {
        pthread_mutex_lock(&m_mutex);
        m_state = true;
        // Unblock all threads waiting for the condition variable
        pthread_cond_broadcast(&m_condition);
        pthread_mutex_unlock(&m_mutex);
    }

    void Reset()
    {
        pthread_mutex_lock(&m_mutex);
        m_state = false;
        pthread_mutex_unlock(&m_mutex);
    }
};

class EventUnixHandle : public UnixHandle<UnixHandleType::Event, UnixEvent>
{
public:
    EventUnixHandle(UnixEvent event)
    : UnixHandle<UnixHandleType::Event, UnixEvent>(event)
    {
    }

    virtual bool Destroy()
    {
        return m_object.Destroy();
    }
};

typedef UnixHandle<UnixHandleType::Thread, pthread_t> ThreadUnixHandle;

// This functions configures behavior of the signals that are not
// related to hardware exception handling.
void ConfigureSignals()
{
    // The default action for SIGPIPE is process termination.
    // Since SIGPIPE can be signaled when trying to write on a socket for which
    // the connection has been dropped, we need to tell the system we want
    // to ignore this signal.
    // Instead of terminating the process, the system call which would had
    // issued a SIGPIPE will, instead, report an error and set errno to EPIPE.
    signal(SIGPIPE, SIG_IGN);
}

void InitializeCurrentProcessCpuCount()
{
    uint32_t count;

    // If the configuration value has been set, it takes precedence. Otherwise, take into account
    // process affinity and CPU quota limit.

    const unsigned int MAX_PROCESSOR_COUNT = 0xffff;
    uint64_t configValue;

    if (g_pRhConfig->ReadConfigValue("PROCESSOR_COUNT", &configValue, true /* decimal */) &&
        0 < configValue && configValue <= MAX_PROCESSOR_COUNT)
    {
        count = configValue;
    }
    else
    {
#if HAVE_SCHED_GETAFFINITY

        cpu_set_t cpuSet;
        int st = sched_getaffinity(getpid(), sizeof(cpu_set_t), &cpuSet);
        if (st != 0)
        {
            _ASSERTE(!"sched_getaffinity failed");
        }

        count = CPU_COUNT(&cpuSet);
#else // HAVE_SCHED_GETAFFINITY
        count = GCToOSInterface::GetTotalProcessorCount();
#endif // HAVE_SCHED_GETAFFINITY

        uint32_t cpuLimit;
        if (GetCpuLimit(&cpuLimit) && cpuLimit < count)
            count = cpuLimit;
    }

    _ASSERTE(count > 0);
    g_RhNumberOfProcessors = count;
}

static uint32_t g_RhPageSize;

void InitializeOsPageSize()
{
    g_RhPageSize = (uint32_t)sysconf(_SC_PAGE_SIZE);

#if defined(HOST_AMD64)
    ASSERT(g_RhPageSize == 0x1000);
#elif defined(HOST_APPLE)
    ASSERT(g_RhPageSize == 0x4000);
#endif
}

REDHAWK_PALEXPORT uint32_t REDHAWK_PALAPI PalGetOsPageSize()
{
    return g_RhPageSize;
}

#if defined(TARGET_LINUX) || defined(TARGET_ANDROID)
static pthread_key_t key;
#endif

// The Redhawk PAL must be initialized before any of its exports can be called. Returns true for a successful
// initialization and false on failure.
REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalInit()
{
#ifndef USE_PORTABLE_HELPERS
    if (!InitializeHardwareExceptionHandling())
    {
        return false;
    }
#endif // !USE_PORTABLE_HELPERS

    ConfigureSignals();

    if (!PalCreateDumpInitialize())
    {
        return false;
    }

    GCConfig::Initialize();

    if (!GCToOSInterface::Initialize())
    {
        return false;
    }

    InitializeCpuCGroup();

    InitializeCurrentProcessCpuCount();

    InitializeOsPageSize();

#if defined(TARGET_LINUX) || defined(TARGET_ANDROID)
    if (pthread_key_create(&key, RuntimeThreadShutdown) != 0)
    {
        return false;
    }
#endif

    return true;
}

#if !defined(TARGET_LINUX) && !defined(TARGET_ANDROID)
struct TlsDestructionMonitor
{
    void* m_thread = nullptr;

    void SetThread(void* thread)
    {
        m_thread = thread;
    }

    ~TlsDestructionMonitor()
    {
        if (m_thread != nullptr)
        {
            RuntimeThreadShutdown(m_thread);
        }
    }
};

// This thread local object is used to detect thread shutdown. Its destructor
// is called when a thread is being shut down.
thread_local TlsDestructionMonitor tls_destructionMonitor;
#endif

// This thread local variable is used for delegate marshalling
DECLSPEC_THREAD intptr_t tls_thunkData;

#ifdef FEATURE_EMULATED_TLS
EXTERN_C intptr_t* RhpGetThunkData()
{
    return &tls_thunkData;
}

EXTERN_C intptr_t RhGetCurrentThunkContext()
{
    return tls_thunkData;
}
#endif //FEATURE_EMULATED_TLS

// Register the thread with OS to be notified when thread is about to be destroyed
// It fails fast if a different thread was already registered.
// Parameters:
//  thread        - thread to attach
extern "C" void PalAttachThread(void* thread)
{
#if defined(TARGET_LINUX) || defined(TARGET_ANDROID)
    if (pthread_setspecific(key, thread) != 0)
    {
        _ASSERTE(!"pthread_setspecific failed");
        RhFailFast();
    }
#else
    tls_destructionMonitor.SetThread(thread);
#endif
}

// Detach thread from OS notifications.
// Parameters:
//  thread        - thread to detach
// Return:
//  true if the thread was detached, false if there was no attached thread
extern "C" bool PalDetachThread(void* thread)
{
    UNREFERENCED_PARAMETER(thread);
    return true;
}

#if !defined(USE_PORTABLE_HELPERS) && !defined(FEATURE_RX_THUNKS)

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalAllocateThunksFromTemplate(HANDLE hTemplateModule, uint32_t templateRva, size_t templateSize, void** newThunksOut)
{
#ifdef TARGET_APPLE
    vm_address_t addr, taddr;
    vm_prot_t prot, max_prot;
    kern_return_t ret;

    // Allocate two contiguous ranges of memory: the first range will contain the trampolines
    // and the second range will contain their data.
    do
    {
        ret = vm_allocate(mach_task_self(), &addr, templateSize * 2, VM_FLAGS_ANYWHERE);
    } while (ret == KERN_ABORTED);

    if (ret != KERN_SUCCESS)
    {
        return UInt32_FALSE;
    }

    do
    {
        ret = vm_remap(
            mach_task_self(), &addr, templateSize, 0, VM_FLAGS_FIXED | VM_FLAGS_OVERWRITE,
            mach_task_self(), ((vm_address_t)hTemplateModule + templateRva), FALSE, &prot, &max_prot, VM_INHERIT_SHARE);
    } while (ret == KERN_ABORTED);

    if (ret != KERN_SUCCESS)
    {
        do
        {
            ret = vm_deallocate(mach_task_self(), addr, templateSize * 2);
        } while (ret == KERN_ABORTED);

        return UInt32_FALSE;
    }

    *newThunksOut = (void*)addr;

    return UInt32_TRUE;
#else
    PORTABILITY_ASSERT("UNIXTODO: Implement this function");
#endif
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalFreeThunksFromTemplate(void *pBaseAddress, size_t templateSize)
{
#ifdef TARGET_APPLE
    kern_return_t ret;

    do
    {
        ret = vm_deallocate(mach_task_self(), (vm_address_t)pBaseAddress, templateSize * 2);
    } while (ret == KERN_ABORTED);

    return ret == KERN_SUCCESS ? UInt32_TRUE : UInt32_FALSE;
#else
    PORTABILITY_ASSERT("UNIXTODO: Implement this function");
#endif
}
#endif // !USE_PORTABLE_HELPERS && !FEATURE_RX_THUNKS

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalMarkThunksAsValidCallTargets(
    void *virtualAddress,
    int thunkSize,
    int thunksPerBlock,
    int thunkBlockSize,
    int thunkBlocksPerMapping)
{
    int ret = mprotect(
        (void*)((uintptr_t)virtualAddress + (thunkBlocksPerMapping * OS_PAGE_SIZE)),
        thunkBlocksPerMapping * OS_PAGE_SIZE,
        PROT_READ | PROT_WRITE);
    return ret == 0 ? UInt32_TRUE : UInt32_FALSE;
}

REDHAWK_PALEXPORT void REDHAWK_PALAPI PalSleep(uint32_t milliseconds)
{
#if HAVE_CLOCK_NANOSLEEP
    timespec endTime;
    clock_gettime(CLOCK_MONOTONIC, &endTime);
    TimeSpecAdd(&endTime, milliseconds);
    while (clock_nanosleep(CLOCK_MONOTONIC, TIMER_ABSTIME, &endTime, NULL) == EINTR)
    {
    }
#else // HAVE_CLOCK_NANOSLEEP
    timespec requested;
    requested.tv_sec = milliseconds / tccSecondsToMilliSeconds;
    requested.tv_nsec = (milliseconds - requested.tv_sec * tccSecondsToMilliSeconds) * tccMilliSecondsToNanoSeconds;

    timespec remaining;
    while (nanosleep(&requested, &remaining) == EINTR)
    {
        requested = remaining;
    }
#endif // HAVE_CLOCK_NANOSLEEP
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI __stdcall PalSwitchToThread()
{
    // sched_yield yields to another thread in the current process.
    sched_yield();

    // The return value of sched_yield indicates the success of the call and does not tell whether a context switch happened.
    // On Linux sched_yield is documented as never failing.
    // Since we do not know if there was a context switch, we will just return `false`.
    return false;
}

extern "C" UInt32_BOOL CloseHandle(HANDLE handle)
{
    if ((handle == NULL) || (handle == INVALID_HANDLE_VALUE))
    {
        return UInt32_FALSE;
    }

    UnixHandleBase* handleBase = (UnixHandleBase*)handle;

    bool success = handleBase->Destroy();

    delete handleBase;

    return success ? UInt32_TRUE : UInt32_FALSE;
}

REDHAWK_PALEXPORT HANDLE REDHAWK_PALAPI PalCreateEventW(_In_opt_ LPSECURITY_ATTRIBUTES pEventAttributes, UInt32_BOOL manualReset, UInt32_BOOL initialState, _In_opt_z_ const WCHAR* pName)
{
    UnixEvent event = UnixEvent(manualReset, initialState);
    if (!event.Initialize())
    {
        return INVALID_HANDLE_VALUE;
    }

    EventUnixHandle* handle = new (nothrow) EventUnixHandle(event);

    if (handle == NULL)
    {
        return INVALID_HANDLE_VALUE;
    }

    return handle;
}

typedef uint32_t(__stdcall *BackgroundCallback)(_In_opt_ void* pCallbackContext);

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalStartBackgroundWork(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext, UInt32_BOOL highPriority)
{
#ifdef HOST_WASM
    // No threads, so we can't start one
    ASSERT(false);
#endif // HOST_WASM
    pthread_attr_t attrs;

    int st = pthread_attr_init(&attrs);
    ASSERT(st == 0);

    static const int NormalPriority = 0;
    static const int HighestPriority = -20;

    // TODO: Figure out which scheduler to use, the default one doesn't seem to
    // support per thread priorities.
#if 0
    sched_param params;
    memset(&params, 0, sizeof(params));

    params.sched_priority = highPriority ? HighestPriority : NormalPriority;

    // Set the priority of the thread
    st = pthread_attr_setschedparam(&attrs, &params);
    ASSERT(st == 0);
#endif
    // Create the thread as detached, that means not joinable
    st = pthread_attr_setdetachstate(&attrs, PTHREAD_CREATE_DETACHED);
    ASSERT(st == 0);

    pthread_t threadId;
    st = pthread_create(&threadId, &attrs, (void *(*)(void*))callback, pCallbackContext);

    int st2 = pthread_attr_destroy(&attrs);
    ASSERT(st2 == 0);

    return st == 0;
}

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalStartBackgroundGCThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext)
{
    return PalStartBackgroundWork(callback, pCallbackContext, UInt32_FALSE);
}

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalStartFinalizerThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext)
{
#ifdef HOST_WASM
    // WASMTODO: No threads so we can't start the finalizer thread
    return true;
#else // HOST_WASM
    return PalStartBackgroundWork(callback, pCallbackContext, UInt32_TRUE);
#endif // HOST_WASM
}

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalStartEventPipeHelperThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext)
{
    return PalStartBackgroundWork(callback, pCallbackContext, UInt32_FALSE);
}

// Returns a 64-bit tick count with a millisecond resolution. It tries its best
// to return monotonically increasing counts and avoid being affected by changes
// to the system clock (either due to drift or due to explicit changes to system
// time).
REDHAWK_PALEXPORT uint64_t REDHAWK_PALAPI PalGetTickCount64()
{
    return GCToOSInterface::GetLowPrecisionTimeStamp();
}

REDHAWK_PALEXPORT HANDLE REDHAWK_PALAPI PalGetModuleHandleFromPointer(_In_ void* pointer)
{
    HANDLE moduleHandle = NULL;

    // Emscripten's implementation of dladdr corrupts memory,
    // but always returns 0 for the module handle, so just skip the call
#if !defined(HOST_WASM)
    Dl_info info;
    int st = dladdr(pointer, &info);
    if (st != 0)
    {
        moduleHandle = info.dli_fbase;
    }
#endif //!defined(HOST_WASM)

    return moduleHandle;
}

REDHAWK_PALEXPORT void PalPrintFatalError(const char* message)
{
    // Write the message using lowest-level OS API available. This is used to print the stack overflow
    // message, so there is not much that can be done here.
    // write() has __attribute__((warn_unused_result)) in glibc, for which gcc 11+ issue `-Wunused-result` even with `(void)write(..)`,
    // so we use additional NOT(!) operator to force unused-result suppression.
    (void)!write(STDERR_FILENO, message, strlen(message));
}

REDHAWK_PALEXPORT char* PalCopyTCharAsChar(const TCHAR* toCopy)
{
    NewArrayHolder<char> copy {new (nothrow) char[strlen(toCopy) + 1]};
    strcpy(copy, toCopy);
    return copy.Extract();
}

REDHAWK_PALEXPORT HANDLE PalLoadLibrary(const char* moduleName)
{
    return dlopen(moduleName, RTLD_LAZY);
}

REDHAWK_PALEXPORT void* PalGetProcAddress(HANDLE module, const char* functionName)
{
    return dlsym(module, functionName);
}

static int W32toUnixAccessControl(uint32_t flProtect)
{
    int prot = 0;

    switch (flProtect & 0xff)
    {
    case PAGE_NOACCESS:
        prot = PROT_NONE;
        break;
    case PAGE_READWRITE:
        prot = PROT_READ | PROT_WRITE;
        break;
    case PAGE_EXECUTE_READ:
        prot = PROT_READ | PROT_EXEC;
        break;
    case PAGE_EXECUTE_READWRITE:
        prot = PROT_READ | PROT_WRITE | PROT_EXEC;
        break;
    case PAGE_READONLY:
        prot = PROT_READ;
        break;
    default:
        ASSERT(false);
        break;
    }
    return prot;
}

REDHAWK_PALEXPORT _Ret_maybenull_ _Post_writable_byte_size_(size) void* REDHAWK_PALAPI PalVirtualAlloc(size_t size, uint32_t protect)
{
    int unixProtect = W32toUnixAccessControl(protect);

    int flags = MAP_ANON | MAP_PRIVATE;

#if defined(HOST_APPLE) && defined(HOST_ARM64)
    if (unixProtect & PROT_EXEC)
    {
        flags |= MAP_JIT;
    }
#endif

    return mmap(NULL, size, unixProtect, flags, -1, 0);
}

REDHAWK_PALEXPORT void REDHAWK_PALAPI PalVirtualFree(_In_ void* pAddress, size_t size)
{
    munmap(pAddress, size);
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalVirtualProtect(_In_ void* pAddress, size_t size, uint32_t protect)
{
    int unixProtect = W32toUnixAccessControl(protect);

    // mprotect expects the address to be page-aligned
    uint8_t* pPageStart = ALIGN_DOWN((uint8_t*)pAddress, OS_PAGE_SIZE);
    size_t memSize = ALIGN_UP((uint8_t*)pAddress + size, OS_PAGE_SIZE) - pPageStart;

    return mprotect(pPageStart, memSize, unixProtect) == 0;
}

#if (defined(HOST_MACCATALYST) || defined(HOST_IOS) || defined(HOST_TVOS)) && defined(HOST_ARM64)
extern "C" void sys_icache_invalidate(const void* start, size_t len);
#endif

REDHAWK_PALEXPORT void PalFlushInstructionCache(_In_ void* pAddress, size_t size)
{
#if defined(__linux__) && defined(HOST_ARM)
    // On Linux/arm (at least on 3.10) we found that there is a problem with __do_cache_op (arch/arm/kernel/traps.c)
    // implementing cacheflush syscall. cacheflush flushes only the first page in range [pAddress, pAddress + size)
    // and leaves other pages in undefined state which causes random tests failures (often due to SIGSEGV) with no particular pattern.
    //
    // As a workaround, we call __builtin___clear_cache on each page separately.

    uint8_t* begin = (uint8_t*)pAddress;
    uint8_t* end = begin + size;

    while (begin < end)
    {
        uint8_t* endOrNextPageBegin = ALIGN_UP(begin + 1, OS_PAGE_SIZE);
        if (endOrNextPageBegin > end)
            endOrNextPageBegin = end;

        __builtin___clear_cache((char *)begin, (char *)endOrNextPageBegin);
        begin = endOrNextPageBegin;
    }
#elif (defined(HOST_MACCATALYST) || defined(HOST_IOS) || defined(HOST_TVOS)) && defined(HOST_ARM64)
    sys_icache_invalidate (pAddress, size);
#else
    __builtin___clear_cache((char *)pAddress, (char *)pAddress + size);
#endif
}

extern "C" HANDLE GetCurrentProcess()
{
    return (HANDLE)-1;
}

extern "C" uint32_t GetCurrentProcessId()
{
    return getpid();
}

extern "C" HANDLE GetCurrentThread()
{
    return (HANDLE)-2;
}

extern "C" UInt32_BOOL DuplicateHandle(
    HANDLE hSourceProcessHandle,
    HANDLE hSourceHandle,
    HANDLE hTargetProcessHandle,
    HANDLE * lpTargetHandle,
    uint32_t dwDesiredAccess,
    UInt32_BOOL bInheritHandle,
    uint32_t dwOptions)
{
    // We can only duplicate the current thread handle. That is all that the MRT uses.
    ASSERT(hSourceProcessHandle == GetCurrentProcess());
    ASSERT(hTargetProcessHandle == GetCurrentProcess());
    ASSERT(hSourceHandle == GetCurrentThread());
    *lpTargetHandle = new (nothrow) ThreadUnixHandle(pthread_self());

    return lpTargetHandle != nullptr;
}

extern "C" UInt32_BOOL InitializeCriticalSection(CRITICAL_SECTION * lpCriticalSection)
{
    pthread_mutexattr_t mutexAttributes;
    int st = pthread_mutexattr_init(&mutexAttributes);
    if (st != 0)
    {
        return false;
    }

    st = pthread_mutexattr_settype(&mutexAttributes, PTHREAD_MUTEX_RECURSIVE);
    if (st == 0)
    {
        st = pthread_mutex_init(&lpCriticalSection->mutex, &mutexAttributes);
    }

    pthread_mutexattr_destroy(&mutexAttributes);

    return (st == 0);
}

extern "C" UInt32_BOOL InitializeCriticalSectionEx(CRITICAL_SECTION * lpCriticalSection, uint32_t arg2, uint32_t arg3)
{
    return InitializeCriticalSection(lpCriticalSection);
}


extern "C" void DeleteCriticalSection(CRITICAL_SECTION * lpCriticalSection)
{
    pthread_mutex_destroy(&lpCriticalSection->mutex);
}

extern "C" void EnterCriticalSection(CRITICAL_SECTION * lpCriticalSection)
{
    pthread_mutex_lock(&lpCriticalSection->mutex);;
}

extern "C" void LeaveCriticalSection(CRITICAL_SECTION * lpCriticalSection)
{
    pthread_mutex_unlock(&lpCriticalSection->mutex);
}

extern "C" UInt32_BOOL IsDebuggerPresent()
{
#ifdef HOST_WASM
    // For now always true since the browser will handle it in case of WASM.
    return UInt32_TRUE;
#else
    // UNIXTODO: Implement this function
    return UInt32_FALSE;
#endif
}

extern "C" UInt32_BOOL SetEvent(HANDLE event)
{
    EventUnixHandle* unixHandle = (EventUnixHandle*)event;
    unixHandle->GetObject()->Set();

    return UInt32_TRUE;
}

extern "C" UInt32_BOOL ResetEvent(HANDLE event)
{
    EventUnixHandle* unixHandle = (EventUnixHandle*)event;
    unixHandle->GetObject()->Reset();

    return UInt32_TRUE;
}

extern "C" uint32_t GetEnvironmentVariableA(const char * name, char * buffer, uint32_t size)
{
    const char* value = getenv(name);
    if (value == NULL)
    {
        return 0;
    }

    size_t valueLen = strlen(value);
    if (valueLen < size)
    {
        strcpy(buffer, value);
        return valueLen;
    }

    // return required size including the null character or 0 if the size doesn't fit into uint32_t
    return (valueLen < UINT32_MAX) ? (valueLen + 1) : 0;
}

extern "C" uint16_t RtlCaptureStackBackTrace(uint32_t arg1, uint32_t arg2, void* arg3, uint32_t* arg4)
{
    // UNIXTODO: Implement this function
    return 0;
}

static PalHijackCallback g_pHijackCallback;
static struct sigaction g_previousActivationHandler;

static void ActivationHandler(int code, siginfo_t* siginfo, void* context)
{
    // Only accept activations from the current process
    if (g_pHijackCallback != NULL && (siginfo->si_pid == getpid()
#ifdef HOST_APPLE
        // On Apple platforms si_pid is sometimes 0. It was confirmed by Apple to be expected, as the si_pid is tracked at the process level. So when multiple
        // signals are in flight in the same process at the same time, it may be overwritten / zeroed.
        || siginfo->si_pid == 0
#endif
        ))
    {
        // Make sure that errno is not modified
        int savedErrNo = errno;
        g_pHijackCallback((NATIVE_CONTEXT*)context, NULL);
        errno = savedErrNo;
    }

    Thread* pThread = ThreadStore::GetCurrentThreadIfAvailable();
    if (pThread)
    {
        pThread->SetActivationPending(false);
    }

    // Call the original handler when it is not ignored or default (terminate).
    if (g_previousActivationHandler.sa_flags & SA_SIGINFO)
    {
        _ASSERTE(g_previousActivationHandler.sa_sigaction != NULL);
        g_previousActivationHandler.sa_sigaction(code, siginfo, context);
    }
    else
    {
        if (g_previousActivationHandler.sa_handler != SIG_IGN &&
            g_previousActivationHandler.sa_handler != SIG_DFL)
        {
            _ASSERTE(g_previousActivationHandler.sa_handler != NULL);
            g_previousActivationHandler.sa_handler(code);
        }
    }
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalRegisterHijackCallback(_In_ PalHijackCallback callback)
{
    ASSERT(g_pHijackCallback == NULL);
    g_pHijackCallback = callback;

    return AddSignalHandler(INJECT_ACTIVATION_SIGNAL, ActivationHandler, &g_previousActivationHandler);
}

REDHAWK_PALEXPORT void REDHAWK_PALAPI PalHijack(HANDLE hThread, _In_opt_ void* pThreadToHijack)
{
    ThreadUnixHandle* threadHandle = (ThreadUnixHandle*)hThread;
    Thread* pThread = (Thread*)pThreadToHijack;
    pThread->SetActivationPending(true);

    int status = pthread_kill(*threadHandle->GetObject(), INJECT_ACTIVATION_SIGNAL);

    // We can get EAGAIN when printing stack overflow stack trace and when other threads hit
    // stack overflow too. Those are held in the sigsegv_handler with blocked signals until
    // the process exits.
    // ESRCH may happen on some OSes when the thread is exiting.
    // The thread should leave cooperative mode, but we could have seen it in its earlier state.
    if ((status == EAGAIN)
     || (status == ESRCH)
#ifdef __APPLE__
        // On Apple, pthread_kill is not allowed to be sent to dispatch queue threads
     || (status == ENOTSUP)
#endif
       )
    {
        pThread->SetActivationPending(false);
        return;
    }

    if (status != 0)
    {
        // Causes creation of a crash dump if enabled
        PalCreateCrashDumpIfEnabled();

        // Failure to send the signal is fatal. There are only two cases when sending
        // the signal can fail. First, if the signal ID is invalid and second,
        // if the thread doesn't exist anymore.
        abort();
    }
}

extern "C" uint32_t WaitForSingleObjectEx(HANDLE handle, uint32_t milliseconds, UInt32_BOOL alertable)
{
    // The handle can only represent an event here
    // TODO: encapsulate this stuff
    UnixHandleBase* handleBase = (UnixHandleBase*)handle;
    ASSERT(handleBase->GetType() == UnixHandleType::Event);
    EventUnixHandle* unixHandle = (EventUnixHandle*)handleBase;

    return unixHandle->GetObject()->Wait(milliseconds);
}

REDHAWK_PALEXPORT uint32_t REDHAWK_PALAPI PalCompatibleWaitAny(UInt32_BOOL alertable, uint32_t timeout, uint32_t handleCount, HANDLE* pHandles, UInt32_BOOL allowReentrantWait)
{
    // Only a single handle wait for event is supported
    ASSERT(handleCount == 1);

    return WaitForSingleObjectEx(pHandles[0], timeout, alertable);
}

#if !__has_builtin(_mm_pause)
extern "C" void _mm_pause()
// Defined for implementing PalYieldProcessor in PalRedhawk.h
{
#if defined(HOST_AMD64) || defined(HOST_X86)
  __asm__ volatile ("pause");
#endif
}
#endif

extern "C" int32_t _stricmp(const char *string1, const char *string2)
{
    return strcasecmp(string1, string2);
}

REDHAWK_PALIMPORT void REDHAWK_PALAPI PopulateControlSegmentRegisters(CONTEXT* pContext)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // Currently the CONTEXT is only used on Windows for RaiseFailFastException.
    // So we punt on filling in SegCs and SegSs for now.
#endif
}

uint32_t g_RhNumberOfProcessors;

REDHAWK_PALEXPORT int32_t PalGetProcessCpuCount()
{
    ASSERT(g_RhNumberOfProcessors > 0);
    return g_RhNumberOfProcessors;
}

__thread void* pStackHighOut = NULL;
__thread void* pStackLowOut = NULL;

// Retrieves the entire range of memory dedicated to the calling thread's stack.  This does
// not get the current dynamic bounds of the stack, which can be significantly smaller than
// the maximum bounds.
REDHAWK_PALEXPORT bool PalGetMaximumStackBounds(_Out_ void** ppStackLowOut, _Out_ void** ppStackHighOut)
{
    if (pStackHighOut == NULL)
    {
#ifdef __APPLE__
        // This is a Mac specific method
        pStackHighOut = pthread_get_stackaddr_np(pthread_self());
        pStackLowOut = ((uint8_t *)pStackHighOut - pthread_get_stacksize_np(pthread_self()));
#else // __APPLE__
        pthread_attr_t attr;
        size_t stackSize;
        int status;

        pthread_t thread = pthread_self();

        status = pthread_attr_init(&attr);
        ASSERT_MSG(status == 0, "pthread_attr_init call failed");

#if HAVE_PTHREAD_ATTR_GET_NP
        status = pthread_attr_get_np(thread, &attr);
#elif HAVE_PTHREAD_GETATTR_NP
        status = pthread_getattr_np(thread, &attr);
#else
#error Dont know how to get thread attributes on this platform!
#endif
        ASSERT_MSG(status == 0, "pthread_getattr_np call failed");

        status = pthread_attr_getstack(&attr, &pStackLowOut, &stackSize);
        ASSERT_MSG(status == 0, "pthread_attr_getstack call failed");

        status = pthread_attr_destroy(&attr);
        ASSERT_MSG(status == 0, "pthread_attr_destroy call failed");

        pStackHighOut = (uint8_t*)pStackLowOut + stackSize;
#endif // __APPLE__
    }

    *ppStackLowOut = pStackLowOut;
    *ppStackHighOut = pStackHighOut;

    return true;
}

// retrieves the full path to the specified module, if moduleBase is NULL retreieves the full path to the
// executable module of the current process.
//
// Return value:  number of characters in name string
//
REDHAWK_PALEXPORT int32_t PalGetModuleFileName(_Out_ const TCHAR** pModuleNameOut, HANDLE moduleBase)
{
#if defined(HOST_WASM)
    // Emscripten's implementation of dladdr corrupts memory and doesn't have the real name, so make up a name instead
    const TCHAR* wasmModuleName = "WebAssemblyModule";
    *pModuleNameOut = wasmModuleName;
    return strlen(wasmModuleName);
#else // HOST_WASM
    Dl_info dl;
    if (dladdr(moduleBase, &dl) == 0)
    {
        *pModuleNameOut = NULL;
        return 0;
    }

    *pModuleNameOut = dl.dli_fname;
    return strlen(dl.dli_fname);
#endif // defined(HOST_WASM)
}

extern "C" void FlushProcessWriteBuffers()
{
    GCToOSInterface::FlushProcessWriteBuffers();
}

static const int64_t SECS_BETWEEN_1601_AND_1970_EPOCHS = 11644473600LL;
static const int64_t SECS_TO_100NS = 10000000; /* 10^7 */

extern "C" void GetSystemTimeAsFileTime(FILETIME *lpSystemTimeAsFileTime)
{
    struct timeval time = { 0 };
    gettimeofday(&time, NULL);

    int64_t result = ((int64_t)time.tv_sec + SECS_BETWEEN_1601_AND_1970_EPOCHS) * SECS_TO_100NS +
        (time.tv_usec * 10);

    lpSystemTimeAsFileTime->dwLowDateTime = (uint32_t)result;
    lpSystemTimeAsFileTime->dwHighDateTime = (uint32_t)(result >> 32);
}

extern "C" uint64_t PalQueryPerformanceCounter()
{
    return GCToOSInterface::QueryPerformanceCounter();
}

extern "C" uint64_t PalQueryPerformanceFrequency()
{
    return GCToOSInterface::QueryPerformanceFrequency();
}

extern "C" uint64_t PalGetCurrentOSThreadId()
{
#if defined(__linux__)
    return (uint64_t)syscall(SYS_gettid);
#elif defined(__APPLE__)
    uint64_t tid;
    pthread_threadid_np(pthread_self(), &tid);
    return (uint64_t)tid;
#elif HAVE_PTHREAD_GETTHREADID_NP
    return (uint64_t)pthread_getthreadid_np();
#elif HAVE_LWP_SELF
    return (uint64_t)_lwp_self();
#else
    // Fallback in case we don't know how to get integer thread id on the current platform
    return (uint64_t)pthread_self();
#endif
}

