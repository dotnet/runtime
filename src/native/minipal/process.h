// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_PROCESS_H
#define HAVE_MINIPAL_PROCESS_H

#include <stdbool.h>
#include <stdint.h>
#include <minipal/types.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct
{
    intptr_t process_handle; // Caller must close via minipal_close_process_handle.
    intptr_t thread_handle;  // Caller must close via minipal_close_process_handle. Always 0 on Unix.
    int32_t process_id;
} minipal_process_info;

/**
 * Launch a child process from a UTF-8 command line.
 *
 * @param command_line   The command line to execute (UTF-8, null-terminated).
 * @param working_dir    Working directory for the child process, or NULL to inherit.
 * @param inherit_handles Whether the child inherits parent's handles.
 * @param out_info       Optional. Receives process handle/PID on success. Caller must
 *                       close process_handle and thread_handle when done.
 * @return true on success, false on failure.
 */
bool minipal_create_process(
    const char* command_line,
    const char* working_dir,
    bool inherit_handles,
    minipal_process_info* out_info);

/**
 * Launch a child process from a UTF-16 command line.
 * Handles the UTF-16 to UTF-8 conversion internally on Unix.
 * On Windows, passes through to CreateProcessW directly.
 *
 * @param command_line   The command line to execute (UTF-16/CHAR16_T, null-terminated).
 * @param working_dir    Working directory for the child process, or NULL to inherit.
 * @param inherit_handles Whether the child inherits parent's handles.
 * @param out_info       Optional. Receives process handle/PID on success. Caller must
 *                       close process_handle and thread_handle when done.
 * @return true on success, false on failure.
 */
bool minipal_create_process_w(
    const CHAR16_T* command_line,
    const CHAR16_T* working_dir,
    bool inherit_handles,
    minipal_process_info* out_info);

/**
 * Close a process or thread handle returned by minipal_create_process.
 *
 * @param handle The handle to close. 0 is a no-op.
 */
void minipal_close_process_handle(intptr_t handle);

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_PROCESS_H
