#include "pal_trust_manager.h"

static RemoteCertificateValidationCallback verifyRemoteCertificate;

ARGS_NON_NULL_ALL void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
{
    abort_unless(verifyRemoteCertificate == NULL, "AndroidCryptoNative_RegisterRemoteCertificateValidationCallback can only be used once");
    verifyRemoteCertificate = callback;
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

ARGS_NON_NULL_ALL jboolean Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv* env, jobject thisHandle, jlong sslStreamProxyHandle)
{
    abort_unless(verifyRemoteCertificate, "verifyRemoteCertificate callback has not been registered");
    return verifyRemoteCertificate((intptr_t)sslStreamProxyHandle);
}
