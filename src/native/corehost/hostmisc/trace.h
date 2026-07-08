// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef TRACE_H
#define TRACE_H

#include <stdarg.h>
#include <stdbool.h>

#include "pal.h" // for pal_char_t, _X

#ifdef __cplusplus
extern "C" {
#endif

typedef void (__cdecl *trace_error_writer_fn)(const pal_char_t* message);

void trace_setup(void);
bool trace_enable(void);
bool trace_is_enabled(void);
void trace_verbose(const pal_char_t* format, ...);
void trace_info(const pal_char_t* format, ...);
void trace_warning(const pal_char_t* format, ...);
void trace_error(const pal_char_t* format, ...);
void trace_println(const pal_char_t* format, ...);
void trace_println_empty(void);
void trace_flush(void);

// va_list variants used by the C++ inline wrappers below to forward variadic
// arguments without re-implementing the message gating/locking logic.
void trace_verbose_v(const pal_char_t* format, va_list args);
void trace_info_v(const pal_char_t* format, va_list args);
void trace_warning_v(const pal_char_t* format, va_list args);
void trace_error_v(const pal_char_t* format, va_list args);
void trace_println_v(const pal_char_t* format, va_list args);

// Sets a callback which is called whenever an error is to be written.
// The setting is per-thread (thread local). If no error writer is set for a
// given thread the error is written to stderr. The callback is set for the
// current thread which calls this function. Returns the previously registered
// writer for the current thread (or NULL).
trace_error_writer_fn trace_set_error_writer(trace_error_writer_fn error_writer);

// Returns the currently set callback for error writing.
trace_error_writer_fn trace_get_error_writer(void);

#ifdef __cplusplus
}
#endif

// ============================================================================
// C++ section: source-compat shim for existing trace::* callers.
// ============================================================================
//
// The trace namespace provides C++ wrappers around the C implementation in
// trace.c. pal::char_t and pal_char_t are the same type (wchar_t on Windows,
// char on Unix), so the C functions can be called directly. The va_list
// versions (trace_*_v) are used to forward variadic arguments from these
// inline wrappers.

#ifdef __cplusplus

namespace trace
{
    inline void setup() { trace_setup(); }
    inline bool enable() { return trace_enable(); }
    inline bool is_enabled() { return trace_is_enabled(); }

    inline void verbose(const pal::char_t* format, ...)
    {
        va_list args;
        va_start(args, format);
        trace_verbose_v(format, args);
        va_end(args);
    }

    inline void info(const pal::char_t* format, ...)
    {
        va_list args;
        va_start(args, format);
        trace_info_v(format, args);
        va_end(args);
    }

    inline void warning(const pal::char_t* format, ...)
    {
        va_list args;
        va_start(args, format);
        trace_warning_v(format, args);
        va_end(args);
    }

    inline void error(const pal::char_t* format, ...)
    {
        va_list args;
        va_start(args, format);
        trace_error_v(format, args);
        va_end(args);
    }

    inline void println(const pal::char_t* format, ...)
    {
        va_list args;
        va_start(args, format);
        trace_println_v(format, args);
        va_end(args);
    }

    inline void println() { trace_println_empty(); }
    inline void flush() { trace_flush(); }

    typedef trace_error_writer_fn error_writer_fn;

    inline error_writer_fn set_error_writer(error_writer_fn error_writer)
    {
        return trace_set_error_writer(error_writer);
    }

    inline error_writer_fn get_error_writer()
    {
        return trace_get_error_writer();
    }
}

#endif // __cplusplus

#endif // TRACE_H
