// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "trace.h"
#include "utils.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#include <minipal/utils.h>
#include <minipal/mutex.h>

#if defined(_WIN32)
#include <locale.h>
#include <share.h>
#else
#include <unistd.h>
#endif

#define TRACE_VERBOSITY_WARN 2
#define TRACE_VERBOSITY_INFO 3
#define TRACE_VERBOSITY_VERBOSE 4

// g_trace_verbosity encodes DOTNET_HOST_TRACE and DOTNET_HOST_TRACE_VERBOSITY to
// selectively control output of trace_warning/info/verbose:
//  DOTNET_HOST_TRACE=0 (or unset)             -> g_trace_verbosity = 0  (only error())
//  DOTNET_HOST_TRACE=1 DOTNET_HOST_TRACE_VERBOSITY=4 (or unset) -> 4  (verbose+)
//  DOTNET_HOST_TRACE=1 DOTNET_HOST_TRACE_VERBOSITY=3            -> 3  (info+)
//  DOTNET_HOST_TRACE=1 DOTNET_HOST_TRACE_VERBOSITY=2            -> 2  (warning+)
//  DOTNET_HOST_TRACE=1 DOTNET_HOST_TRACE_VERBOSITY=1            -> 1  (error only)
static int g_trace_verbosity = 0;
static FILE* g_trace_file = NULL;

static PAL_THREAD_LOCAL trace_error_writer_fn g_error_writer = NULL;

static minipal_mutex g_trace_lock;

#if defined(_WIN32)
#include <synchapi.h>
static INIT_ONCE g_trace_lock_once = INIT_ONCE_STATIC_INIT;
static BOOL CALLBACK trace_init_lock_callback(PINIT_ONCE init_once, PVOID parameter, PVOID* context)
{
    (void)init_once; (void)parameter; (void)context;
    minipal_mutex_init(&g_trace_lock);
    return TRUE;
}
static void trace_ensure_lock_initialized(void)
{
    InitOnceExecuteOnce(&g_trace_lock_once, trace_init_lock_callback, NULL, NULL);
}
#else
#include <pthread.h>
static pthread_once_t g_trace_lock_once = PTHREAD_ONCE_INIT;
static void trace_init_lock(void)
{
    minipal_mutex_init(&g_trace_lock);
}
static void trace_ensure_lock_initialized(void)
{
    pthread_once(&g_trace_lock_once, trace_init_lock);
}
#endif // _WIN32

static void trace_lock_acquire(void)
{
    trace_ensure_lock_initialized();
    minipal_mutex_enter(&g_trace_lock);
}

static void trace_lock_release(void)
{
    minipal_mutex_leave(&g_trace_lock);
}

// Reads DOTNET_HOST_<name>, falling back to COREHOST_<name> for compat.
// Returns a heap-allocated NUL-terminated value, or NULL if neither variable
// is set. Caller must free() the returned pointer.
static pal_char_t* get_host_env_var(const pal_char_t* name)
{
    pal_char_t full_name[256];

    pal_str_printf(full_name, ARRAY_SIZE(full_name), _X("DOTNET_HOST_%s"), name);
    pal_char_t* value = pal_getenv(full_name);
    if (value != NULL)
        return value;

    pal_str_printf(full_name, ARRAY_SIZE(full_name), _X("COREHOST_%s"), name);
    return pal_getenv(full_name);
}

static void trace_format_timestamp(pal_char_t* buffer, size_t buffer_len)
{
    if (buffer_len == 0)
        return;

    time_t t = time(NULL);
#if defined(_WIN32)
    struct tm tm_l;
    if (gmtime_s(&tm_l, &t) != 0)
    {
        buffer[0] = L'\0';
        return;
    }
    if (wcsftime(buffer, buffer_len, L"%c GMT", &tm_l) == 0)
        buffer[0] = L'\0';
#else
    struct tm tm_l;
    if (gmtime_r(&t, &tm_l) == NULL)
    {
        buffer[0] = '\0';
        return;
    }
    if (strftime(buffer, buffer_len, "%c %Z", &tm_l) == 0)
        buffer[0] = '\0';
#endif
}

static void trace_err_print_line(const pal_char_t* message)
{
#if defined(_WIN32)
    // On Windows, use WriteConsoleW for proper Unicode output, fall back to
    // file output if stderr is redirected.
    HANDLE hStdErr = GetStdHandle(STD_ERROR_HANDLE);
    DWORD mode;
    if (GetConsoleMode(hStdErr, &mode))
    {
        WriteConsoleW(hStdErr, message, (DWORD)pal_strlen(message), NULL, NULL);
        WriteConsoleW(hStdErr, L"\n", 1, NULL, NULL);
    }
    else
    {
        _locale_t loc = _create_locale(LC_ALL, ".utf8");
        _fwprintf_l(stderr, L"%s\n", loc, message);
        _free_locale(loc);
    }
#else
    fputs(message, stderr);
    fputc('\n', stderr);
#endif
}

