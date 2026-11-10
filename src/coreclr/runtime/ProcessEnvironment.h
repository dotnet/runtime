// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PROCESSENVIRONMENT_H__
#define __PROCESSENVIRONMENT_H__

#include <minipal/types.h>
#include <stdint.h>

// The runtime's view of the process environment. On Unix, setting a variable switches to a lock-protected
// copy of the environment because setenv is not safe to call while other threads may be reading it.
// It exists so that changes made through ICorProfilerInfo11::SetEnvironmentVariable and the diagnostic
// server are visible to managed code and runtime configuration, and vice versa. Do not add other uses.

// Returns false if the variable is not set. Otherwise, sets *length to the length of the value if it fits
// in the buffer, or to the required buffer size including the null terminator if it does not.
bool ProcessEnvironment_TryGetVariable(const CHAR16_T* name, CHAR16_T* buffer, uint32_t bufferSize, uint32_t* length);

// Same contract as GetEnvironmentVariableW.
uint32_t ProcessEnvironment_GetVariable(const CHAR16_T* name, CHAR16_T* buffer, uint32_t bufferSize);

// Sets the variable, or unsets it if value is null. Returns false when unsetting a variable that is not set.
bool ProcessEnvironment_SetVariable(const CHAR16_T* name, const CHAR16_T* value);

// Returns a double-null-terminated block of "name=value" strings, in the same layout as
// GetEnvironmentStringsW. Free it with ProcessEnvironment_FreeStrings.
CHAR16_T* ProcessEnvironment_GetStrings();
void ProcessEnvironment_FreeStrings(CHAR16_T* strings);

#ifndef TARGET_WINDOWS
// UTF-8 versions of the functions above.
bool ProcessEnvironment_TryGetVariableUtf8(const char* name, char* buffer, uint32_t bufferSize, uint32_t* length);

// Returns 0 on success, ENOENT when unsetting a variable that is not set, EINVAL for an invalid name,
// or ENOMEM when out of memory.
int ProcessEnvironment_SetVariableUtf8(const char* name, const char* value);
char* ProcessEnvironment_GetStringsUtf8();
void ProcessEnvironment_FreeStringsUtf8(char* strings);

// Same contract as GetEnvironmentVariableA.
uint32_t ProcessEnvironment_GetVariableUtf8(const char* name, char* buffer, uint32_t bufferSize);

// Returns a copy of the value, or null if the variable is not set. Free it with free().
char* ProcessEnvironment_GetVariableCopyUtf8(const char* name);
#endif // !TARGET_WINDOWS

#endif // __PROCESSENVIRONMENT_H__
