// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
