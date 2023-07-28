// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_err.h"
#include "pal_utilities.h"

void CryptoNative_ErrClearError(void)
{
    ERR_clear_error();
}

uint64_t CryptoNative_ErrGetExceptionError(int32_t* isAllocFailure)
{
    unsigned long err = ERR_peek_last_error();

    if (isAllocFailure)
    {
        *isAllocFailure = ERR_GET_REASON(err) == ERR_R_MALLOC_FAILURE;
    }

    // We took the one we want, clear the rest.
    ERR_clear_error();
    return err;
}

uint64_t CryptoNative_ErrPeekError(void)
{
    return ERR_peek_error();
}

uint64_t CryptoNative_ErrPeekLastError(void)
{
    return ERR_peek_last_error();
}

const char* CryptoNative_ErrReasonErrorString(uint64_t error)
{
    const char* errStr = NULL;

#if defined NEED_OPENSSL_1_1 || defined NEED_OPENSSL_3_0
    int result = pthread_mutex_lock(&g_err_mutex);
    assert(!result && "Acquiring the error string table mutex failed.");

    if (!g_err_unloaded)
    {
#endif
        errStr = ERR_reason_error_string((unsigned long)error);
#if defined NEED_OPENSSL_1_1 || defined NEED_OPENSSL_3_0
    }

    result = pthread_mutex_unlock(&g_err_mutex);
    assert(!result && "Releasing the error string table mutex failed.");
#endif

    return errStr;
}

void CryptoNative_ErrErrorStringN(uint64_t e, char* buf, int32_t len)
{
#if defined NEED_OPENSSL_1_1 || defined NEED_OPENSSL_3_0
    int result = pthread_mutex_lock(&g_err_mutex);
    assert(!result && "Acquiring the error string table mutex failed.");

    if (!g_err_unloaded)
    {
#endif
        ERR_error_string_n((unsigned long)e, buf, Int32ToSizeT(len));
#if defined NEED_OPENSSL_1_1 || defined NEED_OPENSSL_3_0
    }
    else
    {
        // If there's no string table, just make it be the empty string.
        if (buf != NULL && len > 0)
        {
            buf[0] = 0;
        }
    }

    result = pthread_mutex_unlock(&g_err_mutex);
    assert(!result && "Releasing the error string table mutex failed.");
#endif
}
