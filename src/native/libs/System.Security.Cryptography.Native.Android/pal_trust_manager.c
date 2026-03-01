#include "pal_trust_manager.h"
#include <stdatomic.h>
#include <stdio.h>

static _Atomic RemoteCertificateValidationCallback verifyRemoteCertificate;

ARGS_NON_NULL_ALL void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
{
    atomic_store(&verifyRemoteCertificate, callback);
}

// Gets the X509TrustManager from TrustManagerFactory, optionally initialized
// with a custom KeyStore containing trusted certificates. When customTrustKeyStore
// is NULL, the system default trust store is used. When non-NULL, only the
// certificates in the custom KeyStore are trusted (Java's KeyStore.setCertificateEntry
// treats every entry as a trust anchor).
static jobject GetX509TrustManager(JNIEnv* env, jobject customTrustKeyStore)
{
    jobject result = NULL;
    INIT_LOCALS(loc, algorithm, tmf, trustManagers);

    // String algorithm = TrustManagerFactory.getDefaultAlgorithm();
    loc[algorithm] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetDefaultAlgorithm);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // TrustManagerFactory tmf = TrustManagerFactory.getInstance(algorithm);
    loc[tmf] = (*env)->CallStaticObjectMethod(env, g_TrustManagerFactory, g_TrustManagerFactoryGetInstance, loc[algorithm]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // tmf.init(keyStore) -> NULL for system defaults, or custom KeyStore for custom trust roots
    (*env)->CallVoidMethod(env, loc[tmf], g_TrustManagerFactoryInit, customTrustKeyStore);
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

cleanup:
    RELEASE_LOCALS(loc, env);
    return result;
}

// Creates a KeyStore containing the given trusted certificates.
// Every certificate added via setCertificateEntry becomes a trust anchor —
// there is no Java equivalent of .NET's ExtraStore (chain-building helpers
// that are NOT trust anchors). This is why only root certificates should be
// passed here, not intermediates.
// Returns NULL if trustCerts is NULL or trustCertsLen is 0.
static jobject CreateTrustKeyStore(JNIEnv* env, jobject* trustCerts, int32_t trustCertsLen)
{
    if (trustCerts == NULL || trustCertsLen <= 0)
        return NULL;

    jobject keyStore = NULL;
    jstring ksType = NULL;

    // KeyStore keyStore = KeyStore.getInstance(KeyStore.getDefaultType());
    // keyStore.load(null, null);
    ksType = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetDefaultType);
    ON_EXCEPTION_PRINT_AND_GOTO(error);
    keyStore = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetInstance, ksType);
    ON_EXCEPTION_PRINT_AND_GOTO(error);
    (*env)->CallVoidMethod(env, keyStore, g_KeyStoreLoad, NULL, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    ReleaseLRef(env, ksType);
    ksType = NULL;

    for (int32_t i = 0; i < trustCertsLen; i++)
    {
        char alias[32];
        snprintf(alias, sizeof(alias), "trust_%d", i);
        jstring aliasStr = make_java_string(env, alias);

        // keyStore.setCertificateEntry(alias, cert);
        (*env)->CallVoidMethod(env, keyStore, g_KeyStoreSetCertificateEntry, aliasStr, trustCerts[i]);
        ReleaseLRef(env, aliasStr);
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }

    return keyStore;

error:
    ReleaseLRef(env, ksType);
    ReleaseLRef(env, keyStore);
    return NULL;
}

// Creates a DotnetProxyTrustManager wrapping the platform's X509TrustManager.
// The proxy consults Android's trust infrastructure first, then delegates to the
// managed SslStream validation callback. The platform's verdict (chainTrustedByPlatform)
// is passed to the managed side to be combined with managed validation — making
// the overall result more strict, never less (see SslStream.Android.cs).
jobjectArray GetTrustManagers(JNIEnv* env, intptr_t sslStreamProxyHandle, const char* targetHost, jobject* trustCerts, int32_t trustCertsLen)
{
    jobjectArray trustManagers = NULL;
    INIT_LOCALS(loc, trustKeyStore, platformTrustManager, dotnetProxyTrustManager);

    loc[trustKeyStore] = CreateTrustKeyStore(env, trustCerts, trustCertsLen);
    // If custom trust certs were requested but KeyStore creation failed, propagate the
    // failure rather than silently falling back to system trust (security downgrade).
    if (loc[trustKeyStore] == NULL && trustCerts != NULL && trustCertsLen > 0)
    {
        LOG_ERROR("Failed to create custom trust KeyStore");
        goto cleanup;
    }

    loc[platformTrustManager] = GetX509TrustManager(env, loc[trustKeyStore]);
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

int32_t AndroidCryptoNative_IsCleartextTrafficPermitted(const char* hostname)
{
    JNIEnv* env = GetJNIEnv();
    jstring hostnameStr = make_java_string(env, hostname);
    jboolean result = (*env)->CallStaticBooleanMethod(
        env,
        g_DotnetProxyTrustManager,
        g_DotnetProxyTrustManagerIsCleartextTrafficPermitted,
        hostnameStr);
    ReleaseLRef(env, hostnameStr);
    return (int32_t)result;
}

int32_t AndroidCryptoNative_IsCertificateTrustedForHost(const uint8_t* certDer, int32_t certDerLen, const char* hostname)
{
    JNIEnv* env = GetJNIEnv();
    jbyteArray certArray = make_java_byte_array(env, certDerLen);
    (*env)->SetByteArrayRegion(env, certArray, 0, certDerLen, (const jbyte*)certDer);
    jstring hostnameStr = make_java_string(env, hostname);
    jboolean result = (*env)->CallStaticBooleanMethod(
        env,
        g_DotnetProxyTrustManager,
        g_DotnetProxyTrustManagerIsCertificateTrustedForHost,
        certArray,
        hostnameStr);
    ReleaseLRef(env, certArray);
    ReleaseLRef(env, hostnameStr);
    return (int32_t)result;
}

// JNI entry point called from DotnetProxyTrustManager.verifyRemoteCertificate().
// Forwards the platform's trust verdict to the managed SslStream validation callback.
// The managed side combines this with its own X509Chain.Build result — the callback
// always receives the union of both assessments (more strict, never less).
ARGS_NON_NULL_ALL jboolean Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv* env, jobject thisHandle, jlong sslStreamProxyHandle, jboolean chainTrustedByPlatform)
{
    RemoteCertificateValidationCallback verify = atomic_load(&verifyRemoteCertificate);
    abort_unless(verify, "verifyRemoteCertificate callback has not been registered");
    return verify((intptr_t)sslStreamProxyHandle, (int32_t)chainTrustedByPlatform);
}
