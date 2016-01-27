// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"

/*
Gets the curl version information.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t HttpNative_GetCurlVersionInfo(int32_t* age,
                                                 int32_t* supportsSsl,
                                                 int32_t* supportsAutoDecompression,
                                                 int32_t* supportsHttp2Multiplexing);
