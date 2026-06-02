// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_misc.h"
#include "pal_jni.h"
#include <errno.h>
#include <fcntl.h>
#include <limits.h>
#include <unistd.h>

int32_t CryptoNative_EnsureOpenSslInitialized(void)
{
    return 0;
}

int32_t CryptoNative_GetRandomBytes(uint8_t* buff, int32_t len)
{
    abort_unless(buff != NULL, "The 'buff' parameter must be a valid pointer");
    if (len < 0)
    {
        LOG_ERROR("Invalid random byte count: %d", len);
        return FAIL;
    }

    int flags = O_RDONLY;
#ifdef O_CLOEXEC
    flags |= O_CLOEXEC;
#endif

    int fd;
    int error;
    do
    {
        // Use Android's non-blocking cryptographic OS RNG source.
        fd = open("/dev/urandom", flags);
        error = fd == -1 ? errno : 0;
    }
    while ((fd == -1) && (error == EINTR));

    if (fd == -1)
    {
        LOG_ERROR("Unable to open /dev/urandom: %d", error);
        return FAIL;
    }

#ifndef O_CLOEXEC
    if (fcntl(fd, F_SETFD, FD_CLOEXEC) == -1)
    {
        LOG_ERROR("Unable to set close-on-exec on /dev/urandom: %d", errno);
        close(fd);
        return FAIL;
    }
#endif

    size_t length = (size_t)len;
    size_t offset = 0;
    while (offset != length)
    {
        size_t remaining = length - offset;
        if (remaining > SSIZE_MAX)
        {
            remaining = SSIZE_MAX;
        }

        ssize_t n = read(fd, buff + offset, remaining);
        if (n == -1)
        {
            error = errno;
            if (error == EINTR)
            {
                continue;
            }

            close(fd);
            LOG_ERROR("Unable to read from /dev/urandom: %d", error);
            return FAIL;
        }

        // Avoid spinning if /dev/urandom unexpectedly returns no data.
        if (n == 0)
        {
            LOG_ERROR("No data read from /dev/urandom");
            close(fd);
            return FAIL;
        }

        offset += (size_t)n;
    }

    if (close(fd) == -1)
    {
        LOG_ERROR("Unable to close /dev/urandom: %d", errno);
        return FAIL;
    }

    return SUCCESS;
}

jobject AndroidCryptoNative_CreateKeyPair(JNIEnv* env, jobject publicKey, jobject privateKey)
{
    jobject keyPair = (*env)->NewObject(env, g_keyPairClass, g_keyPairCtor, publicKey, privateKey);
    return CheckJNIExceptions(env) ? FAIL : ToGRef(env, keyPair);
}
