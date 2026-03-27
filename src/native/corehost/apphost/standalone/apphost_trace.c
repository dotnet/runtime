// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "apphost_trace.h"
#include "apphost_pal.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdatomic.h>
#include <sched.h>
#include <minipal/utils.h>

#define TRACE_VERBOSITY_WARN 2
#define TRACE_VERBOSITY_INFO 3
#define TRACE_VERBOSITY_VERBOSE 4

static int g_trace_verbosity = 0;
static FILE* g_trace_file = NULL;
static _Thread_local trace_error_writer_fn g_error_writer = NULL;

// Simple spinlock for trace serialization
static atomic_flag g_trace_lock = ATOMIC_FLAG_INIT;

static void trace_lock_acquire(void)
{
    uint32_t spin = 0;
    while (atomic_flag_test_and_set_explicit(&g_trace_lock, memory_order_acquire))
    {
        if (spin++ % 1024 == 0)
            sched_yield();
    }
}

static void trace_lock_release(void)
{
    atomic_flag_clear_explicit(&g_trace_lock, memory_order_release);
}

static bool get_host_env_var(const char* name, char* value, size_t value_len)
{
    char dotnet_host_name[256];
    snprintf(dotnet_host_name, sizeof(dotnet_host_name), "DOTNET_HOST_%s", name);
    if (pal_getenv(dotnet_host_name, value, value_len))
        return true;

    char corehost_name[256];
    snprintf(corehost_name, sizeof(corehost_name), "COREHOST_%s", name);
    return pal_getenv(corehost_name, value, value_len);
}

void trace_setup(void)
{
    char trace_str[64];
    if (!get_host_env_var("TRACE", trace_str, sizeof(trace_str)))
        return;

    int trace_val = pal_xtoi(trace_str);
    if (trace_val > 0)
    {
        if (trace_enable())
        {
            trace_info("Tracing enabled");
        }
    }
}

bool trace_enable(void)
{
    bool file_open_error = false;
    char tracefile_str[APPHOST_PATH_MAX];
    tracefile_str[0] = '\0';

    if (g_trace_verbosity)
    {
        return false;
    }

    trace_lock_acquire();

    g_trace_file = stderr;
    if (get_host_env_var("TRACEFILE", tracefile_str, sizeof(tracefile_str)))
    {
        FILE* tracefile = fopen(tracefile_str, "a");
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

    char trace_verbosity_str[64];
    if (!get_host_env_var("TRACE_VERBOSITY", trace_verbosity_str, sizeof(trace_verbosity_str)))
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
        trace_error("Unable to open specified trace file for writing: %s", tracefile_str);
    }
    return true;
}

bool trace_is_enabled(void)
{
    return g_trace_verbosity != 0;
}

void trace_verbose(const char* format, ...)
{
    if (g_trace_verbosity < TRACE_VERBOSITY_VERBOSE)
        return;

    va_list args;
    va_start(args, format);
    trace_lock_acquire();
    vfprintf(g_trace_file, format, args);
    fputc('\n', g_trace_file);
    trace_lock_release();
    va_end(args);
}

void trace_info(const char* format, ...)
{
    if (g_trace_verbosity < TRACE_VERBOSITY_INFO)
        return;

    va_list args;
    va_start(args, format);
    trace_lock_acquire();
    vfprintf(g_trace_file, format, args);
    fputc('\n', g_trace_file);
    trace_lock_release();
    va_end(args);
}

void trace_error(const char* format, ...)
{
    va_list args;
    va_start(args, format);

    va_list dup_args;
    va_copy(dup_args, args);
    int count = vsnprintf(NULL, 0, format, args) + 1;
    char* buffer = (char*)malloc((size_t)count);
    if (buffer)
    {
        vsnprintf(buffer, (size_t)count, format, dup_args);

        trace_lock_acquire();
        if (g_error_writer == NULL)
        {
            pal_err_print_line(buffer);
        }
        else
        {
            g_error_writer(buffer);
        }

        if (g_trace_verbosity && ((g_trace_file != stderr) || g_error_writer != NULL))
        {
            va_list trace_args;
            va_copy(trace_args, dup_args);
            fprintf(g_trace_file, "%s\n", buffer);
            va_end(trace_args);
        }
        trace_lock_release();

        free(buffer);
    }
    va_end(dup_args);
    va_end(args);
}

void trace_println(const char* format, ...)
{
    va_list args;
    va_start(args, format);
    trace_lock_acquire();
    vfprintf(stdout, format, args);
    fputc('\n', stdout);
    trace_lock_release();
    va_end(args);
}

void trace_println_empty(void)
{
    trace_println("");
}

void trace_warning(const char* format, ...)
{
    if (g_trace_verbosity < TRACE_VERBOSITY_WARN)
        return;

    va_list args;
    va_start(args, format);
    trace_lock_acquire();
    vfprintf(g_trace_file, format, args);
    fputc('\n', g_trace_file);
    trace_lock_release();
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
    trace_error_writer_fn previous_writer = g_error_writer;
    g_error_writer = error_writer;
    return previous_writer;
}

trace_error_writer_fn trace_get_error_writer(void)
{
    return g_error_writer;
}
