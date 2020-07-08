// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * snprintf is difficult to represent in C# due to the argument list, so the C# PInvoke
 * layer will have multiple overloads pointing to this function
 *
 * Returns the number of characters (excluding null terminator) written to the buffer on
 * success; if the return value is equal to the size then the result may have been truncated.
 * On failure, returns a negative value.
 */
PALEXPORT int32_t SystemNative_SNPrintF(char* string, int32_t size, const char* format, ...);
