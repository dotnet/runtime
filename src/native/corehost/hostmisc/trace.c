// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "trace_c.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <minipal/utils.h>
#include <minipal/mutex.h>

#define TRACE_VERBOSITY_WARN 2
#define TRACE_VERBOSITY_INFO 3
#define TRACE_VERBOSITY_VERBOSE 4

static int g_trace_verbosity = 0;
static FILE* g_trace_file = NULL;

static PAL_THREAD_LOCAL trace_error_writer_fn g_error_writer = NULL;

static minipal_mutex g_trace_lock;
static bool g_trace_lock_initialized = false;

static void trace_ensure_lock_initialized(void)
{
    if (!g_trace_lock_initialized)
    {
        minipal_mutex_init(&g_trace_lock);
        g_trace_lock_initialized = true;
    }
}

static void trace_lock_acquire(void)
{
    trace_ensure_lock_initialized();
    minipal_mutex_enter(&g_trace_lock);
}

static void trace_lock_release(void)
{
    minipal_mutex_leave(&g_trace_lock);
}

static bool get_host_env_var(const pal_char_t* name, pal_char_t* value, size_t value_len)
{
    pal_char_t dotnet_host_name[256];
    pal_str_printf(dotnet_host_name, ARRAY_SIZE(dotnet_host_name), _TRACE_X("DOTNET_HOST_%s"), name);
    if (pal_getenv(dotnet_host_name, value, value_len))
        return true;

    pal_char_t corehost_name[256];
    pal_str_printf(corehost_name, ARRAY_SIZE(corehost_name), _TRACE_X("COREHOST_%s"), name);
    return pal_getenv(corehost_name, value, value_len);
}

static void trace_err_print_line(const pal_char_t* message)
{
#if defined(_WIN32)
    // On Windows, use WriteConsoleW for proper Unicode output, fall back to file output if redirected
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
    if (len < 0)
        return;

    pal_char_t* buffer = (pal_char_t*)malloc((size_t)len * sizeof(pal_char_t));
    if (buffer)
    {
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
    }
#else
    vfprintf(stdout, format, vl);
    fputc('\n', stdout);
#endif
}

void trace_setup(void)
{
    pal_char_t trace_str[64];
    if (!get_host_env_var(_TRACE_X("TRACE"), trace_str, ARRAY_SIZE(trace_str)))
        return;

    int trace_val = pal_xtoi(trace_str);
    if (trace_val > 0)
    {
        if (trace_enable())
        {
            trace_info(_TRACE_X("Tracing enabled"));
        }
    }
}

bool trace_enable(void)
{
    bool file_open_error = false;
    pal_char_t tracefile_str[4096];
    tracefile_str[0] = 0;

    if (g_trace_verbosity)
    {
        return false;
    }

    trace_lock_acquire();

    g_trace_file = stderr;
    if (get_host_env_var(_TRACE_X("TRACEFILE"), tracefile_str, ARRAY_SIZE(tracefile_str)))
    {
#if defined(_WIN32)
        FILE* tracefile = _wfsopen(tracefile_str, L"a", _SH_DENYNO);
#else
        FILE* tracefile = fopen(tracefile_str, "a");
#endif
        if (tracefile)
        {
            setvbuf(tracefile, NULL, _IONBF, 0);
            g_trace_file = tracefile;
        }
        else
        {
            file_open_error = true;
        }
    }

    pal_char_t trace_verbosity_str[64];
    if (!get_host_env_var(_TRACE_X("TRACE_VERBOSITY"), trace_verbosity_str, ARRAY_SIZE(trace_verbosity_str)))
    {
        g_trace_verbosity = TRACE_VERBOSITY_VERBOSE;
    }
    else
    {
        g_trace_verbosity = pal_xtoi(trace_verbosity_str);
    }

    trace_lock_release();

    if (file_open_error)
    {
        trace_error(_TRACE_X("Unable to open specified trace file for writing: %s"), tracefile_str);
    }
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
    va_list dup_args;
    va_copy(dup_args, args);

    va_list trace_args;
    va_copy(trace_args, args);

    int count = pal_strlen_vprintf(format, args) + 1;
    pal_char_t* buffer = (pal_char_t*)malloc((size_t)count * sizeof(pal_char_t));
    if (buffer)
    {
        pal_str_vprintf(buffer, (size_t)count, format, dup_args);

#if defined(_WIN32)
        OutputDebugStringW(buffer);
#endif

        trace_lock_acquire();
        if (g_error_writer == NULL)
        {
            trace_err_print_line(buffer);
        }
        else
        {
            g_error_writer(buffer);
        }

        if (g_trace_verbosity && ((g_trace_file != stderr) || g_error_writer != NULL))
        {
            trace_file_vprintf(g_trace_file, format, trace_args);
        }
        trace_lock_release();

        free(buffer);
    }
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
    trace_println(_TRACE_X(""));
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
    // No need for locking since g_error_writer is thread local.
    trace_error_writer_fn previous_writer = g_error_writer;
    g_error_writer = error_writer;
    return previous_writer;
}

trace_error_writer_fn trace_get_error_writer(void)
{
    // No need for locking since g_error_writer is thread local.
    return g_error_writer;
}
