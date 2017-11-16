// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"
#include "pal_errno.h"


extern "C" int32_t SystemNative_Sysctl(int* name, unsigned int namelen, void* value, size_t* len);

