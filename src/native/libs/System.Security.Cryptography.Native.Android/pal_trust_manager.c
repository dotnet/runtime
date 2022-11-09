#include "pal_trust_manager.h"

static RemoteCertificateValidationCallback verifyRemoteCertificate;

void AndroidCryptoNative_RegisterTrustManagerCallback(RemoteCertificateValidationCallback callback)
{
    verifyRemoteCertificate = callback;
}

jobjectArray InitTrustManagersWithCustomValidatorProxy(JNIEnv* env, intptr_t dotnetHandle)
{
    abort_unless(dotnetHandle != 0, "invalid pointer to the .NET remote certificate validator");

    jobjectArray trustManagers = NULL;
    INIT_LOCALS(loc, defaultAlgorithm, tmf, trustManager, trustManagerProxy);

    // string defaultAlgorithm = TrustManagerFactory.getDefaultAlgorithm();
    loc[defaultAlgorithm] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetDefaultAlgorithm);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // TrustManagerFactory tmf = TrustManagerFactory.getInstance(defaultAlgorithm);
    loc[tmf] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetInstance, loc[defaultAlgorithm]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // tmf.init();
    (*env)->CallVoidMethod(env, loc[tmf], g_TrustManagerFactoryInit, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // TrustManager[] trustManagers = tmf.getTrustManagers();
    trustManagers = (*env)->CallObjectMethod(env, loc[tmf], g_TrustManagerFactoryGetTrustManagers);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // boolean foundAndReplaced = false;
    // for (int i = 0; i < trustManagers.length; i++) {
    //   if (trustManagers[i] instanceof X509TrustManager) {
    //     trustManagers[i] = new RemoteCertificateVerificationProxyTrustManager(dotnetHandle);
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
            loc[trustManagerProxy] = (*env)->NewObject(env, g_RemoteCertificateVerificationProxyTrustManager, g_RemoteCertificateVerificationProxyTrustManagerCtor, (int)dotnetHandle);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            (*env)->SetObjectArrayElement(env, trustManagers, (jsize)i, loc[trustManagerProxy]);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            foundAndReplaced = true;
            break;
        }

        ReleaseLRef(env, loc[trustManager]);
        loc[trustManager] = NULL;
    }

    abort_unless(foundAndReplaced, "no X509 trust managers");
cleanup:
    RELEASE_LOCALS(loc, env);
    return trustManagers;
}

jboolean Java_net_dot_android_crypto_RemoteCertificateVerificationProxyTrustManager_verifyRemoteCertificate(
    JNIEnv* env, jobject thisHandle, intptr_t dotnetHandle, jobjectArray certificates)
{
    abort_unless(verifyRemoteCertificate, "verifyRemoteCertificate callback has not been registered");

    INIT_LOCALS(loc, defaultAlgorithm, tmf, trustManager, trustManagerProxy, certificate, encodedCertificate, encodedCertificates);

    bool isAccepted = false;
    uint8_t* rawData = NULL;
    int32_t* lengths = NULL;

    size_t certificateCount = (size_t)(*env)->GetArrayLength(env, certificates);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    lengths = (int*)xcalloc(certificateCount, sizeof(int32_t));
    size_t totalLength = 0;

    loc[encodedCertificates] = make_java_object_array(env, (int32_t)certificateCount, g_ByteArray, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    for (size_t i = 0; i < certificateCount; i++)
    {
        // X509Certificate certificate = certificates[i];
        // byte[] encodedCertificate = certificate.getEncoded();
        // int length = encodedCertificate.length;
        // lengths[i] = length;
        // totalLength += length;
        // encodedCertificates[i] = encodedCertificate;

        loc[certificate] = (*env)->GetObjectArrayElement(env, certificates, (jsize)i);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        loc[encodedCertificate] = (*env)->CallObjectMethod(env, loc[certificate], g_CertificateGetEncoded);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        jsize length = (*env)->GetArrayLength(env, loc[encodedCertificate]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        lengths[i] = (int32_t)length;
        totalLength += (size_t)lengths[i];

        (*env)->SetObjectArrayElement(env, loc[encodedCertificates], (jsize)i, loc[encodedCertificate]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        ReleaseLRef(env, loc[certificate]);
        ReleaseLRef(env, loc[encodedCertificate]);
        loc[certificate] = NULL;
        loc[encodedCertificate] = NULL;
    }

    rawData = (uint8_t*)xmalloc(totalLength * sizeof(uint8_t));
    int32_t offset = 0;

    for (size_t i = 0; i < certificateCount; i++)
    {
        loc[encodedCertificate] = (*env)->GetObjectArrayElement(env, loc[encodedCertificates], (jsize)i);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        (*env)->GetByteArrayRegion(env, loc[encodedCertificate], 0, lengths[i], (jbyte*)&rawData[offset]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        offset += lengths[i];

        ReleaseLRef(env, loc[encodedCertificate]);
        loc[encodedCertificate] = NULL;
    }

    isAccepted = verifyRemoteCertificate(dotnetHandle, (int32_t)certificateCount, lengths, rawData);

cleanup:
    if (rawData != NULL)
        free(rawData);

    if (lengths != NULL)
        free(lengths);

    RELEASE_LOCALS(loc, env);

    return isAccepted ? JNI_TRUE : JNI_FALSE;
}
