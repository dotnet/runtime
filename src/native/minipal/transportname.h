// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_TRANSPORTNAME_H
#define HAVE_MINIPAL_TRANSPORTNAME_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifdef __APPLE__
// Max semaphore length on Mac is 31 characters. In a sandbox, IPC names are prefixed with an
// application group ID, e.g. ABCDEFGHIJ.MS (10 characters of developer ID and 2 of group name).
#define MINIPAL_MAX_APPLICATION_GROUP_ID_LENGTH 13
#endif

/**
 * Get a value that distinguishes processes that had the same pid at different times, based on the
 * process start time. It means different things on different platforms; use it only for IPC names.
 *
 * @param pid The process id.
 * @param key Receives the value, or 0 on failure, so that both sides of the IPC agree on it.
 * @return true on success, false otherwise.
 */
bool minipal_get_process_id_disambiguation_key(uint32_t pid, uint64_t* key);

/**
 * Build an IPC transport name in the temporary directory (or, on Apple, the application group container):
 * <dir>/<prefix>-<pid>-<disambiguation key>-<suffix>. These names are a contract with dbgshim and the
 * diagnostics tools.
 *
 * @param name Receives the name, or an empty string on failure.
 * @param nameLength Size of the name buffer, including the terminator.
 * @param prefix Name prefix, e.g. "dotnet-diagnostic".
 * @param pid The process id.
 * @param applicationGroupId Apple application group ID, or NULL.
 * @param suffix Name suffix, e.g. "socket".
 * @return true on success, false otherwise.
 */
bool minipal_get_transport_name(char* name, size_t nameLength, const char* prefix, uint32_t pid, const char* applicationGroupId, const char* suffix);

#ifdef __cplusplus
}
#endif

#endif // HAVE_MINIPAL_TRANSPORTNAME_H
