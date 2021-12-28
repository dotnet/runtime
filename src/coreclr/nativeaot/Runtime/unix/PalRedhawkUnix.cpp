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
#include "holder.h"
#include "HardwareExceptions.h"
#include "cgroupcpu.h"

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

#if HAVE_SYS_VMPARAM_H
#include <sys/vmparam.h>
#endif  // HAVE_SYS_VMPARAM_H

#if HAVE_MACH_VM_TYPES_H
#include <mach/vm_types.h>
#endif // HAVE_MACH_VM_TYPES_H

#if HAVE_MACH_VM_PARAM_H
#include <mach/vm_param.h>
#endif  // HAVE_MACH_VM_PARAM_H

#ifdef __APPLE__
#include <mach/vm_statistics.h>
#include <mach/mach_types.h>
#include <mach/mach_init.h>
#include <mach/mach_host.h>
#include <mach/mach_port.h>
#endif // __APPLE__

#if HAVE_CLOCK_GETTIME_NSEC_NP
#include <time.h>
#endif

using std::nullptr_t;

#define PalRaiseFailFastException RaiseFailFastException

#define INVALID_HANDLE_VALUE    ((HANDLE)(intptr_t)-1)

#define PAGE_NOACCESS           0x01
#define PAGE_READWRITE          0x04
#define PAGE_EXECUTE_READ       0x20
#define PAGE_EXECUTE_READWRITE  0x40
#define MEM_COMMIT              0x1000
#define MEM_RESERVE             0x2000
#define MEM_DECOMMIT            0x4000
#define MEM_RELEASE             0x8000

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
    // Abort aborts the process and causes creation of a crash dump
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
#error Don't know how to perform timed wait on this platform
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
                // Verify that if the wait timed out, the event was not set
                ASSERT((st != ETIMEDOUT) || !m_state);
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
        pthread_mutex_unlock(&m_mutex);

        // Unblock all threads waiting for the condition variable
        pthread_cond_broadcast(&m_condition);
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

#if !HAVE_THREAD_LOCAL
extern "C" int __cxa_thread_atexit(void (*)(void*), void*, void *);
extern "C" void *__dso_handle;
#endif

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

extern bool GetCpuLimit(uint32_t* val);

