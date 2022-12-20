// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_compiler.h"
#include <stdint.h>
#include "opensslshim.h"

#include <pthread.h>

#if defined NEED_OPENSSL_1_1 || defined NEED_OPENSSL_3_0
extern pthread_mutex_t g_err_mutex;
extern int volatile g_err_unloaded;
#endif

/*
Shims the ERR_clear_error method.
*/
PALEXPORT void CryptoNative_ErrClearError(void);

/*
Returns the error code to use as the basis for an exception.

If the error represents an allocation error, *isAllocFailure is set to 1.
*/
PALEXPORT uint64_t CryptoNative_ErrGetExceptionError(int32_t* isAllocFailure);

PALEXPORT uint64_t CryptoNative_ErrPeekError(void);

PALEXPORT uint64_t CryptoNative_ErrPeekLastError(void);

/*
Shims the ERR_reason_error_string method.

Returns the string for the specified error.
*/
PALEXPORT const char* CryptoNative_ErrReasonErrorString(uint64_t error);

/*
Direct shim to ERR_error_string_n.
*/
PALEXPORT void CryptoNative_ErrErrorStringN(uint64_t e, char* buf, int32_t len);
