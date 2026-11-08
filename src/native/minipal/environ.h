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
 * Get an environment variable (UTF-8).
 * Same return-value contract as Win32 GetEnvironmentVariableA:
 * - not found: 0
 * - fits in buffer: length excluding null
 * - does not fit: required size including null
 */
uint32_t minipal_environ_get_variable(const char* name, char* buffer, uint32_t buffer_size);

/**
 * Get an environment variable (UTF-16). Same contract as GetEnvironmentVariableW.
 */
uint32_t minipal_environ_get_variable_u16(const CHAR16_T* name, CHAR16_T* buffer, uint32_t buffer_size);

/**
 * Get a malloc'd copy of the variable value (UTF-8). Caller frees with free().
 * Returns NULL if the variable is not set.
 */
char* minipal_environ_getenv(const char* name);

/**
 * Snapshot the process environment as a double-null-terminated UTF-16 block
 * (same layout as GetEnvironmentStringsW). Caller frees with
 * minipal_environ_free_strings_u16.
 */
CHAR16_T* minipal_environ_get_strings_u16(void);

/**
 * Free a block returned by minipal_environ_get_strings_u16.
 */
void minipal_environ_free_strings_u16(void* strings);

/**
 * The process environment array ("NAME=VALUE" strings, NULL-terminated), for example
 * to pass to execve. Do not modify or free. Returns NULL on Windows.
 */
char** minipal_environ_get(void);

#ifdef __cplusplus
}
#endif

#endif /* HAVE_MINIPAL_ENVIRON_H */
