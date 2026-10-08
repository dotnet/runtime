#include "pal_trust_manager.h"
#include <stdatomic.h>
#include <stdio.h>

static _Atomic RemoteCertificateValidationCallback verifyRemoteCertificate;

// Cached JNI global ref to the default platform X509TrustManager (no custom KeyStore).
// Initializing TrustManagerFactory.init(null) is potentially expensive (reads the system
// keystore from disk); caching avoids paying that cost on every TLS handshake.
static _Atomic(jobject) g_cachedDefaultTrustManager = NULL;

ARGS_NON_NULL_ALL void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
{
    atomic_store(&verifyRemoteCertificate, callback);
}

ARGS_NON_NULL(2, 3) int32_t AndroidCryptoNative_GetPlatformValidationError(jstring platformValidationError, const uint16_t** out, int32_t* outLen)
{
    abort_if_invalid_pointer_argument (out);
    abort_if_invalid_pointer_argument (outLen);

    *out = NULL;
    *outLen = 0;
    if (platformValidationError == NULL)
    {
        return SUCCESS;
    }

    JNIEnv* env = GetJNIEnv();
    int32_t len = (*env)->GetStringLength(env, platformValidationError);
    if (CheckJNIExceptions(env))
    {
        return FAIL;
    }

    const jchar* chars = (*env)->GetStringChars(env, platformValidationError, NULL);
    if (chars == NULL)
    {
        CheckJNIExceptions(env);
        return FAIL;
    }

    *out = (const uint16_t*)chars;
    *outLen = len;
    return SUCCESS;
}

void AndroidCryptoNative_ReleasePlatformValidationError(jstring platformValidationError, const uint16_t* chars)
{
    if (platformValidationError == NULL || chars == NULL)
    {
        return;
    }

    JNIEnv* env = GetJNIEnv();
    (*env)->ReleaseStringChars(env, platformValidationError, (const jchar*)chars);
}

// Gets the default platform X509TrustManager from TrustManagerFactory.
// The result is cached because TrustManagerFactory.init(null) may read the
// system keystore from disk.
static jobject GetX509TrustManager(JNIEnv* env)
{
    // Fast path: return a fresh local ref to the cached default trust manager.
    jobject cached = atomic_load(&g_cachedDefaultTrustManager);
    if (cached != NULL)
    {
        return (*env)->NewLocalRef(env, cached);
    }

    jobject result = NULL;
    INIT_LOCALS(loc, algorithm, tmf, trustManagers);

    // String algorithm = TrustManagerFactory.getDefaultAlgorithm();
    loc[algorithm] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetDefaultAlgorithm);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // TrustManagerFactory tmf = TrustManagerFactory.getInstance(algorithm);
    loc[tmf] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetInstance, loc[algorithm]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // tmf.init(null) uses system/app defaults, including network_security_config.xml.
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
            result = (*env)->NewLocalRef(env, tm);
        }
        ReleaseLRef(env, tm);
        if (result != NULL)
            break;
    }

    // Populate the cache. If a racing thread beat us to it, drop our new global ref
    // and keep using the cached one.
    if (result != NULL)
    {
        jobject newGlobal = (*env)->NewGlobalRef(env, result);
        jobject expected = NULL;
        if (!atomic_compare_exchange_strong(&g_cachedDefaultTrustManager, &expected, newGlobal))
        {
            (*env)->DeleteGlobalRef(env, newGlobal);
        }
    }

cleanup:
    RELEASE_LOCALS(loc, env);
    return result;
}

// Creates a DotnetProxyTrustManager wrapping the platform's X509TrustManager.
// The proxy consults Android's trust infrastructure first, then delegates to the
// managed SslStream validation callback. The platform's verdict (chainTrustedByPlatform)
// is passed to the managed side to be combined with managed validation.
jobjectArray GetTrustManagers(JNIEnv* env, intptr_t sslStreamProxyHandle, const char* targetHost)
{
    jobjectArray trustManagers = NULL;
    INIT_LOCALS(loc, platformTrustManager, dotnetProxyTrustManager);

    loc[platformTrustManager] = GetX509TrustManager(env);
    if (loc[platformTrustManager] == NULL)
    {
        LOG_ERROR("Failed to get X509TrustManager");
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

// JNI callback registered for DotnetProxyTrustManager.verifyRemoteCertificate().
// Forwards the platform's trust verdict to the managed SslStream validation callback.
// The managed side decides how to combine this with its own X509Chain.Build result.
// platformValidationError carries the platform's textual rejection reason, or NULL when
// the platform trusts the chain.
ARGS_NON_NULL(1, 2) JNIEXPORT jboolean JNICALL Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv* env, jobject thisHandle, jlong sslStreamProxyHandle, jstring platformValidationError)
{
    RemoteCertificateValidationCallback verify = atomic_load(&verifyRemoteCertificate);
    abort_unless(verify, "verifyRemoteCertificate callback has not been registered");

    jstring validationError = NULL;
    if (platformValidationError != NULL)
    {
        validationError = (jstring)(*env)->NewGlobalRef(env, platformValidationError);
        if (validationError == NULL)
        {
            return JNI_FALSE;
        }
    }

    jboolean result = verify((intptr_t)sslStreamProxyHandle, validationError);
    ReleaseGRef(env, validationError);
    return result;
}