void InitializeCurrentProcessCpuCount()
{
    uint32_t count;

    // If the configuration value has been set, it takes precedence. Otherwise, take into account
    // process affinity and CPU quota limit.

    const unsigned int MAX_PROCESSOR_COUNT = 0xffff;
    uint32_t configValue;

    if (g_pRhConfig->ReadConfigValue(_T("PROCESSOR_COUNT"), &configValue, true /* decimal */) &&
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

    if (!GCToOSInterface::Initialize())
    {
        return false;
    }

    InitializeCpuCGroup();

    InitializeCurrentProcessCpuCount();

    return true;
}

#if HAVE_THREAD_LOCAL

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

#endif // HAVE_THREAD_LOCAL

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

// Attach thread to PAL.
// It can be called multiple times for the same thread.
// It fails fast if a different thread was already registered.
// Parameters:
//  thread        - thread to attach
extern "C" void PalAttachThread(void* thread)
{
#if HAVE_THREAD_LOCAL
    tls_destructionMonitor.SetThread(thread);
#else
    __cxa_thread_atexit(RuntimeThreadShutdown, thread, &__dso_handle);
#endif
}

// Detach thread from PAL.
// It fails fast if some other thread value was attached to PAL.
// Parameters:
//  thread        - thread to detach
// Return:
//  true if the thread was detached, false if there was no attached thread
extern "C" bool PalDetachThread(void* thread)
{
    UNREFERENCED_PARAMETER(thread);
    if (g_threadExitCallback != nullptr)
    {
        g_threadExitCallback();
    }
    return true;
}

#if !defined(USE_PORTABLE_HELPERS) && !defined(FEATURE_RX_THUNKS)
REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalAllocateThunksFromTemplate(HANDLE hTemplateModule, uint32_t templateRva, size_t templateSize, void** newThunksOut)
{
    PORTABILITY_ASSERT("UNIXTODO: Implement this function");
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalFreeThunksFromTemplate(void *pBaseAddress)
{
    PORTABILITY_ASSERT("UNIXTODO: Implement this function");
}
#endif // !USE_PORTABLE_HELPERS && !FEATURE_RX_THUNKS

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalMarkThunksAsValidCallTargets(
    void *virtualAddress,
    int thunkSize,
    int thunksPerBlock,
    int thunkBlockSize,
    int thunkBlocksPerMapping)
{
    return UInt32_TRUE;
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
    // sched_yield yields to another thread in the current process. This implementation
    // won't work well for cross-process synchronization.
    return sched_yield() == 0;
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

REDHAWK_PALEXPORT HANDLE REDHAWK_PALAPI PalCreateEventW(_In_opt_ LPSECURITY_ATTRIBUTES pEventAttributes, UInt32_BOOL manualReset, UInt32_BOOL initialState, _In_opt_z_ const wchar_t* pName)
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

// Returns a 64-bit tick count with a millisecond resolution. It tries its best
// to return monotonically increasing counts and avoid being affected by changes
// to the system clock (either due to drift or due to explicit changes to system
// time).
REDHAWK_PALEXPORT uint64_t REDHAWK_PALAPI PalGetTickCount64()
{
    uint64_t retval = 0;

#if HAVE_CLOCK_GETTIME_NSEC_NP
    {
        retval = clock_gettime_nsec_np(CLOCK_UPTIME_RAW) / tccMilliSecondsToNanoSeconds;
    }
#elif HAVE_CLOCK_MONOTONIC
    {
        clockid_t clockType =
#if HAVE_CLOCK_MONOTONIC_COARSE
            CLOCK_MONOTONIC_COARSE; // good enough resolution, fastest speed
#else
            CLOCK_MONOTONIC;
#endif
        struct timespec ts;
        if (clock_gettime(clockType, &ts) == 0)
        {
            retval = (ts.tv_sec * tccSecondsToMilliSeconds) + (ts.tv_nsec / tccMilliSecondsToNanoSeconds);
        }
        else
        {
            ASSERT_UNCONDITIONALLY("clock_gettime(CLOCK_MONOTONIC) failed\n");
        }
    }
#else
    {
        struct timeval tv;
        if (gettimeofday(&tv, NULL) == 0)
        {
            retval = (tv.tv_sec * tccSecondsToMilliSeconds) + (tv.tv_usec / tccMilliSecondsToMicroSeconds);
        }
        else
        {
            ASSERT_UNCONDITIONALLY("gettimeofday() failed\n");
        }
    }
#endif

    return retval;
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

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalIsAvxEnabled()
{
    return true;
}

REDHAWK_PALEXPORT void PalPrintFatalError(const char* message)
{
    // Write the message using lowest-level OS API available. This is used to print the stack overflow
    // message, so there is not much that can be done here.
    write(STDERR_FILENO, message, strlen(message));
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
    default:
        ASSERT(false);
        break;
    }
    return prot;
}

REDHAWK_PALEXPORT _Ret_maybenull_ _Post_writable_byte_size_(size) void* REDHAWK_PALAPI PalVirtualAlloc(_In_opt_ void* pAddress, size_t size, uint32_t allocationType, uint32_t protect)
{
    // TODO: thread safety!

    if ((allocationType & ~(MEM_RESERVE | MEM_COMMIT)) != 0)
    {
        // TODO: Implement
        return NULL;
    }

    ASSERT(((size_t)pAddress & (OS_PAGE_SIZE - 1)) == 0);

    // Align size to whole pages
    size = (size + (OS_PAGE_SIZE - 1)) & ~(OS_PAGE_SIZE - 1);
    int unixProtect = W32toUnixAccessControl(protect);

    if (allocationType & (MEM_RESERVE | MEM_COMMIT))
    {
        // For Windows compatibility, let the PalVirtualAlloc reserve memory with 64k alignment.
        static const size_t Alignment = 64 * 1024;

        size_t alignedSize = size + (Alignment - OS_PAGE_SIZE);

        void * pRetVal = mmap(pAddress, alignedSize, unixProtect, MAP_ANON | MAP_PRIVATE, -1, 0);

        if (pRetVal != NULL)
        {
            void * pAlignedRetVal = (void *)(((size_t)pRetVal + (Alignment - 1)) & ~(Alignment - 1));
            size_t startPadding = (size_t)pAlignedRetVal - (size_t)pRetVal;
            if (startPadding != 0)
            {
                int ret = munmap(pRetVal, startPadding);
                ASSERT(ret == 0);
            }

            size_t endPadding = alignedSize - (startPadding + size);
            if (endPadding != 0)
            {
                int ret = munmap((void *)((size_t)pAlignedRetVal + size), endPadding);
                ASSERT(ret == 0);
            }

            pRetVal = pAlignedRetVal;
        }

        return pRetVal;
    }

    if (allocationType & MEM_COMMIT)
    {
        int ret = mprotect(pAddress, size, unixProtect);
        return (ret == 0) ? pAddress : NULL;
    }

    return NULL;
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalVirtualFree(_In_ void* pAddress, size_t size, uint32_t freeType)
{
    ASSERT(((freeType & MEM_RELEASE) != MEM_RELEASE) || size == 0);
    ASSERT((freeType & (MEM_RELEASE | MEM_DECOMMIT)) != (MEM_RELEASE | MEM_DECOMMIT));
    ASSERT(freeType != 0);

    // UNIXTODO: Implement this function
    return UInt32_TRUE;
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalVirtualProtect(_In_ void* pAddress, size_t size, uint32_t protect)
{
    int unixProtect = W32toUnixAccessControl(protect);

    return mprotect(pAddress, size, unixProtect) == 0;
}

REDHAWK_PALEXPORT _Ret_maybenull_ void* REDHAWK_PALAPI PalSetWerDataBuffer(_In_ void* pNewBuffer)
{
    static void* pBuffer;
    return PalInterlockedExchangePointer(&pBuffer, pNewBuffer);
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
    return pthread_mutex_init(&lpCriticalSection->mutex, NULL) == 0;
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
    // Using std::getenv instead of getenv since it is guaranteed to be thread safe w.r.t. other
    // std::getenv calls in C++11
    const char* value = std::getenv(name);
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

typedef uint32_t (__stdcall *HijackCallback)(HANDLE hThread, _In_ PAL_LIMITED_CONTEXT* pThreadContext, _In_opt_ void* pCallbackContext);

REDHAWK_PALEXPORT uint32_t REDHAWK_PALAPI PalHijack(HANDLE hThread, _In_ HijackCallback callback, _In_opt_ void* pCallbackContext)
{
    // UNIXTODO: Implement PalHijack
    return E_FAIL;
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

#ifndef __has_builtin
#define __has_builtin(x) 0
#endif

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

extern "C" UInt32_BOOL QueryPerformanceCounter(LARGE_INTEGER *lpPerformanceCount)
{
    // TODO: More efficient, platform-specific implementation
    struct timeval tv;
    if (gettimeofday(&tv, NULL) == -1)
    {
        ASSERT_UNCONDITIONALLY("gettimeofday() failed");
        return UInt32_FALSE;
    }
    lpPerformanceCount->QuadPart =
        (int64_t) tv.tv_sec * (int64_t) tccSecondsToMicroSeconds + (int64_t) tv.tv_usec;
    return UInt32_TRUE;
}

extern "C" UInt32_BOOL QueryPerformanceFrequency(LARGE_INTEGER *lpFrequency)
{
    lpFrequency->QuadPart = (int64_t) tccSecondsToMicroSeconds;
    return UInt32_TRUE;
}

extern "C" uint64_t PalGetCurrentThreadIdForLogging()
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

#if defined(HOST_X86) || defined(HOST_AMD64)

REDHAWK_PALEXPORT void __cpuid(int cpuInfo[4], int function_id)
{
    // Based on the Clang implementation provided in cpuid.h:
    // https://github.com/llvm/llvm-project/blob/master/clang/lib/Headers/cpuid.h

    __asm("  cpuid\n" \
        : "=a"(cpuInfo[0]), "=b"(cpuInfo[1]), "=c"(cpuInfo[2]), "=d"(cpuInfo[3]) \
        : "0"(function_id)
        );
}

REDHAWK_PALEXPORT void __cpuidex(int cpuInfo[4], int function_id, int subFunction_id)
{
    // Based on the Clang implementation provided in cpuid.h:
    // https://github.com/llvm/llvm-project/blob/master/clang/lib/Headers/cpuid.h

    __asm("  cpuid\n" \
        : "=a"(cpuInfo[0]), "=b"(cpuInfo[1]), "=c"(cpuInfo[2]), "=d"(cpuInfo[3]) \
        : "0"(function_id), "2"(subFunction_id)
        );
}

REDHAWK_PALEXPORT uint32_t REDHAWK_PALAPI xmmYmmStateSupport()
{
    DWORD eax;
    __asm("  xgetbv\n" \
        : "=a"(eax) /*output in eax*/\
        : "c"(0) /*inputs - 0 in ecx*/\
        : "edx" /* registers that are clobbered*/
      );
    // check OS has enabled both XMM and YMM state support
    return ((eax & 0x06) == 0x06) ? 1 : 0;
}
#endif // defined(HOST_X86) || defined(HOST_AMD64)

#if defined (HOST_ARM64)

#if HAVE_AUXV_HWCAP_H
#include <sys/auxv.h>
#include <asm/hwcap.h>
#endif

// Based on PAL_GetJitCpuCapabilityFlags from CoreCLR (jitsupport.cpp)
REDHAWK_PALEXPORT void REDHAWK_PALAPI PAL_GetCpuCapabilityFlags(int* flags)
{
    *flags = 0;

#if HAVE_AUXV_HWCAP_H
    unsigned long hwCap = getauxval(AT_HWCAP);

    // HWCAP_* flags are introduced by ARM into the Linux kernel as new extensions are published.
    // For a given kernel, some of these flags may not be present yet.
    // Use ifdef for each to allow for compilation with any vintage kernel.
    // From a single binary distribution perspective, compiling with latest kernel asm/hwcap.h should
    // include all published flags.  Given flags are merged to kernel and published before silicon is
    // available, using the latest kernel for release should be sufficient.
    *flags |= ARM64IntrinsicConstants_ArmBase;
    *flags |= ARM64IntrinsicConstants_ArmBase_Arm64;

#ifdef HWCAP_AES
    if (hwCap & HWCAP_AES)
        *flags |= ARM64IntrinsicConstants_Aes;
#endif
#ifdef HWCAP_ATOMICS
    if (hwCap & HWCAP_ATOMICS)
        *flags |= ARM64IntrinsicConstants_Atomics;
#endif
#ifdef HWCAP_CRC32
    if (hwCap & HWCAP_CRC32)
        *flags |= ARM64IntrinsicConstants_Crc32;
#endif
#ifdef HWCAP_DCPOP
//    if (hwCap & HWCAP_DCPOP)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_ASIMDDP
//    if (hwCap & HWCAP_ASIMDDP)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_FCMA
//    if (hwCap & HWCAP_FCMA)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_FP
//    if (hwCap & HWCAP_FP)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_FPHP
//    if (hwCap & HWCAP_FPHP)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_JSCVT
//    if (hwCap & HWCAP_JSCVT)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_LRCPC
//    if (hwCap & HWCAP_LRCPC)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_PMULL
//    if (hwCap & HWCAP_PMULL)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SHA1
    if (hwCap & HWCAP_SHA1)
        *flags |= ARM64IntrinsicConstants_Sha1;
#endif
#ifdef HWCAP_SHA2
    if (hwCap & HWCAP_SHA2)
        *flags |= ARM64IntrinsicConstants_Sha256;
#endif
#ifdef HWCAP_SHA512
//    if (hwCap & HWCAP_SHA512)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SHA3
//    if (hwCap & HWCAP_SHA3)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_ASIMD
    if (hwCap & HWCAP_ASIMD)
    {
        *flags |= ARM64IntrinsicConstants_AdvSimd;
        *flags |= ARM64IntrinsicConstants_AdvSimd_Arm64;
    }
#endif
#ifdef HWCAP_ASIMDRDM
//    if (hwCap & HWCAP_ASIMDRDM)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_ASIMDHP
//    if (hwCap & HWCAP_ASIMDHP)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SM3
//    if (hwCap & HWCAP_SM3)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SM4
//    if (hwCap & HWCAP_SM4)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP_SVE
//    if (hwCap & HWCAP_SVE)
//        *flags |= ARM64IntrinsicConstants_???;
#endif

#ifdef AT_HWCAP2
    unsigned long hwCap2 = getauxval(AT_HWCAP2);

#ifdef HWCAP2_DCPODP
//    if (hwCap2 & HWCAP2_DCPODP)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVE2
//    if (hwCap2 & HWCAP2_SVE2)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVEAES
//    if (hwCap2 & HWCAP2_SVEAES)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVEPMULL
//    if (hwCap2 & HWCAP2_SVEPMULL)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVEBITPERM
//    if (hwCap2 & HWCAP2_SVEBITPERM)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVESHA3
//    if (hwCap2 & HWCAP2_SVESHA3)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_SVESM4
//    if (hwCap2 & HWCAP2_SVESM4)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_FLAGM2
//    if (hwCap2 & HWCAP2_FLAGM2)
//        *flags |= ARM64IntrinsicConstants_???;
#endif
#ifdef HWCAP2_FRINT
//    if (hwCap2 & HWCAP2_FRINT)
//        *flags |= ARM64IntrinsicConstants_???;
#endif

#endif // AT_HWCAP2

#else // !HAVE_AUXV_HWCAP_H
    // Every ARM64 CPU should support SIMD and FP
    // If the OS have no function to query for CPU capabilities we set just these

    *flags |= ARM64IntrinsicConstants_ArmBase;
    *flags |= ARM64IntrinsicConstants_ArmBase_Arm64;
    *flags |= ARM64IntrinsicConstants_AdvSimd;
    *flags |= ARM64IntrinsicConstants_AdvSimd_Arm64;
#endif // HAVE_AUXV_HWCAP_H
}
#endif
