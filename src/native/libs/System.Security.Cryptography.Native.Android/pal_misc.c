// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_misc.h"
#include "pal_jni.h"
#include <errno.h>
#include <fcntl.h>
#include <unistd.h>

int32_t CryptoNative_EnsureOpenSslInitialized(void)
{
    return 0;
}

int32_t CryptoNative_GetRandomBytes(uint8_t* buff, int32_t len)
{
    abort_unless(buff != NULL, "The 'buff' parameter must be a valid pointer");
    abort_unless(len >= 0, "The 'len' parameter must not be negative");

    int flags = O_RDONLY;
#ifdef O_CLOEXEC
    flags |= O_CLOEXEC;
#endif

    int fd;
    do
    {
        fd = open("/dev/urandom", flags);
    }
    while ((fd == -1) && (errno == EINTR));

    if (fd == -1)
    {
        return FAIL;
    }

#ifndef O_CLOEXEC
    fcntl(fd, F_SETFD, FD_CLOEXEC);
#endif

    int32_t offset = 0;
    while (offset != len)
    {
        ssize_t n = read(fd, buff + offset, (size_t)(len - offset));
        if (n == -1)
        {
            if (errno == EINTR)
            {
                continue;
            }

            close(fd);
            return FAIL;
        }

        if (n == 0)
        {
            close(fd);
            return FAIL;
        }

        offset += (int32_t)n;
    }

    close(fd);
    return SUCCESS;
}

jobject AndroidCryptoNative_CreateKeyPair(JNIEnv* env, jobject publicKey, jobject privateKey)
{
    jobject keyPair = (*env)->NewObject(env, g_keyPairClass, g_keyPairCtor, publicKey, privateKey);
    return CheckJNIExceptions(env) ? FAIL : ToGRef(env, keyPair);
}
