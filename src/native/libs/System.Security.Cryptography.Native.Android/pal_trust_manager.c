// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_trust_manager.h"

ARGS_NON_NULL_ALL void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
{
    StoreRemoteVerificationCallback(callback);
}

ARGS_NON_NULL_ALL jobjectArray GetTrustManagers(JNIEnv* env, intptr_t sslStreamProxyHandle)
{
    // X509TrustManager dotnetProxyTrustManager = new DotnetProxyTrustManager(sslStreamProxyHandle);
    // TrustManager[] trustManagers = new TrustManager[] { dotnetProxyTrustManager };
    // return trustManagers;

    jobjectArray trustManagers = NULL;
    INIT_LOCALS(loc, dotnetProxyTrustManager);

    loc[dotnetProxyTrustManager] = (*env)->NewObject(env, g_DotnetProxyTrustManager, g_DotnetProxyTrustManagerCtor, (jlong)sslStreamProxyHandle);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    trustManagers = make_java_object_array(env, 1, g_TrustManager, loc[dotnetProxyTrustManager]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

cleanup:
    RELEASE_LOCALS(loc, env);
    return trustManagers;
}

