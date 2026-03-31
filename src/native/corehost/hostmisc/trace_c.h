// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef TRACE_C_H
#define TRACE_C_H

#include <stdarg.h>
#include <stdbool.h>

#if defined(_WIN32)
typedef wchar_t pal_char_t;
#define _TRACE_X(s) L ## s
#else
typedef char pal_char_t;
#define _TRACE_X(s) s
#endif

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

// va_list versions for forwarding from C++ wrappers
void trace_verbose_v(const pal_char_t* format, va_list args);
void trace_info_v(const pal_char_t* format, va_list args);
void trace_warning_v(const pal_char_t* format, va_list args);
void trace_error_v(const pal_char_t* format, va_list args);
void trace_println_v(const pal_char_t* format, va_list args);

trace_error_writer_fn trace_set_error_writer(trace_error_writer_fn error_writer);
trace_error_writer_fn trace_get_error_writer(void);

#ifdef __cplusplus
}
#endif

#endif // TRACE_C_H
