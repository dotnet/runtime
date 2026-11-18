// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "console.h"

extern char* SystemJS_ConsoleWriteJS(const uint8_t* buffer, int32_t bufferLength, int32_t isError);

char* SystemJS_ConsoleWrite(const uint8_t* buffer, int32_t bufferLength, int32_t isError)
{
    return SystemJS_ConsoleWriteJS(buffer, bufferLength, isError);
}
