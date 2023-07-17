// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <eventpipe/ep-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-types.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-stack-contents.h>
#include <eventpipe/ep-rt.h>

#ifdef TARGET_WINDOWS
#include <windows.h>
#else
#include <fcntl.h>
#include <sys/stat.h>
#include <unistd.h>
#endif

// The regdisplay.h, StackFrameIterator.h, and thread.h includes are present only to access the Thread
// class and can be removed if it turns out that the required ep_rt_thread_handle_t can be
// implemented in some manner that doesn't rely on the Thread class.

#include "gcenv.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "holder.h"
#include "SpinLock.h"

#ifndef DIRECTORY_SEPARATOR_CHAR
#ifdef TARGET_UNIX
#define DIRECTORY_SEPARATOR_CHAR '/'
#else // TARGET_UNIX
#define DIRECTORY_SEPARATOR_CHAR '\\'
#endif // TARGET_UNIX
#endif

#ifdef TARGET_UNIX
// Per module (1 for NativeAOT), key that will be used to implement TLS in Unix
pthread_key_t eventpipe_tls_key;
__thread EventPipeThreadHolder* eventpipe_tls_instance;
#else
thread_local EventPipeAotThreadHolderTLS EventPipeAotThreadHolderTLS::g_threadHolderTLS;
#endif

// Uses _rt_aot_lock_internal_t that has CrstStatic as a field
// This is initialized at the beginning and EventPipe library requires the lock handle to be maintained by the runtime
ep_rt_lock_handle_t _ep_rt_aot_config_lock_handle;
CrstStatic _ep_rt_aot_config_lock;


#ifndef TARGET_UNIX
uint32_t *_ep_rt_aot_proc_group_offsets;
#endif

/*
 * Forward declares of all static functions.
 */


static
void
walk_managed_stack_for_threads (
    ep_rt_thread_handle_t sampling_thread,
    EventPipeEvent *sampling_event);


bool
ep_rt_aot_walk_managed_stack_for_thread (
    ep_rt_thread_handle_t thread,
    EventPipeStackContents *stack_contents)
{
    PalDebugBreak();
    return false;
}

// The thread store lock must already be held by the thread before this function
// is called.  ThreadSuspend::SuspendEE acquires the thread store lock.
static
void
walk_managed_stack_for_threads (
    ep_rt_thread_handle_t sampling_thread,
    EventPipeEvent *sampling_event)
{
}

void
ep_rt_aot_sample_profiler_write_sampling_event_for_threads (
    ep_rt_thread_handle_t sampling_thread,
    EventPipeEvent *sampling_event)
{
}

const ep_char8_t *
ep_rt_aot_entrypoint_assembly_name_get_utf8 (void) 
{
    // We are (intentionally for now) using the module name rather than entry assembly
    // Cannot use __cpp_threadsafe_static_init feature since it will bring in the C++ runtime and need to use threadsafe way to initialize entrypoint_assembly_name
    static const ep_char8_t * entrypoint_assembly_name = nullptr;
    if (entrypoint_assembly_name == nullptr) {
        ep_char8_t * entrypoint_assembly_name_local;
        const TCHAR * wszModuleFileName = NULL;
        HANDLE moduleHandle = PalGetModuleHandleFromPointer((void*)&ep_rt_aot_entrypoint_assembly_name_get_utf8);
        if(PalGetModuleFileName(&wszModuleFileName, moduleHandle) == 0) {
            entrypoint_assembly_name_local = reinterpret_cast<ep_char8_t *>(malloc(1));
            if(entrypoint_assembly_name_local==NULL) {
                return NULL;
            }
            *entrypoint_assembly_name_local = '\0';
        }
        else {
#ifdef HOST_WINDOWS
            const wchar_t* process_name_const = wcsrchr(wszModuleFileName, DIRECTORY_SEPARATOR_CHAR);
            if (process_name_const != NULL) {
                process_name_const++;
            }
            else {
                process_name_const = reinterpret_cast<const wchar_t *>(wszModuleFileName);
            }
            size_t len = -1;
            const wchar_t* extension = wcsrchr(process_name_const, '.');
            if (extension != NULL) {
                len = extension - process_name_const;
            }
            entrypoint_assembly_name_local = ep_rt_utf16_to_utf8_string(reinterpret_cast<const ep_char16_t *>(process_name_const), len);
#else
            const ep_char8_t* process_name_const = strrchr(wszModuleFileName, DIRECTORY_SEPARATOR_CHAR);
            if (process_name_const != NULL) {
                process_name_const++;
            }
            else {
                process_name_const = reinterpret_cast<const ep_char8_t *>(wszModuleFileName);
            }
            size_t len = strlen(process_name_const);
            const ep_char8_t *extension = strrchr(process_name_const, '.');
            if (extension != NULL) {
                len = extension - process_name_const;
            }
            ep_char8_t* process_name = reinterpret_cast<ep_char8_t *>(malloc(len + 1));
            if (process_name == NULL) {
                return NULL;
            }
            memcpy(process_name, process_name_const, len);
            process_name[len] = '\0';
            entrypoint_assembly_name_local = reinterpret_cast<ep_char8_t*>(process_name);
#endif // HOST_WINDOWS
        }

        if (PalInterlockedCompareExchangePointer((void**)(&entrypoint_assembly_name), (void*)(entrypoint_assembly_name_local), nullptr) != nullptr)
            free(entrypoint_assembly_name_local);
    }

    return entrypoint_assembly_name;
}

