// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

struct UTimBuf
{
    int64_t AcTime;
    int64_t ModTime;
};

/**
 * Sets the last access and last modified time of a file
 *
 * Returns 0 on success; otherwise, returns -1 and errno is set.
 */
extern "C" int32_t UTime(const char* path, UTimBuf* time);
