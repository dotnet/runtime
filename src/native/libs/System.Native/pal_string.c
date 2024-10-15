// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_string.h"
#include "pal_utilities.h"

#include <assert.h>
#include <stdarg.h>
#include <stdio.h>

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

int32_t SystemNative_SNPrintF_1S(char* string, int32_t size, const char* format, char* str)
{
    return SystemNative_SNPrintF(string, size, format, str);
}

int32_t SystemNative_SNPrintF_1I(char* string, int32_t size, const char* format, int arg)
{
    return SystemNative_SNPrintF(string, size, format, arg);
}
