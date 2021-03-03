// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509store.h"

#include <assert.h>
#include <stdbool.h>
#include <string.h>

#define INIT_LOCALS(name, ...) \
    enum { __VA_ARGS__, count_##name }; \
    jobject name[count_##name] = { 0 }; \

#define RELEASE_LOCALS(name, env) \
{ \
    for (int i_##name = 0; i_##name < count_##name; ++i_##name) \
    { \
        jobject local = name[i_##name]; \
        if (local != NULL) \
            (*env)->DeleteLocalRef(env, local); \
    } \
} \

int32_t AndroidCryptoNative_X509StoreAddCertificate(jobject /*KeyStore*/ store,
                                                    jobject /*X509Certificate*/ cert,
                                                    const char* thumbprint)
{
    assert(store != NULL);
    assert(cert != NULL);
    assert(thumbprint != NULL);

    JNIEnv* env = GetJNIEnv();
    jstring alias = JSTRING(thumbprint);

    // store.setCertificateEntry(alias, cert);
    (*env)->CallVoidMethod(env, store, g_KeyStoreSetCertificateEntry, alias, cert);
    (*env)->DeleteLocalRef(env, alias);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

bool AndroidCryptoNative_X509StoreContainsCertificate(jobject /*KeyStore*/ store, const char* thumbprint)
{
    assert(store != NULL);
    assert(thumbprint != NULL);

    JNIEnv* env = GetJNIEnv();
    jstring alias = JSTRING(thumbprint);

    // store.isCertificateEntry(alias);
    jboolean containsCert = (*env)->CallBooleanMethod(env, store, g_KeyStoreIsCertificateEntry, alias);
    (*env)->DeleteLocalRef(env, alias);
    return containsCert;
}

typedef bool (*FilterAliasFunction)(JNIEnv* env, jstring alias);
static void EnumerateCertificates(
    JNIEnv* env, jobject /*KeyStore*/ store, FilterAliasFunction filter, EnumCertificatesCallback cb, void* context)
{
    // Enumeration<String> aliases = store.aliases();
    jobject aliases = (*env)->CallObjectMethod(env, store, g_KeyStoreAliases);

    // while (aliases.hasMoreElements()) {
    //     String alias = aliases.nextElement();
    //     X509Certificate cert = (X509Certificate)store.getCertificate(alias);
    //     if (cert != null)
    //         cb(cert, context);
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

    (*env)->DeleteLocalRef(env, aliases);
}

void AndroidCryptoNative_X509StoreEnumerateCertificates(jobject /*KeyStore*/ store,
                                                        EnumCertificatesCallback cb,
                                                        void* context)
{
    assert(store != NULL);
    assert(cb != NULL);

    JNIEnv* env = GetJNIEnv();
    EnumerateCertificates(env, store, NULL /*filter*/, cb, context);
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

int32_t
AndroidCryptoNative_X509StoreEnumerateTrustedCertificates(bool systemOnly, EnumCertificatesCallback cb, void* context)
{
    assert(cb != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, storeType, store)

    // KeyStore store = KeyStore.getInstance("AndroidCAStore");
    // store.load(null, null);
    loc[storeType] = JSTRING("AndroidCAStore");
    loc[store] = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetInstance, loc[storeType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[store], g_KeyStoreLoad, NULL, NULL);

    // Filter to only system certificates if necessary
    // System certificates are included for 'current user' (matches Windows)
    FilterAliasFunction filter = systemOnly ? &SystemAliasFilter : NULL;
    EnumerateCertificates(env, loc[store], filter, cb, context);
    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env)
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
    ret = ToGRef(env, store);

cleanup:
    (*env)->DeleteLocalRef(env, storeType);
    return ret;
}

int32_t AndroidCryptoNative_X509StoreRemoveCertificate(jobject /*KeyStore*/ store, const char* thumbprint)
{
    assert(store != NULL);
    assert(thumbprint != NULL);

    JNIEnv* env = GetJNIEnv();
    jstring alias = JSTRING(thumbprint);

    // if (store.isCertificateEntry(alias)) {
    //     store.deleteEntry(alias);
    // }
    jboolean isCert = (*env)->CallBooleanMethod(env, store, g_KeyStoreIsCertificateEntry, alias);
    if (isCert)
    {
        (*env)->CallVoidMethod(env, store, g_KeyStoreDeleteEntry, alias);
    }

    (*env)->DeleteLocalRef(env, alias);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}