static void trace_file_vprintf(FILE* f, const pal_char_t* format, va_list vl)
{
#if defined(_WIN32)
    _locale_t loc = _create_locale(LC_ALL, ".utf8");
    _vfwprintf_l(f, format, loc, vl);
    fputwc(L'\n', f);
    _free_locale(loc);
#else
    vfprintf(f, format, vl);
    fputc('\n', f);
#endif
}

static void trace_out_vprint_line(const pal_char_t* format, va_list vl)
{
#if defined(_WIN32)
    va_list vl_copy;
    va_copy(vl_copy, vl);
    int len = 1 + _vscwprintf(format, vl_copy);
    va_end(vl_copy);
    if (len <= 0)
        return;

    pal_char_t* buffer = (pal_char_t*)malloc((size_t)len * sizeof(pal_char_t));
    if (buffer == NULL)
        return;

    _vsnwprintf_s(buffer, len, _TRUNCATE, format, vl);

    HANDLE hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
    DWORD mode;
    if (GetConsoleMode(hStdOut, &mode))
    {
        WriteConsoleW(hStdOut, buffer, (DWORD)wcslen(buffer), NULL, NULL);
        WriteConsoleW(hStdOut, L"\n", 1, NULL, NULL);
    }
    else
    {
        _locale_t loc = _create_locale(LC_ALL, ".utf8");
        _fwprintf_l(stdout, L"%s\n", loc, buffer);
        _free_locale(loc);
    }
    free(buffer);
#else
    vfprintf(stdout, format, vl);
    fputc('\n', stdout);
#endif
}

//
// Turn on tracing for the corehost based on DOTNET_HOST_TRACE and DOTNET_HOST_TRACEFILE env.
//
void trace_setup(void)
{
    pal_char_t* trace_str = get_host_env_var(_X("TRACE"));
    if (trace_str == NULL)
        return;

    int trace_val = pal_xtoi(trace_str);
    free(trace_str);

    if (trace_val > 0)
    {
        if (trace_enable())
        {
            pal_char_t timestamp[100];
            trace_format_timestamp(timestamp, ARRAY_SIZE(timestamp));
            trace_info(_X("Tracing enabled @ %s"), timestamp);
        }
    }
}

bool trace_enable(void)
{
    bool file_open_error = false;
    pal_char_t* tracefile_str = NULL;
    pal_char_t* tracefile_path_to_open = NULL;

    if (g_trace_verbosity)
        return false;

    trace_lock_acquire();

    g_trace_file = stderr; // Trace to stderr by default.
    tracefile_str = get_host_env_var(_X("TRACEFILE"));
    if (tracefile_str != NULL)
    {
        tracefile_path_to_open = tracefile_str;
        if (pal_directory_exists(tracefile_str))
        {
            // If the trace file path is a directory, construct a file path:
            // <dir>/<exe_name>.<pid>.log
            pal_char_t exe_path[APPHOST_PATH_MAX];
            pal_char_t exe_name[256];
            exe_name[0] = _X('\0');
            if (pal_get_own_executable_path(exe_path, ARRAY_SIZE(exe_path)))
            {
                utils_get_filename(exe_path, exe_name, ARRAY_SIZE(exe_name));
                // Strip extension from exe_name.
                pal_char_t* dot = pal_strrchr(exe_name, _X('.'));
                if (dot != NULL)
                    *dot = _X('\0');
            }

            // Fall back to "host" if either the exe path lookup failed or the
            // filename did not fit in the buffer (the original C++ used a
            // dynamically sized pal::string_t and never hit this case).
            if (exe_name[0] == _X('\0'))
                pal_str_printf(exe_name, ARRAY_SIZE(exe_name), _X("host"));

#if defined(_WIN32)
            int pid = (int)GetCurrentProcessId();
#else
            int pid = (int)getpid();
#endif
            pal_char_t trace_path[APPHOST_PATH_MAX];
            int written = pal_str_printf(trace_path, ARRAY_SIZE(trace_path),
                _X("%s") DIR_SEPARATOR_STR _X("%s.%d.log"),
                tracefile_str, exe_name, pid);

            // pal_str_printf returns -1 (Windows _snwprintf_s _TRUNCATE) or the
            // would-be length (POSIX snprintf) when the result was truncated.
            // Treat truncation as an open failure rather than silently using a
            // bad path.
            if (written < 0 || (size_t)written >= ARRAY_SIZE(trace_path))
                file_open_error = true;
            else
                tracefile_path_to_open = trace_path;
        }

        if (!file_open_error)
        {
#if defined(_WIN32)
            FILE* tracefile = _wfsopen(tracefile_path_to_open, L"a", _SH_DENYNO);
#else
            FILE* tracefile = fopen(tracefile_path_to_open, "a");
#endif
            if (tracefile != NULL)
            {
                setvbuf(tracefile, NULL, _IONBF, 0);
                g_trace_file = tracefile;
            }
            else
            {
                file_open_error = true;
            }
        }
    }

    pal_char_t* trace_verbosity_str = get_host_env_var(_X("TRACE_VERBOSITY"));
    if (trace_verbosity_str == NULL)
    {
        g_trace_verbosity = TRACE_VERBOSITY_VERBOSE; // Verbose trace by default.
    }
    else
    {
        g_trace_verbosity = pal_xtoi(trace_verbosity_str);
        free(trace_verbosity_str);
    }

    trace_lock_release();

    if (file_open_error)
        trace_error(_X("Unable to open specified trace file for writing: %s"), tracefile_str);

    free(tracefile_str);
    return true;
}

