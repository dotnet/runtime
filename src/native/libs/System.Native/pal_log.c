// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_log.h"

#include <stdio.h>

void SystemNative_Log(uint8_t* buffer, int32_t length)
{
    fwrite(buffer, 1, (size_t)length, stdout);
    fflush(stdout);
}

void SystemNative_LogError(uint8_t* buffer, int32_t length)
{
    fwrite(buffer, 1, (size_t)length, stderr);
    fflush(stderr);
}
