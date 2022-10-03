// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "trace.h"
#include <mutex>

// g_trace_verbosity is used to encode COREHOST_TRACE and COREHOST_TRACE_VERBOSITY to selectively control output of
//    trace::warn(), trace::info(), and trace::verbose()
//  COREHOST_TRACE=0 COREHOST_TRACE_VERBOSITY=N/A        implies g_trace_verbosity = 0.  // Trace "disabled". error() messages will be produced.
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=4 or unset implies g_trace_verbosity = 4.  // Trace "enabled".  verbose(), info(), warn() and error() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=3          implies g_trace_verbosity = 3.  // Trace "enabled".  info(), warn() and error() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=2          implies g_trace_verbosity = 2.  // Trace "enabled".  warn() and error() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=1          implies g_trace_verbosity = 1.  // Trace "enabled".  error() messages will be produced
static int g_trace_verbosity = 0;
static FILE * g_trace_file = stderr;
static pal::mutex_t g_trace_mutex;
thread_local static trace::error_writer_fn g_error_writer = nullptr;

//
// Turn on tracing for the corehost based on "COREHOST_TRACE" & "COREHOST_TRACEFILE" env.
//
void trace::setup()
{
    // Read trace environment variable
    pal::string_t trace_str;
    if (!pal::getenv(_X("COREHOST_TRACE"), &trace_str))
    {
        return;
    }

    auto trace_val = pal::xtoi(trace_str.c_str());
    if (trace_val > 0)
    {
        if (trace::enable())
        {
            auto ts = pal::get_timestamp();
            trace::info(_X("Tracing enabled @ %s"), ts.c_str());
        }
    }
}

bool trace::enable()
{
    bool file_open_error = false;
    pal::string_t tracefile_str;

    if (g_trace_verbosity)
    {
        return false;
    }
    else
    {
        std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

        g_trace_file = stderr;
        if (pal::getenv(_X("COREHOST_TRACEFILE"), &tracefile_str))
        {
            FILE *tracefile = pal::file_open(tracefile_str, _X("a"));

            if (tracefile)
            {
                setvbuf(tracefile, nullptr, _IONBF, 0);
                g_trace_file = tracefile;
            }
            else
            {
                file_open_error = true;
            }
        }

        pal::string_t trace_str;
        if (!pal::getenv(_X("COREHOST_TRACE_VERBOSITY"), &trace_str))
        {
            g_trace_verbosity = 4;  // Verbose trace by default
        }
        else
        {
            g_trace_verbosity = pal::xtoi(trace_str.c_str());
        }
    }

    if (file_open_error)
    {
        trace::error(_X("Unable to open COREHOST_TRACEFILE=%s for writing"), tracefile_str.c_str());
    }
    return true;
}

bool trace::is_enabled()
{
    return g_trace_verbosity;
}

void trace::verbose(const pal::char_t* format, ...)
{
    if (g_trace_verbosity > 3)
    {
        std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

        va_list args;
        va_start(args, format);
        pal::file_vprintf(g_trace_file, format, args);
        va_end(args);
    }
}

void trace::info(const pal::char_t* format, ...)
{
    if (g_trace_verbosity > 2)
    {
        std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

        va_list args;
        va_start(args, format);
        pal::file_vprintf(g_trace_file, format, args);
        va_end(args);
    }
}

void trace::error(const pal::char_t* format, ...)
{
    std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

    // Always print errors
    va_list args;
    va_start(args, format);

    va_list trace_args;
    va_copy(trace_args, args);

    va_list dup_args;
    va_copy(dup_args, args);
    int count = pal::strlen_vprintf(format, args) + 1;
    std::vector<pal::char_t> buffer(count);
    pal::str_vprintf(&buffer[0], count, format, dup_args);

    if (g_error_writer == nullptr)
    {
        pal::err_fputs(buffer.data());
    }
    else
    {
        g_error_writer(buffer.data());
    }

#if defined(_WIN32)
    ::OutputDebugStringW(buffer.data());
#endif

    if (g_trace_verbosity && ((g_trace_file != stderr) || g_error_writer != nullptr))
    {
        pal::file_vprintf(g_trace_file, format, trace_args);
    }
    va_end(args);
}

void trace::println(const pal::char_t* format, ...)
{
    std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

    va_list args;
    va_start(args, format);
    pal::out_vprintf(format, args);
    va_end(args);
}

void trace::println()
{
    println(_X(""));
}

void trace::warning(const pal::char_t* format, ...)
{
    if (g_trace_verbosity > 1)
    {
        std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

        va_list args;
        va_start(args, format);
        pal::file_vprintf(g_trace_file, format, args);
        va_end(args);
    }
}

void trace::flush()
{
    std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

    pal::file_flush(g_trace_file);
    pal::err_flush();
    pal::out_flush();
}

trace::error_writer_fn trace::set_error_writer(trace::error_writer_fn error_writer)
{
    // No need for locking since g_error_writer is thread local.
    error_writer_fn previous_writer = g_error_writer;
    g_error_writer = error_writer;
    return previous_writer;
}

trace::error_writer_fn trace::get_error_writer()
{
    // No need for locking since g_error_writer is thread local.
    return g_error_writer;
}
