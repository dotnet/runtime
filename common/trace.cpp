// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"

static bool g_enabled = false;

void trace::enable()
{
    g_enabled = true;
}

bool trace::is_enabled()
{
    return g_enabled;
}

void trace::verbose(const pal::char_t* format, ...)
{
    if (g_enabled)
    {
        va_list args;
        va_start(args, format);
        pal::err_vprintf(format, args);
        va_end(args);
    }
}

void trace::info(const pal::char_t* format, ...)
{
    if (g_enabled)
    {
        va_list args;
        va_start(args, format);
        pal::err_vprintf(format, args);
        va_end(args);
    }
}

void trace::error(const pal::char_t* format, ...)
{
    // Always print errors
    va_list args;
    va_start(args, format);
    pal::err_vprintf(format, args);
    va_end(args);
}

void trace::warning(const pal::char_t* format, ...)
{
    if (g_enabled)
    {
        va_list args;
        va_start(args, format);
        pal::err_vprintf(format, args);
        va_end(args);
    }
}
