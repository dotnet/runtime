// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

/*
 * Window Size of the terminal
*/
struct WinSize
{
    uint16_t Row;
    uint16_t Col;
    uint16_t XPixel;
    uint16_t YPixel;
};

/**
 * Gets the windows size of the terminal
 *
 * Returns 0 on success; otherwise, returns errorNo.
 */
extern "C" int32_t GetWindowSize(WinSize* windowsSize);

/**
 * Gets whether the specified file descriptor is for a terminal.
 *
 * Returns 1 if the file descriptor is referring to a terminal;
 * otherwise returns 0 and sets errno.
 */
extern "C" int32_t IsATty(int filedes);

/**
* Reads the number of bytes specified into the provided buffer from stdin.
* in a non-echo and non-canonical mode.
* Returns the number of bytes read on success; otherwise, -1 is returned an errno is set.
*/
extern "C" int32_t ReadStdinUnbuffered(void* buffer, int32_t bufferSize);
