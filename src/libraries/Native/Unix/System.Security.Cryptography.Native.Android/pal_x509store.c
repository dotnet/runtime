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

int32_t AndroidCryptoNative_X509StoreEnumerateTrustedCertificates(bool systemOnly, EnumCertificatesCallback cb, void* context)
{
    assert(cb != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, storeType, store, aliases)

    // KeyStore store = KeyStore.getInstance("AndroidCAStore");
    // store.load(null, null);
    // Enumeration<String> aliases = store.aliases();
    loc[storeType] = JSTRING("AndroidCAStore");
    loc[store] = (*env)->CallStaticObjectMethod(env, g_KeyStore, g_KeyStoreGetInstance, loc[storeType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[store], g_KeyStoreLoad, NULL, NULL);
    loc[aliases] = (*env)->CallObjectMethod(env, loc[store], g_KeyStoreAliases);

    // while (aliases.hasMoreElements()) {
    //     String alias = aliases.nextElement();
    //     X509Certificate cert = (X509Certificate)store.getCertificate(alias);
    //     cb(cert, context);
    // }
    const char systemPrefix[] = "system:";
    jboolean hasNext = (*env)->CallBooleanMethod(env, loc[aliases], g_EnumerationHasMoreElements);
    while (hasNext)
    {
        jstring alias = (*env)->CallObjectMethod(env, loc[aliases], g_EnumerationNextElement);
        ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);

        // Filter to only system certificates if necessary
        // System certificates are included for 'current user' (matches Windows)
        const char* aliasPtr = (*env)->GetStringUTFChars(env, alias, NULL);
        if (!systemOnly || strncmp(aliasPtr, systemPrefix, sizeof(systemPrefix) / sizeof(char)) == 0)
        {
            jobject cert = (*env)->CallObjectMethod(env, loc[store], g_KeyStoreGetCertificate, alias);
            if (cert != NULL && !CheckJNIExceptions(env))
            {
                cert = ToGRef(env, cert);
                cb(cert, context);
            }
        }

        (*env)->ReleaseStringUTFChars(env, alias, aliasPtr);
        hasNext = (*env)->CallBooleanMethod(env, loc[aliases], g_EnumerationHasMoreElements);

loop_cleanup:
        (*env)->DeleteLocalRef(env, alias);
    }

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}
