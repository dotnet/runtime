// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_icushim_internal.h"
#include "pal_errors.h"

/*
Converts a UErrorCode to a ResultCode.
*/
static ResultCode GetResultCode(UErrorCode err)
{
    if (err == U_BUFFER_OVERFLOW_ERROR || err == U_STRING_NOT_TERMINATED_WARNING)
    {
        return InsufficientBuffer;
    }

    if (err == U_MEMORY_ALLOCATION_ERROR)
    {
        return OutOfMemory;
    }

    if (U_SUCCESS(err))
    {
        return Success;
    }

    return UnknownError;
}
