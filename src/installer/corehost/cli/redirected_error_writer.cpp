// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "redirected_error_writer.h"

namespace
{
    pal::stringstream_t & get_redirected_error_stream()
    {
        thread_local static pal::stringstream_t comhost_errors;

        return comhost_errors;
    }
}

void reset_redirected_error_writer()
{
    pal::stringstream_t newstream;
    get_redirected_error_stream().swap(newstream);
}

void __cdecl redirected_error_writer(const pal::char_t* msg)
{
    get_redirected_error_stream() << msg;
}

pal::string_t get_redirected_error_string()
{
    return get_redirected_error_stream().str();
}
