#include "pal_trust_manager.h"

static ValidationCallback dotnetCallback;

void AndroidCryptoNative_RegisterTrustManagerValidationCallback(ValidationCallback callback)
{
    dotnetCallback = callback;
}

jobjectArray initTrustManagersWithCustomValidatorProxy(JNIEnv* env, intptr_t dotnetValidatorHandle)
{
    jobjectArray trustManagers = NULL;
    INIT_LOCALS(loc, defaultAlgorithm, tmf, trustManager, trustManagerProxy);

    // string defaultAlgorithm = TrustManagerFactory.getDefaultAlgorithm();
    // TrustManagerFactory tmf = TrustManagerFactory.getInstance(defaultAlgorithm);
    loc[defaultAlgorithm] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetDefaultAlgorithm);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[tmf] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetInstance, loc[defaultAlgorithm]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // tmf.init();
    // TrustManager[] trustManagers = tmf.getTrustManagers();
    (*env)->CallVoidMethod(env, loc[tmf], g_TrustManagerFactoryInit, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    trustManagers = (*env)->CallObjectMethod(env, loc[tmf], g_TrustManagerFactoryGetTrustManagers);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // boolean foundAndReplaced = false;
    // for (int i = 0; i < trustManagers.length; i++) {
    //   if (trustManagers[i] instanceof X509TrustManager) {
    //     trustManagers[i] = new DotnetProxyTrustManager(dotnetValidatorHandle, trustManagers[i]);
    //     foundAndReplaced = true;
    //     break;
    //   }
    // }

    bool foundAndReplaced = false;
    size_t length = (size_t)(*env)->GetArrayLength(env, trustManagers);
    for (size_t i = 0; i < length; i++)
    {
        loc[trustManager] = (*env)->GetObjectArrayElement(env, trustManagers, (jsize)i);

        if ((*env)->IsInstanceOf(env, loc[trustManager], g_X509TrustManager))
        {
            loc[trustManagerProxy] = (*env)->NewObject(env, g_DotnetProxyTrustManager, g_DotnetProxyTrustManagerCtor, (int)dotnetValidatorHandle, loc[trustManager]);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            (*env)->SetObjectArrayElement(env, trustManagers, (jsize)i, loc[trustManagerProxy]);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            foundAndReplaced = true;
            break;
        }

        ReleaseLRef(env, loc[trustManager]);
        loc[trustManager] = NULL;
    }

    if (!foundAndReplaced)
    {
        LOG_ERROR("no X509 trust managers");
        assert(0 && "x509 certificate was not found");
    }

cleanup:
    RELEASE_LOCALS(loc, env);
    return trustManagers;
}

jboolean Java_net_dot_android_crypto_DotnetProxyTrustManager_validateRemoteCertificate(
    JNIEnv* env,
    jobject handle,
    intptr_t dotnetValidatorHandle,
    jobjectArray certificates)
{
    assert(dotnetCallback && "dotnetCallback has not been registered");

    bool isAccepted = false;
    size_t certificateCount = 0;
    uint8_t** rawData = NULL;
    int32_t* lengths = NULL;
    jobject certificate = NULL;
    jbyteArray encodedCertificate = NULL;

    certificateCount = (size_t)(*env)->GetArrayLength(env, certificates);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    rawData = (uint8_t**)xcalloc(certificateCount, sizeof(uint8_t*));
    lengths = (int*)xcalloc(certificateCount, sizeof(int32_t));

    for (size_t i = 0; i < certificateCount; i++)
    {
        certificate = (*env)->GetObjectArrayElement(env, certificates, (jsize)i);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        encodedCertificate = (*env)->CallObjectMethod(env, certificate, g_CertificateGetEncoded);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        jsize length = (*env)->GetArrayLength(env, encodedCertificate);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        lengths[i] = (int32_t)length;
        rawData[i] = (uint8_t*)xmalloc((size_t)length * sizeof(uint8_t));
        (*env)->GetByteArrayRegion(env, encodedCertificate, 0, length, (jbyte*)rawData[i]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        ReleaseLRef(env, certificate);
        ReleaseLRef(env, encodedCertificate);
    }

    isAccepted = dotnetCallback(dotnetValidatorHandle, (int32_t)certificateCount, lengths, rawData);

cleanup:
    if (rawData != NULL)
    {
        for (size_t i = 0; i < certificateCount; i++)
        {
            if (rawData != NULL)
                free(rawData[i]);
        }
    }

    if (lengths != NULL)
        free(lengths);

    // TODO: make sure that we really don't need to free those local refs manually
    // they seem to be collected when the C# code is invoked

    return isAccepted ? JNI_TRUE : JNI_FALSE;
}
