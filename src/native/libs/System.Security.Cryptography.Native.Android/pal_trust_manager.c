#include "pal_trust_manager.h"
#include <stdatomic.h>
#include <stdio.h>

static _Atomic RemoteCertificateValidationCallback verifyRemoteCertificate;

// Cached JNI global ref to the default platform X509TrustManager (no custom KeyStore).
// Initializing TrustManagerFactory.init(null) is potentially expensive (reads the system
// keystore from disk); caching avoids paying that cost on every TLS handshake.
// A custom KeyStore variant cannot be cached because it is keyed on the user's trustCerts.
static _Atomic(jobject) g_cachedDefaultTrustManager = NULL;

ARGS_NON_NULL_ALL void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
{
    atomic_store(&verifyRemoteCertificate, callback);
}

// Gets the X509TrustManager from TrustManagerFactory, optionally initialized
// with a custom KeyStore containing trusted certificates. When customTrustKeyStore
// is NULL, the system default trust store is used (and the result is cached).
// When non-NULL, only the certificates in the custom KeyStore are trusted (Java's
// KeyStore.setCertificateEntry treats every entry as a trust anchor).
static jobject GetX509TrustManager(JNIEnv* env, jobject customTrustKeyStore)
{
    // Fast path: return a fresh local ref to the cached default trust manager.
    if (customTrustKeyStore == NULL)
    {
        jobject cached = atomic_load(&g_cachedDefaultTrustManager);
        if (cached != NULL)
        {
            return (*env)->NewLocalRef(env, cached);
        }
    }

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

    // Populate the cache for the default (no custom KeyStore) case. If a racing thread
    // beat us to it, drop our new global ref and keep using the cached one.
    if (result != NULL && customTrustKeyStore == NULL)
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

// Test-only helper. NOT supported as a public API surface — these wrappers
// exist solely so AndroidPlatformTrustTests can directly introspect Android's
// platform trust behaviour (network_security_config.xml) without relying on
// a live network. Do not call from product code.
int32_t AndroidCryptoNative_TestOnly_IsCleartextTrafficPermitted(const char* hostname)
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

// Test-only helper. See AndroidCryptoNative_TestOnly_IsCleartextTrafficPermitted.
int32_t AndroidCryptoNative_TestOnly_IsCertificateTrustedForHost(const uint8_t* certDer, int32_t certDerLen, const char* hostname)
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
