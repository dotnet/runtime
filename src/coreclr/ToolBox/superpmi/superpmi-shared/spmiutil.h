//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//----------------------------------------------------------
// SPMIUtil.h - General utility functions
//----------------------------------------------------------
#ifndef _SPMIUtil
#define _SPMIUtil

#include "methodcontext.h"

bool BreakOnDebugBreakorAV();
void SetBreakOnDebugBreakOrAV(bool value);

void DebugBreakorAV(int val); // Global(ish) error handler

char* GetEnvironmentVariableWithDefaultA(const char* envVarName, const char* defaultValue = nullptr);

WCHAR* GetEnvironmentVariableWithDefaultW(const WCHAR* envVarName, const WCHAR* defaultValue = nullptr);

#ifdef TARGET_UNIX
LPSTR GetCommandLineA();
#endif // TARGET_UNIX

bool LoadRealJitLib(HMODULE& realJit, WCHAR* realJitPath);

WCHAR* GetResultFileName(const WCHAR* folderPath, const WCHAR* fileName, const WCHAR* extension);

// SuperPMI stores handles as unsigned 64-bit integers, no matter the platform the collection happens on
// (32 or 64 bit). Handles are defined as pointers. We need to be careful when converting from a handle
// to an int to ensure we don't sign extend the pointer, which is the behavior of some compilers.
// First cast the pointer to an unsigned integer the same size as the pointer, then, if the pointer is
// 32-bits, zero extend it to 64-bits.

template <typename T>
inline DWORDLONG CastHandle(T h)
{
    return (DWORDLONG)(uintptr_t)h;
}

// Basically the same thing, but variables/fields declared as for pointer types.
template <typename T>
inline DWORDLONG CastPointer(T* p)
{
    return (DWORDLONG)(uintptr_t)p;
}

#endif // !_SPMIUtil
