// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "strings.h"

size_t minipal_u16_strlen(const CHAR16_T* str)
{
    size_t len = 0;
    while (*str++)
    {
        len++;
    }
    return len;
}
