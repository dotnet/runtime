// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "debug_stream.h"
#include <minipal/utils.h>
#include <stdio.h>

namespace
{
    data_stream_context_t g_data_streams;
}

bool debug_stream::init()
{
    size_t sizes[] = { 4096, 8192, 2048 };
    if (!dnds_init(&g_data_streams, ARRAY_SIZE(sizes), sizes))
        return false;

    return true;
}
