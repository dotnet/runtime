// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * C runtime aligned_alloc
 */
PALEXPORT void* SystemNative_AlignedAlloc(uintptr_t alignment, uintptr_t size);

/**
 * Free for C runtime aligned_alloc
 */
PALEXPORT void SystemNative_AlignedFree(void* ptr);

/**
 * Realloc for C runtime aligned_alloc
 */
PALEXPORT void* SystemNative_AlignedRealloc(void* ptr, uintptr_t alignment, uintptr_t size);

/**
 * C runtime calloc
 */
PALEXPORT void* SystemNative_Calloc(uintptr_t num, uintptr_t size);

/**
 * C runtime free
 */
PALEXPORT void SystemNative_Free(void* ptr);

/**
 * Get usable size of C runtime malloc
 */
PALEXPORT uintptr_t SystemNative_GetUsableSize(void* ptr);

/**
 * C runtime malloc
 */
PALEXPORT void* SystemNative_Malloc(uintptr_t size);

/**
 * C runtime memset
 */
PALEXPORT void* SystemNative_MemSet(void* s, int c, uintptr_t n);

/**
 * C runtime realloc
 */
PALEXPORT void* SystemNative_Realloc(void* ptr, uintptr_t new_size);
