// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ssl.h"

PAL_SslProtocol AndroidCryptoNative_SSLGetSupportedProtocols(void)
{
    JNIEnv* env = GetJNIEnv();
    PAL_SslProtocol supported = 0;
    INIT_LOCALS(loc, context, params, protocols);

    // SSLContext context = SSLContext.getDefault();
    // SSLParameters params = context.getDefaultSSLParameters();
    // String[] protocols = params.getProtocols();
    loc[context] = (*env)->CallStaticObjectMethod(env, g_sslCtxClass, g_sslCtxGetDefaultMethod);
    loc[params] = (*env)->CallObjectMethod(env, loc[context], g_sslCtxGetDefaultSslParamsMethod);
    loc[protocols] = (*env)->CallObjectMethod(env, loc[params], g_SSLParametersGetProtocols);

    const char tlsv1[] = "TLSv1";
    size_t tlsv1Len = (sizeof(tlsv1) / sizeof(*tlsv1)) - 1;

    jsize count = (*env)->GetArrayLength(env, loc[protocols]);
    for (int32_t i = 0; i < count; i++)
    {
        jstring protocol = (*env)->GetObjectArrayElement(env, loc[protocols], i);
        const char* protocolStr = (*env)->GetStringUTFChars(env, protocol, NULL);
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
        (*env)->DeleteLocalRef(env, protocol);
    }

    RELEASE_LOCALS(loc, env);
    return supported;
}

bool AndroidCryptoNative_SSLSupportsApplicationProtocolsConfiguration(void)
{
    return g_SSLParametersSetApplicationProtocols != NULL;
}
