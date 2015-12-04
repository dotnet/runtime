// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_err.h"
#include "pal_utilities.h"

#include <openssl/err.h>

extern "C" uint64_t ErrGetError()
{
    return ERR_get_error();
}

extern "C" uint64_t ErrGetErrorAlloc(int32_t* isAllocFailure)
{
    unsigned long err = ERR_get_error();

    if (isAllocFailure)
    {
        *isAllocFailure = ERR_GET_REASON(err) == ERR_R_MALLOC_FAILURE;
    }

    return err;
}

extern "C" const char* ErrReasonErrorString(uint64_t error)
{
    return ERR_reason_error_string(static_cast<unsigned long>(error));
}

extern "C" void ErrErrorStringN(uint64_t e, char* buf, int32_t len)
{
    ERR_error_string_n(static_cast<unsigned long>(e), buf, UnsignedCast(len));
}
