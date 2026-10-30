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

// Determines whether the store contains the specified alias, populating *flags
// with the entry's contents when present.
// Returns SUCCESS if the lookup completed (regardless of whether an entry exists),
// FAIL if a JNI exception was encountered while inspecting the store entry.
// On SUCCESS, *contains indicates whether an entry exists for the alias.
ARGS_NON_NULL_ALL static int32_t ContainsEntryForAlias(
    JNIEnv* env, jobject /*KeyStore*/ store, jobject /*X509Certificate*/ cert, jstring alias, bool* contains, EntryFlags* flags)
{
    int32_t ret = FAIL;
    EntryFlags flagsLocal = EntryFlags_None;
    bool containsLocal = false;

    INIT_LOCALS(loc, entry, existingCert);

    bool containsAlias = (*env)->CallBooleanMethod(env, store, g_KeyStoreContainsAlias, alias);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (!containsAlias)
    {
        ret = SUCCESS;
        goto cleanup;
    }

    containsLocal = true;

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
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }
    else if ((*env)->IsInstanceOf(env, loc[entry], g_TrustedCertificateEntryClass))
    {
        flagsLocal |= EntryFlags_HasCertificate;
        loc[existingCert] = (*env)->CallObjectMethod(env, loc[entry], g_TrustedCertificateEntryGetTrustedCertificate);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }
    else
    {
        // Entry for alias exists, but doesn't represent a certificate or private key + certificate
        ret = SUCCESS;
        goto cleanup;
    }

    assert(loc[existingCert] != NULL);
    jboolean equals = (*env)->CallBooleanMethod(env, cert, g_X509CertEquals, loc[existingCert]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (equals)
    {
        flagsLocal |= EntryFlags_MatchesCertificate;
    }

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    if (ret == SUCCESS)
    {
        *contains = containsLocal;
        *flags = flagsLocal;
    }
    return ret;
}

// Determines whether the store contains the specified alias with a matching certificate.
// Returns SUCCESS if the lookup completed (regardless of match result), FAIL on a JNI
// exception while inspecting the entry. On SUCCESS, *matches indicates whether the entry
// exists AND its certificate matches the supplied cert.
ARGS_NON_NULL_ALL
static int32_t ContainsMatchingCertificateForAlias(JNIEnv* env,
                                                   jobject /*KeyStore*/ store,
                                                   jobject /*X509Certificate*/ cert,
                                                   jstring alias,
                                                   bool* matches)
{
    EntryFlags flags;
    bool contains = false;
    *matches = false;

    if (ContainsEntryForAlias(env, store, cert, alias, &contains, &flags) != SUCCESS)
        return FAIL;

    if (!contains)
        return SUCCESS;

    EntryFlags matchesFlags = EntryFlags_HasCertificate & EntryFlags_MatchesCertificate;
    *matches = (flags & matchesFlags) == matchesFlags;
    return SUCCESS;
}

