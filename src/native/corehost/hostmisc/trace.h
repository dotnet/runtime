// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef TRACE_H
#define TRACE_H

#include "pal.h"
#include <minipal/types.h>

namespace trace
{
    void setup();
    bool enable();
    bool is_enabled();
    void verbose(const pal::char_t* format, ...) MINIPAL_ATTR_FORMAT_PRINTF(1, 2);
    void info(const pal::char_t* format, ...) MINIPAL_ATTR_FORMAT_PRINTF(1, 2);
    void warning(const pal::char_t* format, ...) MINIPAL_ATTR_FORMAT_PRINTF(1, 2);
    void error(const pal::char_t* format, ...) MINIPAL_ATTR_FORMAT_PRINTF(1, 2);
    void println(const pal::char_t* format, ...) MINIPAL_ATTR_FORMAT_PRINTF(1, 2);
    void println();
    void flush();

    typedef void (__cdecl *error_writer_fn)(const pal::char_t* message);

    // Sets a callback which is called whenever error is to be written
    // The setting is per-thread (thread local). If no error writer is set for a given thread
    // the error is written to stderr.
    // The callback is set for the current thread which calls this function.
    // The function returns the previously registered writer for the current thread (or null)
    error_writer_fn set_error_writer(error_writer_fn error_writer);

    // Returns the currently set callback for error writing
    error_writer_fn get_error_writer();
};

#endif // TRACE_H