const ep_char8_t *
ep_rt_aot_diagnostics_command_line_get (void)
{
    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: revisit commandline for AOT
#ifdef TARGET_WINDOWS
    const ep_char16_t* command_line = reinterpret_cast<const ep_char16_t *>(::GetCommandLineW());
    return ep_rt_utf16_to_utf8_string(command_line, -1);
#elif TARGET_LINUX
    FILE *cmdline_file = ::fopen("/proc/self/cmdline", "r");
    if (cmdline_file == nullptr)
        return "";

    char *line = NULL;
    size_t line_len = 0;
    if (::getline (&line, &line_len, cmdline_file) == -1) {
        ::fclose (cmdline_file);
        return "";
    }

    ::fclose (cmdline_file);
    return reinterpret_cast<const ep_char8_t*>(line);
#else
    return "";
#endif
}

uint32_t
ep_rt_aot_atomic_inc_uint32_t (volatile uint32_t *value)
{
    STATIC_CONTRACT_NOTHROW;
    return static_cast<uint32_t>(PalInterlockedIncrement ((volatile int32_t *)(value)));
}

uint32_t
ep_rt_aot_atomic_dec_uint32_t (volatile uint32_t *value)
{
    STATIC_CONTRACT_NOTHROW;
    return static_cast<uint32_t>(PalInterlockedDecrement ((volatile int32_t *)(value)));
}

int32_t
ep_rt_aot_atomic_inc_int32_t (volatile int32_t *value)
{
    STATIC_CONTRACT_NOTHROW;
   return static_cast<int32_t>(PalInterlockedIncrement (value));
}

int32_t
ep_rt_aot_atomic_dec_int32_t (volatile int32_t *value)
{
    STATIC_CONTRACT_NOTHROW;
   return static_cast<int32_t>(PalInterlockedDecrement (value));
}

int64_t
ep_rt_aot_atomic_inc_int64_t (volatile int64_t *value)
{
    STATIC_CONTRACT_NOTHROW;

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Consider replacing with a new PalInterlockedIncrement64 service
    int64_t currentValue;
    do {
        currentValue = *value;
    } while (currentValue != PalInterlockedCompareExchange64(value, (currentValue + 1), currentValue));

    // The current value has been atomically replaced with the incremented value.
    return (currentValue + 1);
}

int64_t
ep_rt_aot_atomic_dec_int64_t (volatile int64_t *value) { 
    STATIC_CONTRACT_NOTHROW;

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Consider replacing with a new PalInterlockedDecrement64 service
    int64_t currentValue;
    do {
        currentValue = *value;
    } while (currentValue != PalInterlockedCompareExchange64(value, (currentValue - 1), currentValue));

    // The current value has been atomically replaced with the decremented value.
    return (currentValue - 1);
}

size_t
ep_rt_aot_atomic_compare_exchange_size_t (volatile size_t *target, size_t expected, size_t value) {
    STATIC_CONTRACT_NOTHROW;
#ifdef HOST_64BIT
    return static_cast<size_t>(PalInterlockedCompareExchange64 ((volatile int64_t *)target, (int64_t)value, (int64_t)expected));
#else
    return static_cast<size_t>(PalInterlockedCompareExchange ((volatile int32_t *)target, (int32_t)value, (int32_t)expected));
#endif
}