int32_t AndroidCryptoNative_X509StoreAddCertificate(jobject /*KeyStore*/ store,
                                                    jobject /*X509Certificate*/ cert,
                                                    const char* hashString)
{
    abort_if_invalid_pointer_argument (store);
    abort_if_invalid_pointer_argument (cert);
    abort_if_invalid_pointer_argument (hashString);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    INIT_LOCALS(loc, alias);
    loc[alias] = make_java_string(env, hashString);

    EntryFlags flags;
    bool contains = false;
    if (ContainsEntryForAlias(env, store, cert, loc[alias], &contains, &flags) != SUCCESS)
        goto cleanup;

    if (contains)
    {
        EntryFlags matchesFlags = EntryFlags_HasCertificate & EntryFlags_MatchesCertificate;
        if ((flags & matchesFlags) != matchesFlags)
        {
            LOG_ERROR("Store already contains alias with entry that does not match the expected certificate");
            goto cleanup;
        }

        // Certificate is already in store - nothing to do
        LOG_DEBUG("Store already contains certificate");
        ret = SUCCESS;
        goto cleanup;
    }

    // store.setCertificateEntry(alias, cert);
    (*env)->CallVoidMethod(env, store, g_KeyStoreSetCertificateEntry, loc[alias], cert);
    ret = CheckJNIExceptions(env) ? FAIL : SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
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
    bool releasePrivateKey = true;

    loc[alias] = make_java_string(env, hashString);

    EntryFlags flags;
    bool contains = false;
    if (ContainsEntryForAlias(env, store, cert, loc[alias], &contains, &flags) != SUCCESS)
        goto cleanup;
    if (contains)
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
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    switch (algorithm)
    {
        case PAL_EC:
        {
            EC_KEY* ec = (EC_KEY*)key;
            privateKey = (*env)->CallObjectMethod(env, ec->keyPair, g_keyPairGetPrivateMethod);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
            break;
        }
        case PAL_DSA:
        {
            // key is a KeyPair jobject
            privateKey = (*env)->CallObjectMethod(env, key, g_keyPairGetPrivateMethod);
            ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
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
        ReleaseLRef(env, privateKey);
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
    INIT_LOCALS(loc, alias);
    loc[alias] = make_java_string(env, hashString);

    bool containsCert = false;
    if (ContainsMatchingCertificateForAlias(env, store, cert, loc[alias], &containsCert) != SUCCESS)
    {
        // Lookup failed (JNI exception). Treat as "not contained" so the exception
        // doesn't leak back to managed code.
        containsCert = false;
    }

    RELEASE_LOCALS(loc, env);
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
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
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
            ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);
            loc[publicKey] = (*env)->CallObjectMethod(env, loc[cert], g_X509CertGetPublicKey);
            ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);
            loc[privateKey] = (*env)->CallObjectMethod(env, loc[entry], g_PrivateKeyEntryGetPrivateKey);
            ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);

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
            ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);
            cb(AddGRef(env, loc[cert]), NULL /*privateKey*/, PAL_UnknownAlgorithm, context);
        }

    loop_cleanup:
        // Per-entry exceptions have been logged and cleared via ON_EXCEPTION_PRINT_AND_GOTO.
        // We silently skip the failed entry and continue, preserving the original behavior.
        RELEASE_LOCALS(loc, env);

        hasNext = (*env)->CallBooleanMethod(env, aliases, g_EnumerationHasMoreElements);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
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
    if (aliasPtr == NULL)
    {
        CheckJNIExceptions(env);
        return false;
    }
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
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    while (hasNext)
    {
        jobject cert = NULL;
        jstring alias = (*env)->CallObjectMethod(env, aliases, g_EnumerationNextElement);
        ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);

        if (filter == NULL || filter(env, alias))
        {
            cert = (*env)->CallObjectMethod(env, store, g_KeyStoreGetCertificate, alias);
            if (!CheckJNIExceptions(env) && cert != NULL)
            {
                cb(AddGRef(env, cert), context);
            }
        }

    loop_cleanup:
        ReleaseLRef(env, cert);
        ReleaseLRef(env, alias);

        hasNext = (*env)->CallBooleanMethod(env, aliases, g_EnumerationHasMoreElements);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    ret = SUCCESS;

cleanup:
    ReleaseLRef(env, aliases);
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
    store = NULL;

cleanup:
    ReleaseLRef(env, store);
    ReleaseLRef(env, storeType);
    return ret;
}

int32_t AndroidCryptoNative_X509StoreRemoveCertificate(jobject /*KeyStore*/ store,
                                                       jobject /*X509Certificate*/ cert,
                                                       const char* hashString)
{
    abort_if_invalid_pointer_argument (store);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    INIT_LOCALS(loc, alias);

    loc[alias] = make_java_string(env, hashString);
    bool containsCert = false;
    if (ContainsMatchingCertificateForAlias(env, store, cert, loc[alias], &containsCert) != SUCCESS)
        goto cleanup;
    if (!containsCert)
    {
        // Certificate is not in store - nothing to do
        ret = SUCCESS;
        goto cleanup;
    }

    // store.deleteEntry(alias);
    (*env)->CallVoidMethod(env, store, g_KeyStoreDeleteEntry, loc[alias]);
    ret = CheckJNIExceptions(env) ? FAIL : SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

jobject AndroidCryptoNative_X509StoreGetPrivateKeyEntry(jobject /*KeyStore*/ store, const char* hashString)
{
    abort_if_invalid_pointer_argument (store);

    JNIEnv* env = GetJNIEnv();
    INIT_LOCALS(loc, alias);

    jobject privateKeyEntry = NULL;

    loc[alias] = make_java_string(env, hashString);

    privateKeyEntry = (*env)->CallObjectMethod(env, store, g_KeyStoreGetEntry, loc[alias], NULL);
    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, privateKeyEntry);
        goto cleanup;
    }

    bool isPrivateKeyEntry = (*env)->IsInstanceOf(env, privateKeyEntry, g_PrivateKeyEntryClass);
    if (!isPrivateKeyEntry)
    {
        ReleaseLRef(env, privateKeyEntry);
        privateKeyEntry = NULL;
        goto cleanup;
    }

    privateKeyEntry = ToGRef(env, privateKeyEntry);

cleanup:
    RELEASE_LOCALS(loc, env);
    return privateKeyEntry;
}

int32_t AndroidCryptoNative_X509StoreDeleteEntry(jobject /*KeyStore*/ store, const char* hashString)
{
    int32_t ret = FAIL;

    abort_if_invalid_pointer_argument (store);

    JNIEnv* env = GetJNIEnv();
    INIT_LOCALS(loc, alias);

    loc[alias] = make_java_string(env, hashString);

    // store.deleteEntry(alias);
    (*env)->CallVoidMethod(env, store, g_KeyStoreDeleteEntry, loc[alias]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}
