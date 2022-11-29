#include "pal_trust_manager.h"

static RemoteCertificateValidationCallback verifyRemoteCertificate;

void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
{
    verifyRemoteCertificate = callback;
}

static

jobjectArray InitTrustManagersWithDotnetProxy(JNIEnv* env, intptr_t sslStreamProxyHandle)
{
    jobjectArray trustManagers = NULL;
    INIT_LOCALS(loc, defaultAlgorithm, tmf, trustManager, dotnetProxyTrustManager);

    // string defaultAlgorithm = TrustManagerFactory.getDefaultAlgorithm();
    // TrustManagerFactory tmf = TrustManagerFactory.getInstance(defaultAlgorithm);
    // tmf.init();
    // TrustManager[] trustManagers = tmf.getTrustManagers();

    loc[defaultAlgorithm] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetDefaultAlgorithm);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[tmf] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetInstance, loc[defaultAlgorithm]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    (*env)->CallVoidMethod(env, loc[tmf], g_TrustManagerFactoryInit, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    trustManagers = (*env)->CallObjectMethod(env, loc[tmf], g_TrustManagerFactoryGetTrustManagers);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // boolean foundAndReplaced = false;
    // for (int i = 0; i < trustManagers.length; i++) {
    //   if (trustManagers[i] instanceof X509TrustManager) {
    //     trustManagers[i] = new DotnetProxyTrustManager(sslStreamProxyHandle);
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
            loc[dotnetProxyTrustManager] = (*env)->NewObject(env, g_DotnetProxyTrustManager, g_DotnetProxyTrustManagerCtor, (jlong)sslStreamProxyHandle);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

            (*env)->SetObjectArrayElement(env, trustManagers, (jsize)i, loc[dotnetProxyTrustManager]);
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

jboolean Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv* env, jobject thisHandle, jlong sslStreamProxyHandle)
{
    abort_unless(verifyRemoteCertificate, "verifyRemoteCertificate callback has not been registered");
    return verifyRemoteCertificate((intptr_t)sslStreamProxyHandle);
}