bool trace_is_enabled(void)
{
    return g_trace_verbosity != 0;
}

void trace_verbose_v(const pal_char_t* format, va_list args)
{
    if (g_trace_verbosity < TRACE_VERBOSITY_VERBOSE)
        return;

    trace_lock_acquire();
    trace_file_vprintf(g_trace_file, format, args);
    trace_lock_release();
}

void trace_verbose(const pal_char_t* format, ...)
{
    va_list args;
    va_start(args, format);
    trace_verbose_v(format, args);
    va_end(args);
}

void trace_info_v(const pal_char_t* format, va_list args)
{
    if (g_trace_verbosity < TRACE_VERBOSITY_INFO)
        return;

    trace_lock_acquire();
    trace_file_vprintf(g_trace_file, format, args);
    trace_lock_release();
}

void trace_info(const pal_char_t* format, ...)
{
    va_list args;
    va_start(args, format);
    trace_info_v(format, args);
    va_end(args);
}

void trace_error_v(const pal_char_t* format, va_list args)
{
    // Always print errors.
    va_list dup_args;
    va_copy(dup_args, args);

    va_list trace_args;
    va_copy(trace_args, args);

    int count = pal_strlen_vprintf(format, args);
    if (count < 0)
    {
        // Format failed. Emit a fallback to stderr/error_writer rather than
        // dropping the error entirely, matching the C++ implementation where
        // such failures propagated as exceptions visible to the caller.
        const pal_char_t fallback[] = _X("Trace error: failed to format message");
        trace_lock_acquire();
        if (g_error_writer == NULL)
            trace_err_print_line(fallback);
        else
            g_error_writer(fallback);
        trace_lock_release();
        va_end(trace_args);
        va_end(dup_args);
        return;
    }

    pal_char_t* buffer = (pal_char_t*)malloc((size_t)(count + 1) * sizeof(pal_char_t));
    if (buffer == NULL)
    {
        const pal_char_t fallback[] = _X("Trace error: out of memory while formatting message");
        trace_lock_acquire();
        if (g_error_writer == NULL)
            trace_err_print_line(fallback);
        else
            g_error_writer(fallback);
        trace_lock_release();
        va_end(trace_args);
        va_end(dup_args);
        return;
    }

    pal_str_vprintf(buffer, (size_t)(count + 1), format, dup_args);

#if defined(_WIN32)
    OutputDebugStringW(buffer);
#endif

    trace_lock_acquire();
    if (g_error_writer == NULL)
        trace_err_print_line(buffer);
    else
        g_error_writer(buffer);

    if (g_trace_verbosity && ((g_trace_file != stderr) || g_error_writer != NULL))
        trace_file_vprintf(g_trace_file, format, trace_args);
    trace_lock_release();

    free(buffer);
    va_end(trace_args);
    va_end(dup_args);
}

void trace_error(const pal_char_t* format, ...)
{
    va_list args;
    va_start(args, format);
    trace_error_v(format, args);
    va_end(args);
}

void trace_println_v(const pal_char_t* format, va_list args)
{
    trace_lock_acquire();
    trace_out_vprint_line(format, args);
    trace_lock_release();
}

void trace_println(const pal_char_t* format, ...)
{
    va_list args;
    va_start(args, format);
    trace_println_v(format, args);
    va_end(args);
}

void trace_println_empty(void)
{
    trace_println(_X(""));
}

void trace_warning_v(const pal_char_t* format, va_list args)
{
    if (g_trace_verbosity < TRACE_VERBOSITY_WARN)
        return;

    trace_lock_acquire();
    trace_file_vprintf(g_trace_file, format, args);
    trace_lock_release();
}

void trace_warning(const pal_char_t* format, ...)
{
    va_list args;
    va_start(args, format);
    trace_warning_v(format, args);
    va_end(args);
}

void trace_flush(void)
{
    if (g_trace_file != NULL)
    {
        trace_lock_acquire();
        fflush(g_trace_file);
        trace_lock_release();
    }

    fflush(stderr);
    fflush(stdout);
}

trace_error_writer_fn trace_set_error_writer(trace_error_writer_fn error_writer)
{
    // No locking needed: g_error_writer is thread-local.
    trace_error_writer_fn previous_writer = g_error_writer;
    g_error_writer = error_writer;
    return previous_writer;
}

trace_error_writer_fn trace_get_error_writer(void)
{
    // No locking needed: g_error_writer is thread-local.
    return g_error_writer;
}
