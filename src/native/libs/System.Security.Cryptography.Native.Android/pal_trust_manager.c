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

    size_t count = (size_t)(*env)->GetArrayLength(env, certificates);
    jobject* ptrs = xcalloc(count, sizeof(jobject));

    for (size_t i = 0; i < count; i++)
        ptrs[i] = ToGRef(env, (*env)->GetObjectArrayElement(env, certificates, (jsize)i));

    bool isAccepted = verifyRemoteCertificate(dotnetHandle, (int32_t)count, ptrs);

    free(ptrs);
    return isAccepted ? JNI_TRUE : JNI_FALSE;
}
