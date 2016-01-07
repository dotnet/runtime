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
extern "C" int32_t SystemNative_UTime(const char* path, UTimBuf* time);

/**
 * Gets the resolution of the timestamp, in counts per second.
 *
 * Returns 1 on success; otherwise, 0 on failure.
 */
extern "C" int32_t SystemNative_GetTimestampResolution(uint64_t* resolution);

/**
 * Gets a high-resolution timestamp that can be used for time-interval measurements.
 *
 * Returns 1 on success; otherwise, 0 on failure.
 */
extern "C" int32_t SystemNative_GetTimestamp(uint64_t* timestamp);
