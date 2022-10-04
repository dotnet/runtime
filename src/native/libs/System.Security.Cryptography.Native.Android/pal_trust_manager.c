#include "pal_trust_manager.h"

static ValidationCallback validation_callback;

void AndroidCryptoNative_RegisterTrustManagerValidationCallback(ValidationCallback callback)
{
    validation_callback = callback;
}

jobjectArray init_trust_managers_with_custom_validator(JNIEnv* env, intptr_t csharpObjectHandle)
{
    jstring defaultAlgorithm = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetDefaultAlgorithm);
    jobject tmf = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetInstance, defaultAlgorithm);
    if (CheckJNIExceptions(env))
        return NULL;

    (*env)->CallVoidMethod(env, tmf, g_TrustManagerFactoryInit, NULL);
    jobjectArray trust_managers = (*env)->CallObjectMethod(env, tmf, g_TrustManagerFactoryGetTrustManagers);
    bool found_and_replaced = false;
    size_t length = (size_t)(*env)->GetArrayLength(env, trust_managers);
    for (size_t i = 0; i < length; i++)
    {
        jobject trust_manager = (*env)->GetObjectArrayElement(env, trust_managers, (jsize)i);
        if ((*env)->IsInstanceOf(env, trust_manager, g_X509TrustManager))
        {
            jobject trust_manager_proxy = (*env)->NewObject(env, g_TrustManagerProxy, g_TrustManagerProxyCtor, (int)csharpObjectHandle, trust_manager);
            (*env)->SetObjectArrayElement(env, trust_managers, (jsize)i, trust_manager_proxy);

            found_and_replaced = true;
            break;
        }
    }

    if (!found_and_replaced)
    {
        // TODO fatal error
        LOG_ERROR("no X509 trust managers");
        assert(0 && "x509 certificate was not found");
    }

    // TODO cleanup

    return trust_managers;
}

jboolean Java_net_dot_android_crypto_TrustManagerProxy_validateRemoteCertificate(
    JNIEnv *env,
    jobject trustManagerProxy,
    intptr_t csharpObjectHandle,
    jobjectArray certificates,
    int32_t errors)
{
    // prepare all the certificates
    size_t certificateCount = (size_t)(*env)->GetArrayLength(env, certificates);
    uint8_t **rawData = (uint8_t**)xcalloc(certificateCount, sizeof(uint8_t*));
    int32_t *lengths = (int*)xcalloc(certificateCount, sizeof(int32_t));

    for (size_t i = 0; i < certificateCount; i++)
    {
        jobject certificate = (*env)->GetObjectArrayElement(env, certificates, (jsize)i);
        jbyteArray encodedCertificate = (*env)->CallObjectMethod(env, certificate, g_CertificateGetEncoded);
        jsize length = (*env)->GetArrayLength(env, encodedCertificate);
        // TODO cleanup

        lengths[i] = (int32_t)length;
        rawData[i] = (uint8_t*)xmalloc((size_t)length * sizeof(uint8_t));
        (*env)->GetByteArrayRegion(env, encodedCertificate, 0, length, (jbyte*)rawData[i]);
    }

    assert(validation_callback && "validation_callback must be initialized");
    bool isAccepted = validation_callback(csharpObjectHandle, rawData, lengths, (int32_t)certificateCount, errors);

    // free all the memory we allocated
    for (size_t i = 0; i < certificateCount; i++)
        free(rawData[i]);

    free(rawData);
    free(lengths);

    // TODO java stuff cleanup

    return isAccepted ? JNI_TRUE : JNI_FALSE;
}
