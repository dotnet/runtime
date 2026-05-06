// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

PALEXPORT void* SystemNative_LoadLibrary(const char* filename);

PALEXPORT void* SystemNative_GetLoadLibraryError(void);

PALEXPORT void* SystemNative_GetProcAddress(void* handle, const char* symbol);

PALEXPORT void SystemNative_FreeLibrary(void* handle);

PALEXPORT void* SystemNative_GetDefaultSearchOrderPseudoHandle(void);
