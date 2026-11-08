// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PROCESSENVIRONMENT_H__
#define __PROCESSENVIRONMENT_H__

#include <minipal/types.h>
#include <stdint.h>

// The runtime's view of the process environment. On Unix, this is a lock-protected copy of the
// environment because setenv is not safe to call while other threads may be reading the environment.
// It exists so that changes made through ICorProfilerInfo11::SetEnvironmentVariable and the diagnostic
// server are visible to managed code and runtime configuration, and vice versa. Do not add other uses.

// Same contract as GetEnvironmentVariableW.
uint32_t ProcessEnvironment_GetVariable(const CHAR16_T* name, CHAR16_T* buffer, uint32_t bufferSize);

// Sets the variable, or unsets it if value is null. Returns false when unsetting a variable that is not set.
bool ProcessEnvironment_SetVariable(const CHAR16_T* name, const CHAR16_T* value);

// Returns a double-null-terminated block of "name=value" strings, in the same layout as
// GetEnvironmentStringsW. Free it with ProcessEnvironment_FreeStrings.
CHAR16_T* ProcessEnvironment_GetStrings();
void ProcessEnvironment_FreeStrings(CHAR16_T* strings);

#ifndef TARGET_WINDOWS
// Same contract as GetEnvironmentVariableA.
uint32_t ProcessEnvironment_GetVariableUtf8(const char* name, char* buffer, uint32_t bufferSize);

// Returns the environment to pass to execve for a child process. It is not synchronized
// and does not allocate, so it is meant to be called between fork and exec.
char** ProcessEnvironment_GetEnviron();
#endif // !TARGET_WINDOWS

#endif // __PROCESSENVIRONMENT_H__
