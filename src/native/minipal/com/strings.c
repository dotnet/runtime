// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stdlib.h>
#include <stdbool.h>
#include <assert.h>
#include "minipal_com.h"

size_t PAL_wcslen(WCHAR const* str)
{
    assert(str != NULL);

    size_t len = 0;
    while (*str++ != W('\0'))
        len++;
    return len;
}
