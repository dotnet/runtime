// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_memory.h"
#include <string.h>

extern "C" void* SystemNative_MemSet(void *s, int c, uintptr_t n)
{
    return memset(s, c, static_cast<size_t>(n));
}
