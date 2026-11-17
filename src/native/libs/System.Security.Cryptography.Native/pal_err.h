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
The error functions below decode and encode with the error layout of the OpenSSL loaded at run time. ERR_GET_LIB,
ERR_GET_REASON and the ERR_R_* reasons use the layout of the headers the shim is compiled against, which OpenSSL 3.0
changed, so they are wrong for one of the two when the shim is portable.
*/

/*
Returns the library of the error, like ERR_GET_LIB.
*/
int32_t ErrGetLib(unsigned long error);

/*
Returns the reason to put on the error queue for an allocation failure, like ERR_R_MALLOC_FAILURE.
*/
int32_t ErrGetMallocFailureReason(void);

/*
Returns 1 if the error is an allocation failure (ERR_R_MALLOC_FAILURE), otherwise 0.
*/
int32_t ErrIsMallocFailure(unsigned long error);

/*
Returns 1 if the error is ERR_R_UNSUPPORTED, otherwise 0. The reason exists from OpenSSL 3.0 on.
*/
int32_t ErrIsUnsupported(unsigned long error);

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
