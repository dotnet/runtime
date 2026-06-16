// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ssl.h"

PAL_SslProtocol AndroidCryptoNative_SSLGetSupportedProtocols(void)
{
    JNIEnv* env = GetJNIEnv();
    PAL_SslProtocol supported = 0;
    INIT_LOCALS(loc, context, params, protocols);
    jstring protocol = NULL;
    const char* protocolStr = NULL;

    // SSLContext context = SSLContext.getDefault();
    // SSLParameters params = context.getDefaultSSLParameters();
    // String[] protocols = params.getProtocols();
    loc[context] = (*env)->CallStaticObjectMethod(env, g_sslCtxClass, g_sslCtxGetDefaultMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[params] = (*env)->CallObjectMethod(env, loc[context], g_sslCtxGetDefaultSslParamsMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[protocols] = (*env)->CallObjectMethod(env, loc[params], g_SSLParametersGetProtocols);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    const char tlsv1[] = "TLSv1";
    size_t tlsv1Len = (sizeof(tlsv1) / sizeof(*tlsv1)) - 1;

    jsize count = (*env)->GetArrayLength(env, loc[protocols]);
    ON_EXCEPTION_PRINT_AND_GOTO(error);
    for (int32_t i = 0; i < count; i++)
    {
        protocol = (*env)->GetObjectArrayElement(env, loc[protocols], i);
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        if (protocol == NULL)
            goto error;

        protocolStr = (*env)->GetStringUTFChars(env, protocol, NULL);
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        if (protocolStr == NULL)
            goto error;

        if (strncmp(protocolStr, tlsv1, tlsv1Len) == 0)
        {
            if (strlen(protocolStr) == tlsv1Len)
            {
                supported |= PAL_SslProtocol_Tls10;
            }
            else if (strcmp(protocolStr + tlsv1Len, ".1") == 0)
            {
                supported |= PAL_SslProtocol_Tls11;
            }
            else if (strcmp(protocolStr + tlsv1Len, ".2") == 0)
            {
                supported |= PAL_SslProtocol_Tls12;
            }
            else if (strcmp(protocolStr + tlsv1Len, ".3") == 0)
            {
                supported |= PAL_SslProtocol_Tls13;
            }
        }

        (*env)->ReleaseStringUTFChars(env, protocol, protocolStr);
        protocolStr = NULL;
        ReleaseLRef(env, protocol);
        protocol = NULL;
    }

    goto cleanup;

error:
    supported = 0;

cleanup:
    if (protocolStr != NULL)
        (*env)->ReleaseStringUTFChars(env, protocol, protocolStr);

    ReleaseLRef(env, protocol);
    RELEASE_LOCALS(loc, env);
    return supported;
}

bool AndroidCryptoNative_SSLSupportsApplicationProtocolsConfiguration(void)
{
    return g_SSLParametersSetApplicationProtocols != NULL;
}
