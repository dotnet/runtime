// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_memory.h"
#include <string.h>

void* SystemNative_MemSet(void *s, int c, uintptr_t n)
{
    return memset(s, c, (size_t)n);
}
