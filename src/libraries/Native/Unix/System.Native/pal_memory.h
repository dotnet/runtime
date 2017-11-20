// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"

BEGIN_EXTERN_C

#include "pal_types.h"

/**
 * Fills memory with a set byte. Implemented as shim to memset(3).
 *
 * Returns a pointer to the memory.
 */
void* SystemNative_MemSet(void *s, int c, uintptr_t n);

END_EXTERN_C
