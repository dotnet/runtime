// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_LINUX) && !defined(_GNU_SOURCE)
#define _GNU_SOURCE
#endif

#include <eventpipe/ep.h>
#include <minipal/conditionvariable.h>
#include <minipal/ospagesize.h>
#include <minipal/utils.h>
#include <time.h>

#ifndef HOST_WINDOWS
#include <minipal/thread.h>
#include <sys/mman.h>
#ifdef TARGET_LINUX
#include <sched.h>
#endif
extern char **environ;
#endif

ep_rt_spin_lock_handle_t ep_rt_test_config_lock;

static PLATFORM_THREAD_LOCAL ep_rt_test_thread_t current_thread;
static volatile uint32_t worker_count;

struct ep_rt_test_wait_event
{
#ifdef HOST_WINDOWS
    HANDLE handle;
#else
    minipal_mutex mutex;
    minipal_condition_variable condition;
    bool manual;
    bool signaled;
#endif
};

uint64_t ep_rt_test_current_thread_id(void)
{
#ifdef HOST_WINDOWS
    return GetCurrentThreadId();
#else
    return (uint64_t)minipal_get_current_thread_id();
#endif
}

ep_rt_thread_handle_t ep_rt_test_current_thread(void)
{
    current_thread.id = ep_rt_test_current_thread_id();
    return &current_thread;
}

EventPipeThread *ep_rt_test_thread_get_or_create(void)
{
    if (!current_thread.holder)
    {
        EventPipeThread *thread = ep_thread_alloc();
        if (!thread)
            return NULL;
        EventPipeThreadHolder *holder = ep_thread_holder_alloc(thread);
        if (!holder)
        {
            ep_thread_free(thread);
            return NULL;
        }
        if (!ep_thread_register(thread))
        {
            ep_thread_holder_free(holder);
            return NULL;
        }
        current_thread.holder = holder;
    }
    return ep_thread_holder_get_thread(current_thread.holder);
}

void ep_rt_test_thread_exited(void)
{
    if (current_thread.holder)
    {
        if (!ep_thread_unregister(ep_thread_holder_get_thread(current_thread.holder)))
            EP_UNREACHABLE("Failed to unregister EventPipe test thread");
        ep_thread_holder_free(current_thread.holder);
        current_thread.holder = NULL;
    }
}

typedef struct
{
    ep_rt_thread_params_t params;
    ep_rt_wait_event_handle_t *started;
    uint64_t *id;
} ep_rt_test_thread_start_t;

#ifdef HOST_WINDOWS
static DWORD WINAPI ep_rt_test_thread_start(void *data)
#else
static void *ep_rt_test_thread_start(void *data)
#endif
{
    ep_rt_test_thread_start_t *start = (ep_rt_test_thread_start_t *)data;
    start->params.thread = ep_rt_test_current_thread();
    *start->id = current_thread.id;
    if (!ep_rt_test_wait_set(start->started))
        EP_UNREACHABLE("Failed to signal EventPipe test thread startup");
    start->params.thread_func(&start->params);
    ep_rt_test_thread_exited();
    free(start);
    ep_rt_atomic_dec_uint32_t(&worker_count);
    return 0;
}

bool ep_rt_test_thread_create(ep_rt_thread_start_func func, void *params, EventPipeThreadType type, uint64_t *id)
{
    ep_rt_wait_event_handle_t started = {0};
    ep_rt_test_wait_alloc(&started, true, false);
    if (!ep_rt_wait_event_is_valid(&started))
        return false;

    uint64_t thread_id = 0;
    ep_rt_test_thread_start_t *start = (ep_rt_test_thread_start_t *)calloc(1, sizeof(*start));
    if (!start)
    {
        ep_rt_test_wait_free(&started);
        return false;
    }
    start->params.thread_type = type;
    start->params.thread_func = func;
    start->params.thread_params = params;
    start->started = &started;
    start->id = &thread_id;

    ep_rt_atomic_inc_uint32_t(&worker_count);
    bool created = false;
#ifdef HOST_WINDOWS
    HANDLE thread = CreateThread(NULL, 0, ep_rt_test_thread_start, start, 0, NULL);
    created = thread != NULL;
    if (created && !CloseHandle(thread))
        EP_UNREACHABLE("Failed to close EventPipe test thread handle");
#else
    pthread_attr_t attributes;
    if (pthread_attr_init(&attributes) == 0)
    {
        pthread_t thread;
        created = pthread_attr_setdetachstate(&attributes, PTHREAD_CREATE_DETACHED) == 0 &&
            pthread_create(&thread, &attributes, ep_rt_test_thread_start, start) == 0;
        if (pthread_attr_destroy(&attributes) != 0)
            EP_UNREACHABLE("Failed to destroy EventPipe test thread attributes");
    }
#endif
    if (created)
    {
        if (ep_rt_test_wait(&started, EP_INFINITE_WAIT) != 0)
            EP_UNREACHABLE("Failed to wait for EventPipe test thread startup");
        if (id)
            *id = thread_id;
    }
    else
    {
        ep_rt_atomic_dec_uint32_t(&worker_count);
        free(start);
    }
    ep_rt_test_wait_free(&started);
    return created;
}

