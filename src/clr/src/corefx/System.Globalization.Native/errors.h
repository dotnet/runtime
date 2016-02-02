// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <unicode/utypes.h>

/*
* These values should be kept in sync with
* Interop.GlobalizationInterop.ResultCode
*/
enum ResultCode : int32_t
{
    Success = 0,
    UnknownError = 1,
    InsufficentBuffer = 2,
};

/*
Converts a UErrorCode to a ResultCode.
*/
static ResultCode GetResultCode(UErrorCode err)
{
    if (err == U_BUFFER_OVERFLOW_ERROR || err == U_STRING_NOT_TERMINATED_WARNING)
    {
        return InsufficentBuffer;
    }

    if (U_SUCCESS(err))
    {
        return Success;
    }

    return UnknownError;
}
