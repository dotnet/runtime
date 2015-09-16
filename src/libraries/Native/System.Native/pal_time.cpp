// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_time.h"

#include <assert.h>
#include <utime.h>

static void ConvertUTimBuf(const UTimBuf& pal, utimbuf& native)
{
    native.actime = pal.AcTime;
    native.modtime = pal.ModTime;
}

extern "C" int32_t UTime(const char* path, UTimBuf* times)
{
    assert(times != nullptr);

    utimbuf temp;
    ConvertUTimBuf(*times, temp);
    return utime(path, &temp);
}
