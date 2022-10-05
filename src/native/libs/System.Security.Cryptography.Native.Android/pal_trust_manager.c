#include "pal_trust_manager.h"

static ValidationCallback dotnetValidationCallback;

void AndroidCryptoNative_RegisterTrustManagerValidationCallback(ValidationCallback callback)
{
    dotnetValidationCallback = callback;
}

jobjectArray initTrustManagersWithCustomValidatorProxy(JNIEnv* env, intptr_t dotnetRemoteCertificateValidatorHandle)
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
    // int length = trustManagers.getLength();
    // for (int i = 0; i < length; i++) {
    //   if (trustManagers[i] instanceof X509TrustManager) {
    //     trustManagers[i] = new RemoteCertificateValidationCallbackProxy(dotnetRemoteCertificateValidatorHandle, trustManagers[i]);
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
            loc[trustManagerProxy] = (*env)->NewObject(env, g_RemoteCertificateValidationCallbackProxy, g_RemoteCertificateValidationCallbackProxyCtor, (int)dotnetRemoteCertificateValidatorHandle, loc[trustManager]);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            (*env)->SetObjectArrayElement(env, trustManagers, (jsize)i, loc[trustManagerProxy]);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            foundAndReplaced = true;
            break;
        }

        ReleaseLRef(env, loc[trustManager]);
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

jboolean Java_net_dot_android_crypto_RemoteCertificateValidationCallbackProxy_validateRemoteCertificate(
    JNIEnv* env,
    jobject RemoteCertificateValidationCallbackProxy,
    intptr_t dotnetRemoteCertificateValidatorHandle,
    jobjectArray certificates,
    int32_t errors)
{
    assert(dotnetValidationCallback && "dotnetValidationCallback has not been registered");

    bool isAccepted = false;
    size_t certificateCount = 0;
    uint8_t** rawData = NULL;
    int32_t* lengths = NULL;
    INIT_LOCALS(loc, certificate, encodedCertificate);

    certificateCount = (size_t)(*env)->GetArrayLength(env, certificates);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    rawData = (uint8_t**)xcalloc(certificateCount, sizeof(uint8_t*));
    lengths = (int*)xcalloc(certificateCount, sizeof(int32_t));

    for (size_t i = 0; i < certificateCount; i++)
    {
        loc[certificate] = (*env)->GetObjectArrayElement(env, certificates, (jsize)i);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        loc[encodedCertificate] = (*env)->CallObjectMethod(env, loc[certificate], g_CertificateGetEncoded);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        jsize length = (*env)->GetArrayLength(env, loc[encodedCertificate]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        lengths[i] = (int32_t)length;
        rawData[i] = (uint8_t*)xmalloc((size_t)length * sizeof(uint8_t));
        (*env)->GetByteArrayRegion(env, loc[encodedCertificate], 0, length, (jbyte*)rawData[i]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        ReleaseLRef(env, loc[certificate]);
        ReleaseLRef(env, loc[encodedCertificate]);
    }

    isAccepted = dotnetValidationCallback(dotnetRemoteCertificateValidatorHandle, rawData, lengths, (int32_t)certificateCount, errors);

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

    RELEASE_LOCALS(loc, env);

    return isAccepted ? JNI_TRUE : JNI_FALSE;
}
