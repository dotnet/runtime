#include "pal_trust_manager.h"
#include <stdatomic.h>

static _Atomic RemoteCertificateValidationCallback verifyRemoteCertificate;

ARGS_NON_NULL_ALL void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
{
    atomic_store(&verifyRemoteCertificate, callback);
}

// Gets the default X509TrustManager from the system TrustManagerFactory
static jobject GetDefaultX509TrustManager(JNIEnv* env)
{
    jobject result = NULL;
    INIT_LOCALS(loc, algorithm, tmf, trustManagers);

    // String algorithm = TrustManagerFactory.getDefaultAlgorithm();
    loc[algorithm] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetDefaultAlgorithm);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // TrustManagerFactory tmf = TrustManagerFactory.getInstance(algorithm);
    loc[tmf] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetInstance, loc[algorithm]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // tmf.init((KeyStore)null) -> use default system key store
    (*env)->CallVoidMethod(env, loc[tmf], g_TrustManagerFactoryInit, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // TrustManager[] tms = tmf.getTrustManagers();
    loc[trustManagers] = (*env)->CallObjectMethod(env, loc[tmf], g_TrustManagerFactoryGetTrustManagers);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // Find the first X509TrustManager in the array
    jsize length = (*env)->GetArrayLength(env, loc[trustManagers]);
    for (jsize i = 0; i < length; i++)
    {
        jobject tm = (*env)->GetObjectArrayElement(env, loc[trustManagers], i);
        if ((*env)->IsInstanceOf(env, tm, g_X509TrustManager))
        {
            result = tm;
            break;
        }
        ReleaseLRef(env, tm);
    }

cleanup:
    RELEASE_LOCALS(loc, env);
    return result;
}

jobjectArray GetTrustManagers(JNIEnv* env, intptr_t sslStreamProxyHandle, const char* targetHost)
{
    // X509TrustManager platformTrustManager = GetDefaultX509TrustManager();
    // DotnetProxyTrustManager dotnetProxyTrustManager = new DotnetProxyTrustManager(sslStreamProxyHandle, platformTrustManager, targetHost);
    // TrustManager[] trustManagers = new TrustManager[] { dotnetProxyTrustManager };
    // return trustManagers;

    jobjectArray trustManagers = NULL;
    INIT_LOCALS(loc, platformTrustManager, dotnetProxyTrustManager);

    loc[platformTrustManager] = GetDefaultX509TrustManager(env);
    if (loc[platformTrustManager] == NULL)
    {
        LOG_ERROR("Failed to get default X509TrustManager");
        goto cleanup;
    }

    jstring targetHostStr = targetHost != NULL ? make_java_string(env, targetHost) : NULL;
    loc[dotnetProxyTrustManager] = (*env)->NewObject(
        env,
        g_DotnetProxyTrustManager,
        g_DotnetProxyTrustManagerCtor,
        (jlong)sslStreamProxyHandle,
        loc[platformTrustManager],
        targetHostStr);
    ReleaseLRef(env, targetHostStr);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    trustManagers = make_java_object_array(env, 1, g_TrustManager, loc[dotnetProxyTrustManager]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

cleanup:
    RELEASE_LOCALS(loc, env);
    return trustManagers;
}

ARGS_NON_NULL_ALL jboolean Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv* env, jobject thisHandle, jlong sslStreamProxyHandle, jboolean chainTrustedByPlatform)
{
    RemoteCertificateValidationCallback verify = atomic_load(&verifyRemoteCertificate);
    abort_unless(verify, "verifyRemoteCertificate callback has not been registered");
    return verify((intptr_t)sslStreamProxyHandle, (int32_t)chainTrustedByPlatform);
}
