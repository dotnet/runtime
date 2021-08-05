// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509store.h"
#include "pal_eckey.h"
#include "pal_misc.h"
#include "pal_rsa.h"

#include <assert.h>
#include <stdbool.h>
#include <string.h>

typedef enum
{
    EntryFlags_None = 0,
    EntryFlags_HasCertificate = 1,
    EntryFlags_HasPrivateKey = 2,
    EntryFlags_MatchesCertificate = 4,
} EntryFlags;

// Returns whether or not the store contains the specified alias
// If the entry exists, the flags parameter is set based on the contents of the entry
ARGS_NON_NULL_ALL static bool ContainsEntryForAlias(
    JNIEnv* env, jobject /*KeyStore*/ store, jobject /*X509Certificate*/ cert, jstring alias, EntryFlags* flags)
{
    bool ret = false;
    EntryFlags flagsLocal = EntryFlags_None;

    INIT_LOCALS(loc, entry, existingCert);

    bool containsAlias = (*env)->CallBooleanMethod(env, store, g_KeyStoreContainsAlias, alias);
    if (!containsAlias)
        goto cleanup;

    ret = true;

    // KeyStore.Entry entry = store.getEntry(alias, null);
    // if (entry instanceof KeyStore.PrivateKeyEntry) {
    //     existingCert = ((KeyStore.PrivateKeyEntry)entry).getCertificate();
    // } else if (entry instanceof KeyStore.TrustedCertificateEntry) {
    //     existingCert = ((KeyStore.TrustedCertificateEntry)entry).getTrustedCertificate();
    // }
    loc[entry] = (*env)->CallObjectMethod(env, store, g_KeyStoreGetEntry, alias, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if ((*env)->IsInstanceOf(env, loc[entry], g_PrivateKeyEntryClass))
    {
        // Private key entries always have a certificate
        flagsLocal |= EntryFlags_HasCertificate;
        flagsLocal |= EntryFlags_HasPrivateKey;
        loc[existingCert] = (*env)->CallObjectMethod(env, loc[entry], g_PrivateKeyEntryGetCertificate);
    }
    else if ((*env)->IsInstanceOf(env, loc[entry], g_TrustedCertificateEntryClass))
    {
        flagsLocal |= EntryFlags_HasCertificate;
        loc[existingCert] = (*env)->CallObjectMethod(env, loc[entry], g_TrustedCertificateEntryGetTrustedCertificate);
    }
    else
    {
        // Entry for alias exists, but doesn't represent a certificate or private key + certificate
        goto cleanup;
    }

    assert(loc[existingCert] != NULL);
    if ((*env)->CallBooleanMethod(env, cert, g_X509CertEquals, loc[existingCert]))
    {
        flagsLocal |= EntryFlags_MatchesCertificate;
    }

cleanup:
    RELEASE_LOCALS(loc, env);
    *flags = flagsLocal;
    return ret;
}

ARGS_NON_NULL_ALL
static bool ContainsMatchingCertificateForAlias(JNIEnv* env,
                                                jobject /*KeyStore*/ store,
                                                jobject /*X509Certificate*/ cert,
                                                jstring alias)
{
    EntryFlags flags;
    if (!ContainsEntryForAlias(env, store, cert, alias, &flags))
        return false;

    EntryFlags matchesFlags = EntryFlags_HasCertificate & EntryFlags_MatchesCertificate;
    return (flags & matchesFlags) == matchesFlags;
}

int32_t AndroidCryptoNative_X509StoreAddCertificate(jobject /*KeyStore*/ store,
                                                    jobject /*X509Certificate*/ cert,
                                                    const char* hashString)
{
    abort_if_invalid_pointer_argument (store);
    abort_if_invalid_pointer_argument (cert);
    abort_if_invalid_pointer_argument (hashString);

    JNIEnv* env = GetJNIEnv();

    jstring alias = make_java_string(env, hashString);
    EntryFlags flags;
    if (ContainsEntryForAlias(env, store, cert, alias, &flags))
    {
        ReleaseLRef(env, alias);
        EntryFlags matchesFlags = EntryFlags_HasCertificate & EntryFlags_MatchesCertificate;
        if ((flags & matchesFlags) != matchesFlags)
        {
            LOG_ERROR("Store already contains alias with entry that does not match the expected certificate");
            return FAIL;
        }

        // Certificate is already in store - nothing to do
        LOG_DEBUG("Store already contains certificate");
        return SUCCESS;
    }

    // store.setCertificateEntry(alias, cert);
    (*env)->CallVoidMethod(env, store, g_KeyStoreSetCertificateEntry, alias, cert);
    (*env)->DeleteLocalRef(env, alias);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t AndroidCryptoNative_X509StoreAddCertificateWithPrivateKey(jobject /*KeyStore*/ store,
                                                                  jobject /*X509Certificate*/ cert,
                                                                  void* key,
                                                                  PAL_KeyAlgorithm algorithm,
                                                                  const char* hashString)
{
    abort_if_invalid_pointer_argument (store);
    abort_if_invalid_pointer_argument (cert);
    abort_if_invalid_pointer_argument (key);
    abort_if_invalid_pointer_argument (hashString);

    int32_t ret = FAIL;
    JNIEnv* env = GetJNIEnv();

    INIT_LOCALS(loc, alias, certs);
    jobject privateKey = NULL;

    loc[alias] = make_java_string(env, hashString);

    EntryFlags flags;
    if (ContainsEntryForAlias(env, store, cert, loc[alias], &flags))
    {
        EntryFlags matchesFlags = EntryFlags_HasCertificate & EntryFlags_MatchesCertificate;
        if ((flags & matchesFlags) != matchesFlags)
        {
            RELEASE_LOCALS(loc, env);
            LOG_ERROR("Store already contains alias with entry that does not match the expected certificate");
            return FAIL;
        }

        if ((flags & EntryFlags_HasPrivateKey) == EntryFlags_HasPrivateKey)
        {
            RELEASE_LOCALS(loc, env);
            // Certificate with private key is already in store - nothing to do
            LOG_DEBUG("Store already contains certificate with private key");
            return SUCCESS;
        }

        // Delete existing entry. We will replace the existing cert with the cert + private key.
        // store.deleteEntry(alias);
        (*env)->CallVoidMethod(env, store, g_KeyStoreDeleteEntry, loc[alias]);
    }

    bool releasePrivateKey = true;
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
        {
            releasePrivateKey = false;
            LOG_ERROR("Unknown algorithm for private key");
            goto cleanup;
        }
    }

    // X509Certificate[] certs = new X509Certificate[] { cert };
    // store.setKeyEntry(alias, privateKey, null, certs);
    loc[certs] = make_java_object_array(env, 1, g_X509CertClass, cert);
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

bool AndroidCryptoNative_X509StoreContainsCertificate(jobject /*KeyStore*/ store,
                                                      jobject /*X509Certificate*/ cert,
                                                      const char* hashString)
{
    abort_if_invalid_pointer_argument (store);
    abort_if_invalid_pointer_argument (cert);
    abort_if_invalid_pointer_argument (hashString);

    JNIEnv* env = GetJNIEnv();
    jstring alias = make_java_string(env, hashString);

    bool containsCert = ContainsMatchingCertificateForAlias(env, store, cert, alias);
    (*env)->DeleteLocalRef(env, alias);
    return containsCert;
}

ARGS_NON_NULL_ALL
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

    LOG_INFO("Ignoring unknown private key type");
    *algorithm = PAL_UnknownAlgorithm;
    return NULL;
}

ARGS_NON_NULL_ALL static int32_t
EnumerateCertificates(JNIEnv* env, jobject /*KeyStore*/ store, EnumCertificatesCallback cb, void* context)
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

            // Private key entries always have a certificate.
            // For key algorithms we recognize, the certificate and private key handle are given to the callback.
            // For key algorithms we do not recognize, only the certificate will be given to the callback.
            cb(AddGRef(env, loc[cert]), keyHandle, keyAlgorithm, context);
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
    abort_if_invalid_pointer_argument (store);
    abort_if_invalid_pointer_argument (cb);

    JNIEnv* env = GetJNIEnv();
    return EnumerateCertificates(env, store, cb, context);
}

