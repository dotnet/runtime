// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdint.h>

// Returns a malloc-allocated UTF-8 error, or NULL on success.
char* SystemJS_ConsoleWrite(const uint8_t* buffer, int32_t bufferLength, int32_t isError);
