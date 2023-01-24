// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "trace.h"
#include <mutex>
#include <thread>

#define TRACE_VERBOSITY_WARN 2
#define TRACE_VERBOSITY_INFO 3
#define TRACE_VERBOSITY_VERBOSE 4

// g_trace_verbosity is used to encode COREHOST_TRACE and COREHOST_TRACE_VERBOSITY to selectively control output of
//    trace::warn(), trace::info(), and trace::verbose()
//  COREHOST_TRACE=0 COREHOST_TRACE_VERBOSITY=N/A        implies g_trace_verbosity = 0.  // Trace "disabled". error() messages will be produced.
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=4 or unset implies g_trace_verbosity = 4.  // Trace "enabled".  verbose(), info(), warn() and error() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=3          implies g_trace_verbosity = 3.  // Trace "enabled".  info(), warn() and error() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=2          implies g_trace_verbosity = 2.  // Trace "enabled".  warn() and error() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=1          implies g_trace_verbosity = 1.  // Trace "enabled".  error() messages will be produced
static int g_trace_verbosity = 0;
static FILE * g_trace_file = nullptr;
thread_local static trace::error_writer_fn g_error_writer = nullptr;

namespace
{
    class spin_lock
    {
    public:
        spin_lock() = default;
        spin_lock(const spin_lock&) = delete;
        spin_lock& operator=(const spin_lock&) = delete;

        void lock()
        {
            uint32_t spin = 0;
            while (flag.test_and_set(std::memory_order_acquire))
            {
                if (spin++ % 1024 == 0)
                    std::this_thread::yield();
            }
        }

        void unlock()
        {
            flag.clear(std::memory_order_release);
        }

    private:
        std::atomic_flag flag = ATOMIC_FLAG_INIT;
    };

    spin_lock g_trace_lock;
}

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
        std::lock_guard<spin_lock> lock(g_trace_lock);

        g_trace_file = stderr;  // Trace to stderr by default
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
            g_trace_verbosity = TRACE_VERBOSITY_VERBOSE;  // Verbose trace by default
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
    if (g_trace_verbosity < TRACE_VERBOSITY_VERBOSE)
        return;

    va_list args;
    va_start(args, format);
    {
        std::lock_guard<spin_lock> lock(g_trace_lock);
        pal::file_vprintf(g_trace_file, format, args);
    }
    va_end(args);
}

void trace::info(const pal::char_t* format, ...)
{
    if (g_trace_verbosity < TRACE_VERBOSITY_INFO)
        return;

    va_list args;
    va_start(args, format);
    {
        std::lock_guard<spin_lock> lock(g_trace_lock);
        pal::file_vprintf(g_trace_file, format, args);
    }
    va_end(args);
}

void trace::error(const pal::char_t* format, ...)
{
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

#if defined(_WIN32)
    ::OutputDebugStringW(buffer.data());
#endif

    {
        std::lock_guard<spin_lock> lock(g_trace_lock);

        if (g_error_writer == nullptr)
        {
            pal::err_fputs(buffer.data());
        }
        else
        {
            g_error_writer(buffer.data());
        }

        if (g_trace_verbosity && ((g_trace_file != stderr) || g_error_writer != nullptr))
        {
            pal::file_vprintf(g_trace_file, format, trace_args);
        }
    }
    va_end(args);
}

void trace::println(const pal::char_t* format, ...)
{
    va_list args;
    va_start(args, format);
    {
        std::lock_guard<spin_lock> lock(g_trace_lock);
        pal::out_vprintf(format, args);
    }
    va_end(args);
}

void trace::println()
{
    println(_X(""));
}

void trace::warning(const pal::char_t* format, ...)
{
    if (g_trace_verbosity < TRACE_VERBOSITY_WARN)
        return;

    va_list args;
    va_start(args, format);
    {
        std::lock_guard<spin_lock> lock(g_trace_lock);
        pal::file_vprintf(g_trace_file, format, args);
    }
    va_end(args);
}

void trace::flush()
{
    if (g_trace_file != nullptr)
    {
        std::lock_guard<spin_lock> lock(g_trace_lock);
        std::fflush(g_trace_file);
    }

    std::fflush(stderr);
    std::fflush(stdout);
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
