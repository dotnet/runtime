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

#endif // !_SPMIUtil