ep_char8_t *
ep_rt_aot_atomic_compare_exchange_utf8_string (ep_char8_t *volatile *target, ep_char8_t *expected, ep_char8_t *value) { 
    STATIC_CONTRACT_NOTHROW;
    return static_cast<ep_char8_t *>(PalInterlockedCompareExchangePointer ((void *volatile *)target, value, expected));
}


void
ep_rt_aot_wait_event_alloc (
    ep_rt_wait_event_handle_t *wait_event,
    bool manual,
    bool initial)
{
    STATIC_CONTRACT_NOTHROW;

    EP_ASSERT (wait_event != NULL);
    EP_ASSERT (wait_event->event == NULL);

    wait_event->event = new (nothrow) CLREventStatic ();
    if (wait_event->event) {
        // NativeAOT has the NoThrow versions
        if (manual)
            wait_event->event->CreateManualEventNoThrow (initial);
        else
            wait_event->event->CreateAutoEventNoThrow (initial);
    }
}

void
ep_rt_aot_wait_event_free (ep_rt_wait_event_handle_t *wait_event)
{
    STATIC_CONTRACT_NOTHROW;

    if (wait_event != NULL && wait_event->event != NULL) {
        wait_event->event->CloseEvent ();
        delete wait_event->event;
        wait_event->event = NULL;
    }
}

bool
ep_rt_aot_wait_event_set (ep_rt_wait_event_handle_t *wait_event) 
{ 
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

    return wait_event->event->Set ();
}

int32_t
ep_rt_aot_wait_event_wait (
    ep_rt_wait_event_handle_t *wait_event,
    uint32_t timeout,
    bool alertable) 
{ 
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

    return wait_event->event->Wait (timeout, alertable);
}

bool
ep_rt_aot_wait_event_is_valid (ep_rt_wait_event_handle_t *wait_event) 
{ 
    STATIC_CONTRACT_NOTHROW;

    if (wait_event == NULL || wait_event->event == NULL)
        return false;

    return wait_event->event->IsValid ();
}

/*
 * Misc.
 */

int
ep_rt_aot_get_last_error (void)
{
    STATIC_CONTRACT_NOTHROW;
    return PalGetLastError();
}

bool
ep_rt_aot_thread_create (
    void *thread_func,
    void *params,
    EventPipeThreadType thread_type,
    void *id)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (thread_func != NULL);

    // shipping criteria: no EVENTPIPE-NATIVEAOT-TODO left in the codebase
    // TODO: Fill in the outgoing id if any callers ever need it
    if (id)
        *reinterpret_cast<DWORD*>(id) = 0xffffffff;

    switch (thread_type)
    {
    default:
        return false;

    case EP_THREAD_TYPE_SERVER:
        // Match CoreCLR and hardcode a null thread context in this case.
        return PalStartEventPipeHelperThread(reinterpret_cast<BackgroundCallback>(thread_func), NULL);

    case EP_THREAD_TYPE_SESSION:
    case EP_THREAD_TYPE_SAMPLING:
        ep_rt_thread_params_t* thread_params = new (nothrow) ep_rt_thread_params_t ();
        if (!thread_params)
            return false;

        thread_params->thread_type = thread_type;
        thread_params->thread_func = reinterpret_cast<ep_rt_thread_start_func>(thread_func);
        thread_params->thread_params = params;
        if (!PalStartEventPipeHelperThread(reinterpret_cast<BackgroundCallback>(ep_rt_thread_aot_start_session_or_sampling_thread), thread_params)) {
            delete thread_params;
            return false;
        }

        return true;
    }
}

void
ep_rt_aot_thread_sleep (uint64_t ns)
{
    STATIC_CONTRACT_NOTHROW;
    PalSleep(static_cast<uint32_t>(ns/1000000));
}

uint32_t
ep_rt_aot_current_process_get_id (void)
{
    STATIC_CONTRACT_NOTHROW;
    return static_cast<uint32_t>(GetCurrentProcessId ());
}

