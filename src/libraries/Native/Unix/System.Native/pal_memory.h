// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * Fills memory with a set byte. Implemented as shim to memset(3).
 *
 * Returns a pointer to the memory.
 */
PALEXPORT void* SystemNative_MemSet(void *s, int c, uintptr_t n);
