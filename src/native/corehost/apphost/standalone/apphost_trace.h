// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef APPHOST_TRACE_H
#define APPHOST_TRACE_H

#include <stdarg.h>
#include <stdbool.h>

typedef void (*trace_error_writer_fn)(const char* message);

void trace_setup(void);
bool trace_enable(void);
bool trace_is_enabled(void);
void trace_verbose(const char* format, ...);
void trace_info(const char* format, ...);
void trace_warning(const char* format, ...);
void trace_error(const char* format, ...);
void trace_println(const char* format, ...);
void trace_println_empty(void);
void trace_flush(void);

trace_error_writer_fn trace_set_error_writer(trace_error_writer_fn error_writer);
trace_error_writer_fn trace_get_error_writer(void);

#endif // APPHOST_TRACE_H