ep_rt_thread_id_t
ep_rt_aot_current_thread_get_id (void)
{
    STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX
    return static_cast<ep_rt_thread_id_t>(PalGetCurrentOSThreadId());
#else
    return static_cast<ep_rt_thread_id_t>(::GetCurrentThreadId ());
#endif
}

int64_t
ep_rt_aot_perf_counter_query (void)
{
    STATIC_CONTRACT_NOTHROW;
    return (int64_t)PalQueryPerformanceCounter();
}

int64_t
ep_rt_aot_perf_frequency_query (void)
{
    STATIC_CONTRACT_NOTHROW;
    return (int64_t)PalQueryPerformanceFrequency();
}

int64_t
ep_rt_aot_system_timestamp_get (void)
{
    STATIC_CONTRACT_NOTHROW;

    FILETIME value;
    GetSystemTimeAsFileTime (&value);
    return static_cast<int64_t>(((static_cast<uint64_t>(value.dwHighDateTime)) << 32) | static_cast<uint64_t>(value.dwLowDateTime));
}

ep_rt_file_handle_t
ep_rt_aot_file_open_write (const ep_char8_t *path)
{
    if (!path)
        return INVALID_HANDLE_VALUE;

#ifdef TARGET_WINDOWS
    ep_char16_t *path_utf16 = ep_rt_utf8_to_utf16le_string (path, -1);
    if (!path_utf16)
        return INVALID_HANDLE_VALUE;

    HANDLE res = ::CreateFileW (reinterpret_cast<LPCWSTR>(path_utf16), GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    ep_rt_utf16_string_free (path_utf16);
    return static_cast<ep_rt_file_handle_t>(res);
#else
    mode_t perms = S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH;
    int fd = creat (path, perms);
    if (fd == -1)
        return INVALID_HANDLE_VALUE;

    return (ep_rt_file_handle_t)(ptrdiff_t)fd;
#endif
}

bool
ep_rt_aot_file_close (ep_rt_file_handle_t file_handle)
{
#ifdef TARGET_WINDOWS
    return ::CloseHandle (file_handle) != FALSE;
#else
    int fd = (int)(ptrdiff_t)file_handle;
    close (fd);
    return true;
#endif
}

bool
ep_rt_aot_file_write (
    ep_rt_file_handle_t file_handle,
    const uint8_t *buffer,
    uint32_t bytes_to_write,
    uint32_t *bytes_written)
{
#ifdef TARGET_WINDOWS
    return ::WriteFile (file_handle, buffer, bytes_to_write, reinterpret_cast<LPDWORD>(bytes_written), NULL) != FALSE;
#else
    int fd = (int)(ptrdiff_t)file_handle;
    int ret;
    do {
        ret = write (fd, buffer, bytes_to_write);
    } while (ret == -1 && errno == EINTR);

    if (ret == -1) {
        if (bytes_written != NULL) {
            *bytes_written = 0;
        }

        return false;
    }

    if (bytes_written != NULL)
        *bytes_written = ret;

    return true;
#endif
}

uint8_t *
ep_rt_aot_valloc0 (size_t buffer_size)
{
    STATIC_CONTRACT_NOTHROW;
    return reinterpret_cast<uint8_t *>(PalVirtualAlloc (NULL, buffer_size, MEM_COMMIT, PAGE_READWRITE));
}

void
ep_rt_aot_vfree (
    uint8_t *buffer,
    size_t buffer_size)
{
    STATIC_CONTRACT_NOTHROW;

    if (buffer)
        PalVirtualFree (buffer, 0, MEM_RELEASE);
}

void
ep_rt_aot_spin_lock_alloc (ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;

    // EventPipe library expects SpinLocks to be used but NativeAOT will use a lock and change as needed if performance is an issue
    // Uses _rt_aot_lock_internal_t that has CrstStatic as a field
    // EventPipe library will intialize using thread, EventPipeBufferManager instances and will maintain these on the EventPipe library side

    spin_lock->lock = new (nothrow) CrstStatic ();
    spin_lock->lock->InitNoThrow (CrstType::CrstEventPipe);
}

void
ep_rt_aot_spin_lock_free (ep_rt_spin_lock_handle_t *spin_lock)
{
    STATIC_CONTRACT_NOTHROW;

    if (spin_lock && spin_lock->lock) {
        delete spin_lock->lock;
        spin_lock->lock = NULL;
    }
}

size_t
ep_rt_aot_utf16_string_len (const ep_char16_t *str)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (str != NULL);

    #ifdef TARGET_UNIX
        const uint16_t *a = (const uint16_t *)str;
        size_t length = 0;
        while (a [length])
            ++length;
        return length;
    #else
        return wcslen (reinterpret_cast<LPCWSTR>(str));
    #endif
}

