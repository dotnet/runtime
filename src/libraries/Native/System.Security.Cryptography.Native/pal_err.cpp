// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_err.h"
#include "pal_utilities.h"

#include <openssl/err.h>

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" uint64_t ErrGetError()
{
    return CryptoNative_ErrGetError();
}

extern "C" uint64_t CryptoNative_ErrGetError()
{
    return ERR_get_error();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" uint64_t ErrGetErrorAlloc(int32_t* isAllocFailure)
{
    return CryptoNative_ErrGetErrorAlloc(isAllocFailure);
}

extern "C" uint64_t CryptoNative_ErrGetErrorAlloc(int32_t* isAllocFailure)
{
    unsigned long err = ERR_get_error();

    if (isAllocFailure)
    {
        *isAllocFailure = ERR_GET_REASON(err) == ERR_R_MALLOC_FAILURE;
    }

    return err;
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const char* ErrReasonErrorString(uint64_t error)
{
    return CryptoNative_ErrReasonErrorString(error);
}

extern "C" const char* CryptoNative_ErrReasonErrorString(uint64_t error)
{
    return ERR_reason_error_string(static_cast<unsigned long>(error));
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void ErrErrorStringN(uint64_t e, char* buf, int32_t len)
{
    return CryptoNative_ErrErrorStringN(e, buf, len);
}

extern "C" void CryptoNative_ErrErrorStringN(uint64_t e, char* buf, int32_t len)
{
    ERR_error_string_n(static_cast<unsigned long>(e), buf, UnsignedCast(len));
}
