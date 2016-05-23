// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"

static bool g_enabled = false;

//
// Turn on tracing for the corehost based on "COREHOST_TRACE" env.
//
void trace::setup()
{
    // Read trace environment variable
    pal::string_t trace_str;
    if (!pal::getenv(_X("COREHOST_TRACE"), &trace_str))
    {
        return;
    }

    auto trace_val = pal::xtoi(trace_str.c_str());
    if (trace_val > 0)
    {
        trace::enable();
        trace::info(_X("Tracing enabled"));
    }
}

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

void trace::println(const pal::char_t* format, ...)
{
    va_list args;
    va_start(args, format);
    pal::out_vprintf(format, args);
    va_end(args);
}

void trace::println()
{
    println(_X(""));
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
