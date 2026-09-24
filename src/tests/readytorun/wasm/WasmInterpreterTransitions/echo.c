// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>

extern int32_t managed_echo(int32_t value);

int32_t echo(int32_t value)
{
    return managed_echo(value);
}
