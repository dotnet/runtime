// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"
#include "pal_errno.h"

PALEXPORT int32_t SystemNative_Sysctl(int* name, unsigned int namelen, void* value, size_t* len);
