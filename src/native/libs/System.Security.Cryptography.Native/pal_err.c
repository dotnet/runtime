// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "openssl.h"
#include "pal_err.h"
#include "pal_utilities.h"

// OpenSSL 3.0 changed how an error code packs the library and the reason, and ERR_GET_LIB and ERR_GET_REASON
// decode with the layout of the headers the shim is compiled against. A portable build loads whichever OpenSSL is
// installed, so the functions below decode and encode with the layout of the loaded library instead.
// 1.x: lib << 24 | func << 12 | reason, the reason 12 bits including its flags.
// 3.0 and later: lib << 23 | reason, the reason 23 bits including its flags, or ERR_SYSTEM_FLAG | errno.
// Layouts and constants: OpenSSL 1.1.1w and 3.0.0 headers,
// https://github.com/openssl/openssl/blob/e04bd3433fd84e1861bf258ea37928d9845e6a86/include/openssl/err.h
// https://github.com/openssl/openssl/blob/89cd17a031e022211684eb7eb41190cf1910f9fa/include/openssl/err.h.in
#define ERR_X_LIB_MASK 0xFFUL // The same in both layouts.
#define ERR_1_X_LIB_OFFSET 24
#define ERR_1_X_REASON_MASK 0xFFFUL
#define ERR_1_X_R_MALLOC_FAILURE (1 | 64) // Reason 1 | ERR_R_FATAL.
#define ERR_3_X_SYSTEM_FLAG 0x80000000UL
#define ERR_3_X_LIB_OFFSET 23
#define ERR_3_X_REASON_MASK 0x7FFFFFUL
#define ERR_3_X_R_MALLOC_FAILURE (256 | (0x3 << 18)) // Reason 256 | ERR_RFLAG_FATAL | ERR_RFLAG_COMMON.

#if OPENSSL_VERSION_NUMBER >= OPENSSL_VERSION_3_0_RTM
c_static_assert(ERR_LIB_MASK == ERR_X_LIB_MASK);
c_static_assert(ERR_SYSTEM_FLAG == ERR_3_X_SYSTEM_FLAG);
c_static_assert(ERR_LIB_OFFSET == ERR_3_X_LIB_OFFSET);
c_static_assert(ERR_REASON_MASK == ERR_3_X_REASON_MASK);
c_static_assert(ERR_R_MALLOC_FAILURE == ERR_3_X_R_MALLOC_FAILURE);
#else
c_static_assert(ERR_GET_LIB(0xFFFFFFFFUL) == ERR_X_LIB_MASK);
c_static_assert(ERR_PACK(1, 0, 0) == (1UL << ERR_1_X_LIB_OFFSET));
c_static_assert(ERR_GET_REASON(0xFFFFFFFFUL) == ERR_1_X_REASON_MASK);
c_static_assert(ERR_R_MALLOC_FAILURE == ERR_1_X_R_MALLOC_FAILURE);
#endif

static int IsOpenSsl3Loaded(void)
{
    return CryptoNative_OpenSslVersionNumber() >= OPENSSL_VERSION_3_0_RTM;
}

// Returns the reason of the error, or -1 for an OpenSSL 3 system error.
static int32_t GetErrorReason(unsigned long error)
{
    if (IsOpenSsl3Loaded())
    {
        return (error & ERR_3_X_SYSTEM_FLAG) ? -1 : (int32_t)(error & ERR_3_X_REASON_MASK);
    }

    return (int32_t)(error & ERR_1_X_REASON_MASK);
}

int32_t ErrGetLib(unsigned long error)
{
    if (IsOpenSsl3Loaded())
    {
        return (error & ERR_3_X_SYSTEM_FLAG) ? ERR_LIB_SYS : (int32_t)((error >> ERR_3_X_LIB_OFFSET) & ERR_X_LIB_MASK);
    }

    return (int32_t)((error >> ERR_1_X_LIB_OFFSET) & ERR_X_LIB_MASK);
}

int32_t ErrGetMallocFailureReason(void)
{
    return IsOpenSsl3Loaded() ? ERR_3_X_R_MALLOC_FAILURE : ERR_1_X_R_MALLOC_FAILURE;
}

int32_t ErrIsMallocFailure(unsigned long error)
{
    return GetErrorReason(error) == ErrGetMallocFailureReason();
}

int32_t ErrIsUnsupported(unsigned long error)
{
    return IsOpenSsl3Loaded() && GetErrorReason(error) == ERR_R_UNSUPPORTED;
}

void CryptoNative_ErrClearError(void)
{
    ERR_clear_error();
}

uint64_t CryptoNative_ErrGetExceptionError(int32_t* isAllocFailure)
{
    unsigned long err = ERR_peek_last_error();

    if (isAllocFailure)
    {
        *isAllocFailure = ErrIsMallocFailure(err);
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
