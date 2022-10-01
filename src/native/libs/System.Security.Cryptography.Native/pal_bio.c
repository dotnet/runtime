// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_bio.h"

#include <assert.h>

BIO* CryptoNative_CreateMemoryBio(void)
{
    ERR_clear_error();
    return BIO_new(BIO_s_mem());
}

BIO* CryptoNative_BioNewFile(const char* filename, const char* mode)
{
    ERR_clear_error();
    return BIO_new_file(filename, mode);
}

int32_t CryptoNative_BioDestroy(BIO* a)
{
    return BIO_free(a);
}

int32_t CryptoNative_BioGets(BIO* b, char* buf, int32_t size)
{
    ERR_clear_error();
    return BIO_gets(b, buf, size);
}

int32_t CryptoNative_BioRead(BIO* b, void* buf, int32_t len)
{
    ERR_clear_error();
    return BIO_read(b, buf, len);
}

int32_t CryptoNative_BioWrite(BIO* b, const void* buf, int32_t len)
{
    ERR_clear_error();
    return BIO_write(b, buf, len);
}

int32_t CryptoNative_GetMemoryBioSize(BIO* bio)
{
    // No impact on error queue.
    long ret = BIO_get_mem_data(bio, NULL);

    // BIO_get_mem_data returns the memory size, which will always be
    // an int32.
    assert(ret <= INT32_MAX);
    return (int32_t)ret;
}

int32_t CryptoNative_BioCtrlPending(BIO* bio)
{
    // No impact on the error queue.
    size_t result = BIO_ctrl_pending(bio);
    assert(result <= INT32_MAX);
    return (int32_t)result;
}
