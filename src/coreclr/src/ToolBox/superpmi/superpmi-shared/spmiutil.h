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

extern bool breakOnDebugBreakorAV;

extern void DebugBreakorAV(int val); // Global(ish) error handler

extern char* GetEnvironmentVariableWithDefaultA(const char* envVarName, const char* defaultValue = nullptr);

extern WCHAR* GetEnvironmentVariableWithDefaultW(const WCHAR* envVarName, const WCHAR* defaultValue = nullptr);

#ifdef FEATURE_PAL
extern LPSTR GetCommandLineA();
#endif // FEATURE_PAL

#endif // !_SPMIUtil
