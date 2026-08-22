// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_key_manager.h"
#include <stdatomic.h>

static _Atomic LocalCertificateSelectionCallback selectLocalCertificate;

ARGS_NON_NULL_ALL void AndroidCryptoNative_RegisterSslStreamCallbacks(
    RemoteCertificateValidationCallback remoteCertificateValidationCallback,
    LocalCertificateSelectionCallback localCertificateSelectionCallback)
{
    SetRemoteCertificateValidationCallback(remoteCertificateValidationCallback);
    atomic_store(&selectLocalCertificate, localCertificateSelectionCallback);
}

jobjectArray AndroidCryptoNative_SSLStreamCreateKeyManagersForSelection(intptr_t sslStreamProxyHandle)
{
    abort_unless(sslStreamProxyHandle != 0, "invalid pointer to the .NET SslStream proxy");

    jobjectArray keyManagers = NULL;
    JNIEnv* env = GetJNIEnv();

    INIT_LOCALS(loc, dotnetX509KeyManager);

    loc[dotnetX509KeyManager] = (*env)->NewObject(
        env,
        g_DotnetX509KeyManager,
        g_DotnetX509KeyManagerProxyCtor,
        (jlong)sslStreamProxyHandle);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    keyManagers = make_java_object_array(env, 1, g_KeyManager, loc[dotnetX509KeyManager]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    keyManagers = ToGRef(env, keyManagers);

cleanup:
    RELEASE_LOCALS(loc, env);
    return keyManagers;
}

JNIEXPORT jobjectArray JNICALL Java_net_dot_android_crypto_DotnetX509KeyManager_selectClientCertificate(
    JNIEnv* env,
    jclass thisClass,
    jlong sslStreamProxyHandle,
    jobjectArray acceptableIssuers)
{
    (void)thisClass;

    jobjectArray keyManagers = NULL;
    jobjectArray result = NULL;
    uint16_t** issuerValues = NULL;

    INIT_LOCALS(loc, issuerString, keyManagerArray);

    jsize issuerCount = acceptableIssuers == NULL ? 0 : (*env)->GetArrayLength(env, acceptableIssuers);
    if (CheckJNIExceptions(env))
    {
        goto cleanup;
    }

    if (issuerCount > 0)
    {
        issuerValues = xcalloc((size_t)issuerCount, sizeof(uint16_t*));

        for (jsize i = 0; i < issuerCount; i++)
        {
            loc[issuerString] = (jstring)(*env)->GetObjectArrayElement(env, acceptableIssuers, i);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
            if (loc[issuerString] == NULL)
            {
                goto cleanup;
            }

            jsize length = (*env)->GetStringLength(env, loc[issuerString]);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            issuerValues[i] = xmalloc(((size_t)length + 1) * sizeof(uint16_t));
            (*env)->GetStringRegion(env, loc[issuerString], 0, length, (jchar*)issuerValues[i]);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
            issuerValues[i][length] = '\0';

            ReleaseLRef(env, loc[issuerString]);
            loc[issuerString] = NULL;
        }
    }

    LocalCertificateSelectionCallback select = atomic_load(&selectLocalCertificate);
    abort_unless(select, "selectLocalCertificate callback has not been registered");

    intptr_t keyManagersHandle = select(
        (intptr_t)sslStreamProxyHandle,
        (int32_t)issuerCount,
        issuerValues);
    keyManagers = (jobjectArray)keyManagersHandle;

    if (keyManagers == NULL)
    {
        goto cleanup;
    }

    loc[keyManagerArray] = (*env)->NewLocalRef(env, keyManagers);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    result = (jobjectArray)loc[keyManagerArray];
    loc[keyManagerArray] = NULL;

cleanup:
    ReleaseGRef(env, keyManagers);
    RELEASE_LOCALS(loc, env);

    if (issuerValues != NULL)
    {
        for (jsize i = 0; i < issuerCount; i++)
        {
            if (issuerValues[i] != NULL)
            {
                free(issuerValues[i]);
            }
        }
    }

    free(issuerValues);
    return result;
}
