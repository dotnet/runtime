// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_icushim.h"
#include "pal_compiler.h"

/*
* These values should be kept in sync with
* Interop.GlobalizationInterop.ResultCode
*/
typedef enum
{
    Success = 0,
    UnknownError = 1,
    InsufficentBuffer = 2,
    OutOfMemory = 3
} ResultCode;