uint32_t
ep_rt_aot_volatile_load_uint32_t (const volatile uint32_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    return VolatileLoad<uint32_t> ((const uint32_t *)ptr);
}

uint32_t
ep_rt_aot_volatile_load_uint32_t_without_barrier (const volatile uint32_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    return VolatileLoadWithoutBarrier<uint32_t> ((const uint32_t *)ptr);
}

void
ep_rt_aot_volatile_store_uint32_t (
    volatile uint32_t *ptr,
    uint32_t value)
{
    STATIC_CONTRACT_NOTHROW;
    VolatileStore<uint32_t> ((uint32_t *)ptr, value);
}

void
ep_rt_aot_volatile_store_uint32_t_without_barrier (
    volatile uint32_t *ptr,
    uint32_t value)
{
    STATIC_CONTRACT_NOTHROW;
    VolatileStoreWithoutBarrier<uint32_t>((uint32_t *)ptr, value);
}

uint64_t
ep_rt_aot_volatile_load_uint64_t (const volatile uint64_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    return VolatileLoad<uint64_t> ((const uint64_t *)ptr);
}

uint64_t
ep_rt_aot_volatile_load_uint64_t_without_barrier (const volatile uint64_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    return VolatileLoadWithoutBarrier<uint64_t> ((const uint64_t *)ptr);
}

void
ep_rt_aot_volatile_store_uint64_t (
    volatile uint64_t *ptr,
    uint64_t value)
{
    STATIC_CONTRACT_NOTHROW;
    VolatileStore<uint64_t> ((uint64_t *)ptr, value);
}

void
ep_rt_aot_volatile_store_uint64_t_without_barrier (
    volatile uint64_t *ptr,
    uint64_t value)
{
    STATIC_CONTRACT_NOTHROW;
    VolatileStoreWithoutBarrier<uint64_t> ((uint64_t *)ptr, value);
}

int64_t
ep_rt_aot_volatile_load_int64_t (const volatile int64_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    return VolatileLoad<int64_t> ((int64_t *)ptr);
}

int64_t
ep_rt_aot_volatile_load_int64_t_without_barrier (const volatile int64_t *ptr)
{
    STATIC_CONTRACT_NOTHROW;
    return VolatileLoadWithoutBarrier<int64_t> ((int64_t *)ptr);
}

void
ep_rt_aot_volatile_store_int64_t (
    volatile int64_t *ptr,
    int64_t value)
{
    STATIC_CONTRACT_NOTHROW;
    VolatileStore<int64_t> ((int64_t *)ptr, value);
}

void
ep_rt_aot_volatile_store_int64_t_without_barrier (
    volatile int64_t *ptr,
    int64_t value)
{
    STATIC_CONTRACT_NOTHROW;
    VolatileStoreWithoutBarrier<int64_t> ((int64_t *)ptr, value);
}

void *
ep_rt_aot_volatile_load_ptr (volatile void **ptr)
{
    STATIC_CONTRACT_NOTHROW;
    return VolatileLoad<void *> ((void **)ptr);
}

void *
ep_rt_aot_volatile_load_ptr_without_barrier (volatile void **ptr)
{
    STATIC_CONTRACT_NOTHROW;
    return VolatileLoadWithoutBarrier<void *> ((void **)ptr);
}

void
ep_rt_aot_volatile_store_ptr (
    volatile void **ptr,
    void *value)
{
    STATIC_CONTRACT_NOTHROW;
    VolatileStore<void *> ((void **)ptr, value);
}

void
ep_rt_aot_volatile_store_ptr_without_barrier (
    volatile void **ptr,
    void *value)
{
    STATIC_CONTRACT_NOTHROW;
    VolatileStoreWithoutBarrier<void *> ((void **)ptr, value);
}

void unix_tls_callback_fn(void *value) 
{
    if (value) {
        // we need to do the unallocation here
        EventPipeThreadHolder *thread_holder_old = static_cast<EventPipeThreadHolder*>(value);    
        // @TODO - inline
        thread_holder_free_func (thread_holder_old);
        value = NULL;
    }
}

