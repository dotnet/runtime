// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_bio.h"

#include <assert.h>

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" BIO* CreateMemoryBio()
{
    return CryptoNative_CreateMemoryBio();
}

extern "C" BIO* CryptoNative_CreateMemoryBio()
{
    return BIO_new(BIO_s_mem());
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" BIO* BioNewFile(const char* filename, const char* mode)
{
    return CryptoNative_BioNewFile(filename, mode);
}

extern "C" BIO* CryptoNative_BioNewFile(const char* filename, const char* mode)
{
    return BIO_new_file(filename, mode);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t BioDestroy(BIO* a)
{
    return CryptoNative_BioDestroy(a);
}

extern "C" int32_t CryptoNative_BioDestroy(BIO* a)
{
    return BIO_free(a);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t BioGets(BIO* b, char* buf, int32_t size)
{
    return CryptoNative_BioGets(b, buf, size);
}

extern "C" int32_t CryptoNative_BioGets(BIO* b, char* buf, int32_t size)
{
    return BIO_gets(b, buf, size);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t BioRead(BIO* b, void* buf, int32_t len)
{
    return CryptoNative_BioRead(b, buf, len);
}

extern "C" int32_t CryptoNative_BioRead(BIO* b, void* buf, int32_t len)
{
    return BIO_read(b, buf, len);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t BioWrite(BIO* b, const void* buf, int32_t len)
{
    return CryptoNative_BioWrite(b, buf, len);
}

extern "C" int32_t CryptoNative_BioWrite(BIO* b, const void* buf, int32_t len)
{
    return BIO_write(b, buf, len);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t GetMemoryBioSize(BIO* bio)
{
    return CryptoNative_GetMemoryBioSize(bio);
}

extern "C" int32_t CryptoNative_GetMemoryBioSize(BIO* bio)
{
    long ret = BIO_get_mem_data(bio, nullptr);

    // BIO_get_mem_data returns the memory size, which will always be
    // an int32.
    assert(ret <= INT32_MAX);
    return static_cast<int32_t>(ret);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t BioCtrlPending(BIO* bio)
{
    return CryptoNative_BioCtrlPending(bio);
}

extern "C" int32_t CryptoNative_BioCtrlPending(BIO* bio)
{
    size_t result = BIO_ctrl_pending(bio);
    assert(result <= INT32_MAX);
    return static_cast<int32_t>(result);
}
