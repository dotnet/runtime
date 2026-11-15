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

// Errors returned by ProcessEnvironment_SetVariable. These are the Windows error codes so that callers
// can convert them with HRESULT_FROM_WIN32.
#define PROCESSENVIRONMENT_ERROR_NOT_ENOUGH_MEMORY 8L
#define PROCESSENVIRONMENT_ERROR_INVALID_PARAMETER 87L
#define PROCESSENVIRONMENT_ERROR_ENVVAR_NOT_FOUND 203L

// Same contract as GetEnvironmentVariableW.
uint32_t ProcessEnvironment_GetVariable(const CHAR16_T* name, CHAR16_T* buffer, uint32_t bufferSize);

// Sets the variable, or unsets it if value is null. Returns 0 on success, or one of the errors above;
// PROCESSENVIRONMENT_ERROR_ENVVAR_NOT_FOUND is returned when unsetting a variable that is not set.
uint32_t ProcessEnvironment_SetVariable(const CHAR16_T* name, const CHAR16_T* value);

#ifndef TARGET_WINDOWS
// Same contract as GetEnvironmentVariableA.
uint32_t ProcessEnvironment_GetVariableUtf8(const char* name, char* buffer, uint32_t bufferSize);

// Calls callback with each "name=value" entry. The environment cannot change during the enumeration,
// so the callback must not get or set environment variables.
typedef void (*ProcessEnvironmentEntryCallback)(const char* entry, void* context);
void ProcessEnvironment_EnumerateVariablesUtf8(ProcessEnvironmentEntryCallback callback, void* context);

// QCalls for System.Environment.
extern "C" const char* Environment_GetEnvironmentVariable(const char* name, int32_t* needsFree);
extern "C" int32_t Environment_SetEnvironmentVariable(const char* name, const char* value);
extern "C" char** Environment_GetEnviron(int32_t* needsFree);
#endif // !TARGET_WINDOWS

#endif // __PROCESSENVIRONMENT_H__