ARGS_NON_NULL_ALL
static bool SystemAliasFilter(JNIEnv* env, jstring alias)
{
    const char systemPrefix[] = "system:";
    size_t prefixLen = (sizeof(systemPrefix) / sizeof(*systemPrefix)) - 1;

    const char* aliasPtr = (*env)->GetStringUTFChars(env, alias, NULL);
    bool isSystem = (strncmp(aliasPtr, systemPrefix, prefixLen) == 0);
    (*env)->ReleaseStringUTFChars(env, alias, aliasPtr);
    return isSystem;
}

typedef bool (*FilterAliasFunction)(JNIEnv* env, jstring alias);
ARGS_NON_NULL_ALL static int32_t EnumerateTrustedCertificates(
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
    abort_if_invalid_pointer_argument (cb);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, storeType, store);

    // KeyStore store = KeyStore.getInstance("AndroidCAStore");
    // store.load(null, null);
    loc[storeType] = make_java_string(env, "AndroidCAStore");
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
    jstring storeType = make_java_string(env, "AndroidKeyStore");
    jobject store = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetInstance, storeType);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, store, g_KeyStoreLoad, NULL, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = ToGRef(env, store);

cleanup:
    (*env)->DeleteLocalRef(env, storeType);
    return ret;
}

int32_t AndroidCryptoNative_X509StoreRemoveCertificate(jobject /*KeyStore*/ store,
                                                       jobject /*X509Certificate*/ cert,
                                                       const char* hashString)
{
    abort_if_invalid_pointer_argument (store);

    JNIEnv* env = GetJNIEnv();

    jstring alias = make_java_string(env, hashString);
    if (!ContainsMatchingCertificateForAlias(env, store, cert, alias))
    {
        // Certificate is not in store - nothing to do
        return SUCCESS;
    }

    // store.deleteEntry(alias);
    (*env)->CallVoidMethod(env, store, g_KeyStoreDeleteEntry, alias);

    (*env)->DeleteLocalRef(env, alias);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}