void ep_rt_aot_init (void)
{
    extern ep_rt_lock_handle_t _ep_rt_aot_config_lock_handle;
    extern CrstStatic _ep_rt_aot_config_lock;

    _ep_rt_aot_config_lock_handle.lock = &_ep_rt_aot_config_lock;
    _ep_rt_aot_config_lock_handle.lock->InitNoThrow (CrstType::CrstEventPipeConfig);

    // Initialize the pthread key used for TLS in Unix
    #ifdef TARGET_UNIX
    pthread_key_create(&eventpipe_tls_key, unix_tls_callback_fn);
    #endif
}

bool ep_rt_aot_lock_acquire (ep_rt_lock_handle_t *lock)
{
    if (lock) {
        lock->lock->Enter();
        return true;
    }
    return false;
}

bool ep_rt_aot_lock_release (ep_rt_lock_handle_t *lock)
{
    if (lock) {
        lock->lock->Leave();
        return true;
    }
    return false;
}

bool ep_rt_aot_spin_lock_acquire (ep_rt_spin_lock_handle_t *spin_lock)
{
    // In NativeAOT, we use a lock, instead of a SpinLock. 
    // The method signature matches the EventPipe library expectation of a SpinLock
    if (spin_lock) {
        spin_lock->lock->Enter();
        return true;
    }
    return false;
}

bool ep_rt_aot_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock)
{
    // In NativeAOT, we use a lock, instead of a SpinLock. 
    // The method signature matches the EventPipe library expectation of a SpinLock
    if (spin_lock) {
        spin_lock->lock->Leave();
        return true;
    }
    return false;
}

#ifndef HOST_WIN32
#if defined(__APPLE__)
#if defined (HOST_OSX)
extern "C" {char ***_NSGetEnviron(void);}
#define environ (*_NSGetEnviron())
#else
static char *_ep_rt_aot_environ[1] = { NULL };
#define environ _ep_rt_aot_environ
#endif /* defined (HOST_OSX) */
#else
extern "C" {
extern char **environ;
}
#endif /* defined (__APPLE__) */
#endif /* !defined (HOST_WIN32) */

void ep_rt_aot_os_environment_get_utf16 (dn_vector_ptr_t *env_array)
{
    STATIC_CONTRACT_NOTHROW;
    EP_ASSERT (env_array != NULL);

#ifdef HOST_WIN32
    ep_char16_t * envs = reinterpret_cast<ep_char16_t *>(GetEnvironmentStringsW ());
    if (envs) {
        const ep_char16_t * next = envs;
        while (*next) {
            dn_vector_ptr_push_back (env_array, ep_rt_utf16_string_dup (reinterpret_cast<const ep_char16_t *>(next)));
            next += ep_rt_utf16_string_len (reinterpret_cast<const ep_char16_t *>(next)) + 1;
        }
        FreeEnvironmentStringsW (reinterpret_cast<LPWSTR>(envs));
    }
#else
    ep_char8_t **next = NULL;
    for (next = environ; *next != NULL; ++next)
        dn_vector_ptr_push_back (env_array, ep_rt_utf8_to_utf16le_string (*next, -1));
#endif
}

#ifdef EP_CHECKED_BUILD

void ep_rt_aot_lock_requires_lock_held (const ep_rt_lock_handle_t *lock)
{
    EP_ASSERT (((ep_rt_lock_handle_t *)lock)->lock->OwnedByCurrentThread ());
}

void ep_rt_aot_lock_requires_lock_not_held (const ep_rt_lock_handle_t *lock)
{
    EP_ASSERT (lock->lock == NULL || !((ep_rt_lock_handle_t *)lock)->lock->OwnedByCurrentThread ());
}

void ep_rt_aot_spin_lock_requires_lock_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
    EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));
    EP_ASSERT (spin_lock->lock->OwnedByCurrentThread ());
}

void ep_rt_aot_spin_lock_requires_lock_not_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
    EP_ASSERT (spin_lock->lock == NULL || !spin_lock->lock->OwnedByCurrentThread ());
}

#endif /* EP_CHECKED_BUILD */
#endif /* ENABLE_PERFTRACING */
