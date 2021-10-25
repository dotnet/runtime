// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_err.h"

void CryptoNative_ErrClearError()
{
}

uint64_t CryptoNative_ErrGetErrorAlloc(int32_t* isAllocFailure)
{
    return 0;
}

uint64_t CryptoNative_ErrPeekError()
{
    return 0;
}

uint64_t CryptoNative_ErrPeekLastError()
{
    return 0;
}

const char* CryptoNative_ErrReasonErrorString(uint64_t error)
{
    return "See logcat for more details.";
}

static const char see_logcat_message[] = "See logcat for more details.";
#define SEE_LOGCAT_MESSAGE_LEN (int32_t)(sizeof(see_logcat_message))

void CryptoNative_ErrErrorStringN(uint64_t e, char* buf, int32_t len)
{
    abort_if_invalid_pointer_argument (buf);
    if (len <= 0) {
        return;
    }

    memcpy (buf, see_logcat_message, len < SEE_LOGCAT_MESSAGE_LEN ? (size_t)len : SEE_LOGCAT_MESSAGE_LEN);
}
