// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * C runtime malloc
 */
PALEXPORT void* SystemNative_MemAlloc(uintptr_t size);

/**
 * C runtime realloc
 */
PALEXPORT void* SystemNative_MemReAlloc(void* ptr, uintptr_t size);

/**
 * C runtime free
 */
PALEXPORT void SystemNative_MemFree(void* ptr);

/**
 * C runtime memset
 */
PALEXPORT void* SystemNative_MemSet(void* s, int c, uintptr_t n);
