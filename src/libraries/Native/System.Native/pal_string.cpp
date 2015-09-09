// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_string.h"

#include <cstdarg>
#include <stdio.h>
#include <string.h>

extern "C"
int32_t SNPrintF(char* string, uint64_t size, const char* format, ...)
{
    va_list arguments;
    va_start(arguments, format);
    int result = vsnprintf(string, size, format, arguments);
    va_end(arguments);
    return result;
}
