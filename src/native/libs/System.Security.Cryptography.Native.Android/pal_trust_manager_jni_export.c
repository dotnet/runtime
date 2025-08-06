// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_trust_manager.h"
#include <stdatomic.h>

static _Atomic RemoteCertificateValidationCallback verifyRemoteCertificate;

void StoreRemoteVerificationCallback (RemoteCertificateValidationCallback callback)
{
    atomic_store(&verifyRemoteCertificate, callback);
}

ARGS_NON_NULL_ALL jboolean Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv* env, jobject thisHandle, jlong sslStreamProxyHandle)
{
    RemoteCertificateValidationCallback verify = atomic_load(&verifyRemoteCertificate);
    abort_unless(verify, "verifyRemoteCertificate callback has not been registered");
    return verify((intptr_t)sslStreamProxyHandle);
}
