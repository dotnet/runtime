// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

/**
 * Fills memory with a set byte. Implemented as shim to memset(3).
 *
 * Returns a pointer to the memory.
 */
extern "C" void* SystemNative_MemSet(void *s, int c, uintptr_t n);
