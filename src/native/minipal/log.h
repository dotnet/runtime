// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_LOG_H
#define HAVE_MINIPAL_LOG_H

#include <minipal/types.h>
#include <stdbool.h>
#include <stdarg.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef enum
{
    minipal_log_flags_fatal = 1 << 1,
    minipal_log_flags_error = 1 << 2,
    minipal_log_flags_warning = 1 << 3,
    minipal_log_flags_info = 1 << 4,
    minipal_log_flags_debug = 1 << 5,
    minipal_log_flags_verbose = 1 << 6
} minipal_log_flags;

/**
 * Print a formatted message to the log: logcat on Android, otherwise stderr for fatal and error
 * messages and stdout for the others.
 *
 * @param flags The severity of the message.
 * @param fmt The printf format string.
 * @param ... The format arguments.
 * @return The number of bytes written. On non-Android platforms, a negative value indicates failure.
 *         On Android, failures can result in zero or a partial byte count.
 */
#define minipal_log_print_fatal(...) minipal_log_print(minipal_log_flags_fatal, __VA_ARGS__)
#define minipal_log_print_error(...) minipal_log_print(minipal_log_flags_error, __VA_ARGS__)
#define minipal_log_print_info(...) minipal_log_print(minipal_log_flags_info, __VA_ARGS__)
#define minipal_log_print_verbose(...) minipal_log_print(minipal_log_flags_verbose, __VA_ARGS__)
int minipal_log_print(minipal_log_flags flags, const char* fmt, ... ) MINIPAL_ATTR_FORMAT_PRINTF(2, 3);

/**
 * Print a formatted message to the log, as minipal_log_print does.
 *
 * @param flags The severity of the message.
 * @param fmt The printf format string.
 * @param args The format arguments.
 * @return The number of bytes written. On non-Android platforms, a negative value indicates failure.
 *         On Android, failures can result in zero or a partial byte count.
 */
#define minipal_log_vprint_fatal(...) minipal_log_vprint(minipal_log_flags_fatal, __VA_ARGS__)
#define minipal_log_vprint_error(...) minipal_log_vprint(minipal_log_flags_error, __VA_ARGS__)
#define minipal_log_vprint_info(...) minipal_log_vprint(minipal_log_flags_info, __VA_ARGS__)
#define minipal_log_vprint_verbose(...) minipal_log_vprint(minipal_log_flags_verbose, __VA_ARGS__)
int minipal_log_vprint(minipal_log_flags flags, const char* fmt,va_list args);

/**
 * Flush the C runtime buffers of the log stream for the severity. Does nothing on Android.
 *
 * @param flags The severity.
 */
#define minipal_log_flush_fatal() minipal_log_flush(minipal_log_flags_fatal)
#define minipal_log_flush_error() minipal_log_flush(minipal_log_flags_error)
#define minipal_log_flush_info() minipal_log_flush(minipal_log_flags_info)
#define minipal_log_flush_verbose() minipal_log_flush(minipal_log_flags_verbose)
void minipal_log_flush(minipal_log_flags flags);

/**
 * Flush the C runtime buffers of the stderr and stdout log streams. Does nothing on Android.
 */
void minipal_log_flush_all(void);

/**
 * Write a message to the log without the C runtime, so that it is async-signal-safe.
 *
 * @param flags The severity of the message.
 * @param msg The null-terminated message.
 * @return The number of bytes written.
 */
#define minipal_log_write_fatal(msg) minipal_log_write(minipal_log_flags_fatal, msg)
#define minipal_log_write_error(msg) minipal_log_write(minipal_log_flags_error, msg)
#define minipal_log_write_info(msg) minipal_log_write(minipal_log_flags_info, msg)
#define minipal_log_write_verbose(msg) minipal_log_write(minipal_log_flags_verbose, msg)
int minipal_log_write(minipal_log_flags flags, const char* msg);

/**
 * Commit the log output for the severity to storage, without the C runtime. Does nothing on Android.
 *
 * @param flags The severity.
 */
#define minipal_log_sync_fatal() minipal_log_sync(minipal_log_flags_fatal)
#define minipal_log_sync_error() minipal_log_sync(minipal_log_flags_error)
#define minipal_log_sync_info() minipal_log_sync(minipal_log_flags_info)
#define minipal_log_sync_verbose() minipal_log_sync(minipal_log_flags_verbose)
void minipal_log_sync(minipal_log_flags flags);

/**
 * Commit the stderr and stdout log output to storage, without the C runtime. Does nothing on Android.
 */
void minipal_log_sync_all(void);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // HAVE_MINIPAL_LOG_H
