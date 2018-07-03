// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_string.h"
#include "pal_utilities.h"

#include <assert.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>

int32_t SystemNative_SNPrintF(char* string, int32_t size, const char* format, ...)
{
    assert(string != NULL || size == 0);
    assert(size >= 0);
    assert(format != NULL);

    if (size < 0)
        return -1;

    va_list arguments;
    va_start(arguments, format);
    int result = vsnprintf(string, Int32ToSizeT(size), format, arguments);
    va_end(arguments);
    return result;
}

int32_t SystemNative_PrintF(const char* format, ...)
{
    va_list arguments;
    va_start(arguments, format);
    int result = vprintf(format, arguments);
    va_end(arguments);
    return result;
}
