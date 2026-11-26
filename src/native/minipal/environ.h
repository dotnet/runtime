// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_ENVIRON_H
#define HAVE_MINIPAL_ENVIRON_H

#include <minipal/types.h>
#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Get an environment variable (narrow string; ANSI code page on Windows).
 * Same return-value contract as Win32 GetEnvironmentVariableA.
 *
 * @param name The name of the variable, which must be non-empty and must not contain '='.
 * @param buffer Receives the value.
 * @param buffer_size Size of the buffer, including the null terminator.
 * @return 0 if not found; the length excluding null if the value fits in the buffer;
 *         otherwise the required size including null.
 */
uint32_t minipal_environ_get_variable(const char* name, char* buffer, uint32_t buffer_size);

/**
 * Get an environment variable (UTF-16). Same contract as GetEnvironmentVariableW.
 *
 * @param name The name of the variable.
 * @param buffer Receives the value.
 * @param buffer_size Size of the buffer in characters, including the null terminator.
 * @return 0 if not found; the length excluding null if the value fits in the buffer;
 *         otherwise the required size including null.
 */
uint32_t minipal_environ_get_variable_u16(const CHAR16_T* name, CHAR16_T* buffer, uint32_t buffer_size);

#ifndef HOST_WINDOWS
/**
 * Get the process environment array, for example to pass to execve. Do not modify or free.
 *
 * @return The "NAME=VALUE" strings, NULL-terminated.
 */
char** minipal_environ_get(void);
#endif

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_ENVIRON_H