void ep_rt_test_threads_wait(void)
{
    EP_YIELD_WHILE(ep_rt_volatile_load_uint32_t(&worker_count) != 0);
}

void ep_rt_test_wait_alloc(ep_rt_wait_event_handle_t *event, bool manual, bool initial)
{
    event->event = NULL;
    ep_rt_test_wait_event_t *instance = (ep_rt_test_wait_event_t *)calloc(1, sizeof(*instance));
    if (!instance)
        return;
#ifdef HOST_WINDOWS
    instance->handle = CreateEventW(NULL, manual, initial, NULL);
    if (!instance->handle)
    {
        free(instance);
        return;
    }
#else
    if (!minipal_mutex_init(&instance->mutex))
    {
        free(instance);
        return;
    }
    if (!minipal_condition_variable_init(&instance->condition))
    {
        minipal_mutex_destroy(&instance->mutex);
        free(instance);
        return;
    }
    instance->manual = manual;
    instance->signaled = initial;
#endif
    event->event = instance;
}

void ep_rt_test_wait_free(ep_rt_wait_event_handle_t *event)
{
    if (!event->event)
        return;
#ifdef HOST_WINDOWS
    if (!CloseHandle(event->event->handle))
        EP_UNREACHABLE("Failed to close EventPipe test event");
#else
    minipal_condition_variable_destroy(&event->event->condition);
    minipal_mutex_destroy(&event->event->mutex);
#endif
    free(event->event);
    event->event = NULL;
}

bool ep_rt_test_wait_set(ep_rt_wait_event_handle_t *event)
{
#ifdef HOST_WINDOWS
    return SetEvent(event->event->handle) != 0;
#else
    ep_rt_test_wait_event_t *instance = event->event;
    minipal_mutex_enter(&instance->mutex);
    instance->signaled = true;
    bool result = instance->manual ?
        minipal_condition_variable_broadcast(&instance->condition) :
        minipal_condition_variable_signal(&instance->condition);
    minipal_mutex_leave(&instance->mutex);
    return result;
#endif
}

int32_t ep_rt_test_wait(ep_rt_wait_event_handle_t *event, uint32_t timeout)
{
#ifdef HOST_WINDOWS
    return (int32_t)WaitForSingleObject(event->event->handle, timeout);
#else
    ep_rt_test_wait_event_t *instance = event->event;
    int64_t start = minipal_lowres_ticks();
    int32_t result = 0;
    minipal_mutex_enter(&instance->mutex);
    while (!instance->signaled)
    {
        uint32_t remaining = timeout;
        if (timeout != EP_INFINITE_WAIT)
        {
            uint64_t elapsed = (uint64_t)(minipal_lowres_ticks() - start);
            if (elapsed >= timeout)
            {
                result = 258; // WAIT_TIMEOUT
                break;
            }
            remaining -= (uint32_t)elapsed;
        }
        minipal_condition_variable_result wait_result =
            minipal_condition_variable_wait(&instance->condition, &instance->mutex, remaining);
        if (wait_result == MINIPAL_CONDITION_VARIABLE_FAILED)
        {
            result = -1;
            break;
        }
    }
    if (result == 0 && !instance->manual)
        instance->signaled = false;
    minipal_mutex_leave(&instance->mutex);
    return result;
#endif
}

EventPipeWaitHandle ep_rt_test_wait_handle(ep_rt_wait_event_handle_t *event)
{
#ifdef HOST_WINDOWS
    return (EventPipeWaitHandle)event->event->handle;
#else
    return (EventPipeWaitHandle)event->event;
#endif
}

uint32_t ep_rt_test_temp_path(ep_char8_t *buffer, uint32_t length)
{
#ifdef HOST_WINDOWS
    WCHAR path[MAX_PATH + 1];
    DWORD count = GetTempPathW(ARRAY_SIZE(path), path);
    if (count == 0 || count >= ARRAY_SIZE(path))
        return 0;
    ep_char8_t *utf8 = ep_rt_utf16_to_utf8_string((const ep_char16_t *)path);
    if (!utf8)
        return 0;
    size_t size = strlen(utf8);
    bool fits = size < length;
    if (fits)
        memcpy(buffer, utf8, size + 1);
    ep_rt_utf8_string_free(utf8);
    return fits ? (uint32_t)size : 0;
#else
    const char *path = getenv("TMPDIR");
    if (!path || !*path)
        path = "/tmp";
    size_t size = strlen(path);
    bool separator = path[size - 1] != '/';
    if (size >= length || (separator && size + 1 >= length))
        return 0;
    memcpy(buffer, path, size);
    if (separator)
        buffer[size++] = '/';
    buffer[size] = '\0';
    return (uint32_t)size;
#endif
}

uint32_t ep_rt_test_processor_count(void)
{
#ifdef HOST_WINDOWS
    DWORD count = GetActiveProcessorCount(ALL_PROCESSOR_GROUPS);
#else
    long count = sysconf(_SC_NPROCESSORS_ONLN);
#endif
    if (count <= 0 || (uint64_t)count > UINT32_MAX)
        EP_UNREACHABLE("Failed to get processor count");
    return (uint32_t)count;
}

