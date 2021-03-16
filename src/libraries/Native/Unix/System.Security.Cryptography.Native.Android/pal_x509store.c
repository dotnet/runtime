// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509store.h"
#include "pal_eckey.h"
#include "pal_misc.h"
#include "pal_rsa.h"

#include <assert.h>
#include <stdbool.h>
#include <string.h>

#define INIT_LOCALS(name, ...) \
    enum { __VA_ARGS__, count_##name }; \
    jobject name[count_##name] = { 0 } \

#define RELEASE_LOCALS(name, env) \
do { \
    for (int i_##name = 0; i_##name < count_##name; ++i_##name) \
    { \
        jobject local = name[i_##name]; \
        if (local != NULL) \
            (*env)->DeleteLocalRef(env, local); \
    } \
} while (0)

static jstring CreateAliasForCertificate(JNIEnv* env, jobject /*X509Certificate*/ cert)
{
    // Use the certificate's hash code as a unique alias
    int hashCode = (*env)->CallIntMethod(env, cert, g_X509CertHashCode);

    char buffer[9] = {0}; // 8-character hex + null terminator
    size_t written = (size_t)snprintf(buffer, sizeof(buffer), "%08X", hashCode);
    assert(written == sizeof(buffer) - 1);

    return (*env)->NewStringUTF(env, buffer);
}

static bool TryGetExistingCertificateAlias(JNIEnv* env,
                                           jobject /*KeyStore*/ store,
                                           jobject /*X509Certificate*/ cert,
                                           jobject* outAlias)
{
    // String alias = store.getCertificateAlias(cert);
    jstring alias = (*env)->CallObjectMethod(env, store, g_KeyStoreGetCertificateAlias, cert);
    bool containsCert = alias != NULL;
    if (outAlias != NULL)
    {
        *outAlias = alias;
    }
    else
    {
        (*env)->DeleteLocalRef(env, alias);
    }

    return containsCert;
}

int32_t AndroidCryptoNative_X509StoreAddCertificate(jobject /*KeyStore*/ store, jobject /*X509Certificate*/ cert)
{
    assert(store != NULL);
    assert(cert != NULL);

    JNIEnv* env = GetJNIEnv();

    if (TryGetExistingCertificateAlias(env, store, cert, NULL /*outAlias*/))
    {
        // Certificate is already in store - nothing to do
        return SUCCESS;
    }

    jstring alias = CreateAliasForCertificate(env, cert);

    // store.setCertificateEntry(alias, cert);
    (*env)->CallVoidMethod(env, store, g_KeyStoreSetCertificateEntry, alias, cert);
    (*env)->DeleteLocalRef(env, alias);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_X509StoreAddCertificateWithPrivateKey(jobject /*KeyStore*/ store,
                                                                  jobject /*X509Certificate*/ cert,
                                                                  void* key,
                                                                  PAL_KeyAlgorithm algorithm)
{
    assert(store != NULL);
    assert(cert != NULL);
    assert(key != NULL);

    int32_t ret = FAIL;
    JNIEnv* env = GetJNIEnv();

    INIT_LOCALS(loc, alias, certs);

    bool releasePrivateKey = true;
    jobject privateKey;
    switch (algorithm)
    {
        case PAL_EC:
        {
            EC_KEY* ec = (EC_KEY*)key;
            privateKey = (*env)->CallObjectMethod(env, ec->keyPair, g_keyPairGetPrivateMethod);
            break;
        }
        case PAL_DSA:
        {
            // key is a KeyPair jobject
            privateKey = (*env)->CallObjectMethod(env, key, g_keyPairGetPrivateMethod);
            break;
        }
        case PAL_RSA:
        {
            RSA* rsa = (RSA*)key;
            privateKey = rsa->privateKey;
            releasePrivateKey = false; // Private key is a global ref stored directly on RSA handle
            break;
        }
        default:
            return FAIL;
    }

    loc[alias] = CreateAliasForCertificate(env, cert);

    // X509Certificate[] certs = new X509Certificate[] { cert };
    // store.setKeyEntry(alias, privateKey, null, certs);
    loc[certs] = (*env)->NewObjectArray(env, 1, g_X509CertClass, cert);
    (*env)->CallVoidMethod(env, store, g_KeyStoreSetKeyEntry, loc[alias], privateKey, NULL, loc[certs]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    if (releasePrivateKey)
    {
        (*env)->DeleteLocalRef(env, privateKey);
    }

    return ret;
}

bool AndroidCryptoNative_X509StoreContainsCertificate(jobject /*KeyStore*/ store, jobject /*X509Certificate*/ cert)
{
    assert(store != NULL);
    assert(cert != NULL);

    JNIEnv* env = GetJNIEnv();
    return TryGetExistingCertificateAlias(env, store, cert, NULL /*outAlias*/);
}

static void* HandleFromKeys(JNIEnv* env,
                            jobject /*PublicKey*/ publicKey,
                            jobject /*PrivateKey*/ privateKey,
                            PAL_KeyAlgorithm* algorithm)
{
    if ((*env)->IsInstanceOf(env, privateKey, g_DSAKeyClass))
    {
        *algorithm = PAL_DSA;
        return AndroidCryptoNative_CreateKeyPair(env, publicKey, privateKey);
    }
    else if ((*env)->IsInstanceOf(env, privateKey, g_ECKeyClass))
    {
        *algorithm = PAL_EC;
        return AndroidCryptoNative_NewEcKeyFromKeys(env, publicKey, privateKey);
    }
    else if ((*env)->IsInstanceOf(env, privateKey, g_RSAKeyClass))
    {
        *algorithm = PAL_RSA;
        return AndroidCryptoNative_NewRsaFromKeys(env, publicKey, privateKey);
    }

    LOG_INFO("Ignoring unknown privake key type");
    return NULL;
}

static int32_t EnumerateCertificates(JNIEnv* env, jobject /*KeyStore*/ store, EnumCertificatesCallback cb, void* context)
{
    int32_t ret = FAIL;

    // Enumeration<String> aliases = store.aliases();
    jobject aliases = (*env)->CallObjectMethod(env, store, g_KeyStoreAliases);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // while (aliases.hasMoreElements()) {
    //     String alias = aliases.nextElement();
    //     KeyStore.Entry entry = store.getEntry(alias);
    //     if (entry instanceof KeyStore.PrivateKeyEntry) {
    //         ...
    //     } else if (entry instanceof KeyStore.TrustedCertificateEntry) {
    //         ...
    //     }
    // }
    jboolean hasNext = (*env)->CallBooleanMethod(env, aliases, g_EnumerationHasMoreElements);
    while (hasNext)
    {
        INIT_LOCALS(loc, alias, entry, cert, publicKey, privateKey);

        loc[alias] = (*env)->CallObjectMethod(env, aliases, g_EnumerationNextElement);
        ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);

        loc[entry] = (*env)->CallObjectMethod(env, store, g_KeyStoreGetEntry, loc[alias], NULL);
        ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);

        if ((*env)->IsInstanceOf(env, loc[entry], g_PrivateKeyEntryClass))
        {
            // Certificate cert = entry.getCertificate();
            // Public publicKey = cert.getPublicKey();
            // PrivateKey privateKey = entry.getPrivateKey();
            loc[cert] = (*env)->CallObjectMethod(env, loc[entry], g_PrivateKeyEntryGetCertificate);
            loc[publicKey] = (*env)->CallObjectMethod(env, loc[cert], g_X509CertGetPublicKey);
            loc[privateKey] = (*env)->CallObjectMethod(env, loc[entry], g_PrivateKeyEntryGetPrivateKey);

            PAL_KeyAlgorithm keyAlgorithm = PAL_UnknownAlgorithm;
            void* keyHandle = HandleFromKeys(env, loc[publicKey], loc[privateKey], &keyAlgorithm);
            if (keyHandle != NULL)
            {
                cb(AddGRef(env, loc[cert]), keyHandle, keyAlgorithm, context);
            }
        }
        else if ((*env)->IsInstanceOf(env, loc[entry], g_TrustedCertificateEntryClass))
        {
            // Certificate cert = entry.getTrustedCertificate();
            loc[cert] = (*env)->CallObjectMethod(env, loc[entry], g_TrustedCertificateEntryGetTrustedCertificate);
            cb(AddGRef(env, loc[cert]), NULL /*privateKey*/, PAL_UnknownAlgorithm, context);
        }

    loop_cleanup:
        RELEASE_LOCALS(loc, env);

        hasNext = (*env)->CallBooleanMethod(env, aliases, g_EnumerationHasMoreElements);
    }

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, aliases);
    return ret;
}

int32_t AndroidCryptoNative_X509StoreEnumerateCertificates(jobject /*KeyStore*/ store,
                                                        EnumCertificatesCallback cb,
                                                        void* context)
{
    assert(store != NULL);
    assert(cb != NULL);

    JNIEnv* env = GetJNIEnv();
    return EnumerateCertificates(env, store, cb, context);
}

static bool SystemAliasFilter(JNIEnv* env, jstring alias)
{
    const char systemPrefix[] = "system:";
    size_t prefixLen = (sizeof(systemPrefix) - 1) / sizeof(char);

    const char* aliasPtr = (*env)->GetStringUTFChars(env, alias, NULL);
    bool isSystem = (strncmp(aliasPtr, systemPrefix, prefixLen) == 0);
    (*env)->ReleaseStringUTFChars(env, alias, aliasPtr);
    return isSystem;
}

typedef bool (*FilterAliasFunction)(JNIEnv* env, jstring alias);
static int32_t EnumerateTrustedCertificates(
    JNIEnv* env, jobject /*KeyStore*/ store, bool systemOnly, EnumTrustedCertificatesCallback cb, void* context)
{
    int32_t ret = FAIL;

    // Filter to only system certificates if necessary
    // System certificates are included for 'current user' (matches Windows)
    FilterAliasFunction filter = systemOnly ? &SystemAliasFilter : NULL;

    // Enumeration<String> aliases = store.aliases();
    jobject aliases = (*env)->CallObjectMethod(env, store, g_KeyStoreAliases);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // while (aliases.hasMoreElements()) {
    //     String alias = aliases.nextElement();
    //     X509Certificate cert = (X509Certificate)store.getCertificate(alias);
    //     if (cert != null) {
    //         cb(cert, context);
    //     }
    // }
    jboolean hasNext = (*env)->CallBooleanMethod(env, aliases, g_EnumerationHasMoreElements);
    while (hasNext)
    {
        jstring alias = (*env)->CallObjectMethod(env, aliases, g_EnumerationNextElement);
        ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);

        if (filter == NULL || filter(env, alias))
        {
            jobject cert = (*env)->CallObjectMethod(env, store, g_KeyStoreGetCertificate, alias);
            if (cert != NULL && !CheckJNIExceptions(env))
            {
                cert = ToGRef(env, cert);
                cb(cert, context);
            }
        }

        hasNext = (*env)->CallBooleanMethod(env, aliases, g_EnumerationHasMoreElements);

    loop_cleanup:
        (*env)->DeleteLocalRef(env, alias);
    }

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, aliases);
    return ret;
}

int32_t AndroidCryptoNative_X509StoreEnumerateTrustedCertificates(bool systemOnly,
                                                                  EnumTrustedCertificatesCallback cb,
                                                                  void* context)
{
    assert(cb != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, storeType, store);

    // KeyStore store = KeyStore.getInstance("AndroidCAStore");
    // store.load(null, null);
    loc[storeType] = JSTRING("AndroidCAStore");
    loc[store] = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetInstance, loc[storeType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[store], g_KeyStoreLoad, NULL, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = EnumerateTrustedCertificates(env, loc[store], systemOnly, cb, context);

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

jobject /*KeyStore*/ AndroidCryptoNative_X509StoreOpenDefault(void)
{
    JNIEnv* env = GetJNIEnv();
    jobject ret = NULL;

    // KeyStore store = KeyStore.getInstance("AndroidKeyStore");
    // store.load(null, null);
    jstring storeType = JSTRING("AndroidKeyStore");
    jobject store = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetInstance, storeType);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, store, g_KeyStoreLoad, NULL, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = ToGRef(env, store);

cleanup:
    (*env)->DeleteLocalRef(env, storeType);
    return ret;
}

int32_t AndroidCryptoNative_X509StoreRemoveCertificate(jobject /*KeyStore*/ store, jobject /*X509Certificate*/ cert)
{
    assert(store != NULL);

    JNIEnv* env = GetJNIEnv();

    jstring alias = NULL;
    if (!TryGetExistingCertificateAlias(env, store, cert, &alias))
    {
        // Certificate is not in store - nothing to do
        return SUCCESS;
    }

    // store.deleteEntry(alias);
    (*env)->CallVoidMethod(env, store, g_KeyStoreDeleteEntry, alias);

    (*env)->DeleteLocalRef(env, alias);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}
