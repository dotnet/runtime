// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Functions based on OpenSSL 3.0 API, used when building against/running with older versions.

#pragma once
#include "pal_types.h"

// For 3.0 to behave like previous versions.
void local_ERR_put_error(int32_t lib, int32_t func, int32_t reason, const char* file, int32_t line);