uint32_t ep_rt_test_current_processor(void)
{
#ifdef HOST_WINDOWS
    return GetCurrentProcessorNumber();
#elif defined(TARGET_LINUX)
    int processor = sched_getcpu();
    return processor < 0 ? UINT32_MAX : (uint32_t)processor;
#else
    return UINT32_MAX; // EventPipe's sentinel when the processor number is unavailable.
#endif
}

void ep_rt_test_system_time(EventPipeSystemTime *time)
{
#ifdef HOST_WINDOWS
    SYSTEMTIME utc;
    GetSystemTime(&utc);
    ep_system_time_set(time, utc.wYear, utc.wMonth, utc.wDayOfWeek, utc.wDay,
        utc.wHour, utc.wMinute, utc.wSecond, utc.wMilliseconds);
#else
    uint64_t timestamp = minipal_get_system_time();
    time_t seconds = (time_t)(timestamp / 10000000 - UINT64_C(11644473600));
    struct tm utc;
    if (!gmtime_r(&seconds, &utc))
        EP_UNREACHABLE("Failed to convert system time");
    ep_system_time_set(time, (uint16_t)(utc.tm_year + 1900), (uint16_t)(utc.tm_mon + 1),
        (uint16_t)utc.tm_wday, (uint16_t)utc.tm_mday, (uint16_t)utc.tm_hour,
        (uint16_t)utc.tm_min, (uint16_t)utc.tm_sec, (uint16_t)((timestamp % 10000000) / 10000));
#endif
}

int32_t ep_rt_test_alloc_granularity(void)
{
#ifdef HOST_WINDOWS
    SYSTEM_INFO info;
    GetSystemInfo(&info);
    return (int32_t)info.dwAllocationGranularity;
#else
    return minipal_getpagesize();
#endif
}

uint8_t *ep_rt_test_valloc(size_t size)
{
#ifdef HOST_WINDOWS
    return (uint8_t *)VirtualAlloc(NULL, size, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
#else
    void *buffer = mmap(NULL, size, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    return buffer == MAP_FAILED ? NULL : (uint8_t *)buffer;
#endif
}

void ep_rt_test_vfree(uint8_t *buffer, size_t size)
{
    if (!buffer)
        return;
#ifdef HOST_WINDOWS
    if (!VirtualFree(buffer, 0, MEM_RELEASE))
#else
    if (munmap(buffer, size) != 0)
#endif
        EP_UNREACHABLE("Failed to free EventPipe test virtual memory");
}

FILE *ep_rt_test_file_open(const ep_char8_t *path)
{
#ifdef HOST_WINDOWS
    ep_char16_t *utf16 = ep_rt_utf8_to_utf16le_string(path);
    if (!utf16)
        return NULL;
    FILE *file = NULL;
    _wfopen_s(&file, (const WCHAR *)utf16, L"wb");
    ep_rt_utf16_string_free(utf16);
    return file;
#else
    return fopen(path, "wb");
#endif
}

void ep_rt_test_environment(dn_vector_ptr_t *environment)
{
#ifdef HOST_WINDOWS
    WCHAR *block = GetEnvironmentStringsW();
    if (!block)
        EP_UNREACHABLE("Failed to read process environment");
    for (const WCHAR *entry = block; *entry; entry += wcslen(entry) + 1)
    {
        ep_char16_t *copy = ep_rt_utf16_string_dup((const ep_char16_t *)entry);
        if (!copy || !dn_vector_ptr_push_back(environment, copy))
            EP_UNREACHABLE("Failed to copy process environment");
    }
    if (!FreeEnvironmentStringsW(block))
        EP_UNREACHABLE("Failed to free process environment");
#else
    for (char **entry = environ; *entry; ++entry)
    {
        ep_char16_t *copy = ep_rt_utf8_to_utf16le_string(*entry);
        if (!copy || !dn_vector_ptr_push_back(environment, copy))
            EP_UNREACHABLE("Failed to copy process environment");
    }
#endif
}

bool ep_rt_test_string_replace(ep_char8_t **str, const ep_char8_t *search, const ep_char8_t *replacement)
{
    if (!*str)
        return false;
    const ep_char8_t *match = strstr(*str, search);
    if (!match)
        return false;
    size_t prefix = (size_t)(match - *str);
    size_t search_length = strlen(search);
    size_t replacement_length = strlen(replacement);
    size_t remaining = strlen(*str) - search_length;
    if (remaining == SIZE_MAX || replacement_length > SIZE_MAX - remaining - 1)
        return false;
    ep_char8_t *result = ep_rt_utf8_string_alloc(remaining + replacement_length + 1);
    if (!result)
        return false;
    memcpy(result, *str, prefix);
    memcpy(result + prefix, replacement, replacement_length);
    memcpy(result + prefix + replacement_length, match + search_length, remaining - prefix + 1);
    ep_rt_utf8_string_free(*str);
    *str = result;
    return true;
}
