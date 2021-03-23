// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ssl.h"

int32_t CryptoNative_OpenSslGetProtocolSupport(SslProtocols protocol)
{
    JNIEnv* env = GetJNIEnv();
    jobject sslCtxObj = (*env)->CallStaticObjectMethod(env, g_sslCtxClass, g_sslCtxGetDefaultMethod);
    jobject sslParametersObj = (*env)->CallObjectMethod(env, sslCtxObj, g_sslCtxGetDefaultSslParamsMethod);
    jobjectArray protocols = (jobjectArray)(*env)->CallObjectMethod(env, sslParametersObj, g_sslParamsGetProtocolsMethod);

    int protocolsCount = (*env)->GetArrayLength(env, protocols);
    int supported = 0;
    for (int i = 0; i < protocolsCount; i++)
    {
        jstring protocolStr = (jstring) ((*env)->GetObjectArrayElement(env, protocols, i));
        const char* protocolStrPtr = (*env)->GetStringUTFChars(env, protocolStr, NULL);
        if ((!strcmp(protocolStrPtr, "TLSv1")   && protocol == PAL_SSL_TLS)   ||
            (!strcmp(protocolStrPtr, "TLSv1.1") && protocol == PAL_SSL_TLS11) ||
            (!strcmp(protocolStrPtr, "TLSv1.2") && protocol == PAL_SSL_TLS12) ||
            (!strcmp(protocolStrPtr, "TLSv1.3") && protocol == PAL_SSL_TLS13))
        {
            supported = 1;
            (*env)->ReleaseStringUTFChars(env, protocolStr, protocolStrPtr);
            (*env)->DeleteLocalRef(env, protocolStr);
            break;
        }
        (*env)->ReleaseStringUTFChars(env, protocolStr, protocolStrPtr);
        (*env)->DeleteLocalRef(env, protocolStr);
    }
    (*env)->DeleteLocalRef(env, sslCtxObj);
    (*env)->DeleteLocalRef(env, sslParametersObj);
    (*env)->DeleteLocalRef(env, protocols);
    return supported;
}
