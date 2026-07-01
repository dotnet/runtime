// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_proxy.h"

#include <stdlib.h>

PALEXPORT int32_t AndroidCryptoNative_GetProxyForUrl(const char* urlUtf8,
                                                     int32_t*    outCount,
                                                     AndroidProxyInfo** outProxies)
{
    abort_if_invalid_pointer_argument(outCount);
    abort_if_invalid_pointer_argument(outProxies);

    *outCount = 0;
    *outProxies = NULL;

    if (urlUtf8 == NULL)
        return 0; // treat as "no proxy"

    JNIEnv* env = GetJNIEnv();
    int32_t ret = 0;
    AndroidProxyInfo* result = NULL;
    int32_t written = 0;

    // All transient outer-scope local refs go here. RELEASE_LOCALS(loc, env) at the
    // cleanup label releases them exactly once on every path.
    INIT_LOCALS(loc, jurl, juri, jselector, jlist,
                     jproxyTypeDirect, jproxyTypeHttp, jproxyTypeSocks);

    loc[jurl] = make_java_string(env, urlUtf8); // aborts on OOM

    // URI.create(url) — IllegalArgumentException for malformed input.
    loc[juri] = (*env)->CallStaticObjectMethod(env, g_URI, g_URI_create, loc[jurl]);
    if (TryClearJNIExceptions(env))
        goto cleanup;

    // ProxySelector.getDefault() — VM-wide singleton; may throw SecurityException.
    loc[jselector] = (*env)->CallStaticObjectMethod(env, g_ProxySelector, g_ProxySelector_getDefault);
    if (TryClearJNIExceptions(env))
        goto cleanup;
    if (loc[jselector] == NULL)
        goto cleanup;

    // ProxySelector.select(uri)
    loc[jlist] = (*env)->CallObjectMethod(env, loc[jselector], g_ProxySelector_select, loc[juri]);
    if (TryClearJNIExceptions(env))
        goto cleanup;
    if (loc[jlist] == NULL)
        goto cleanup;

    // Resolve the Proxy.Type enum constants for IsSameObject comparisons.
    loc[jproxyTypeDirect] = (*env)->GetStaticObjectField(env, g_ProxyType, g_ProxyType_DIRECT);
    loc[jproxyTypeHttp]   = (*env)->GetStaticObjectField(env, g_ProxyType, g_ProxyType_HTTP);
    loc[jproxyTypeSocks]  = (*env)->GetStaticObjectField(env, g_ProxyType, g_ProxyType_SOCKS);
    if (TryClearJNIExceptions(env))
        goto cleanup;

    jint n = (*env)->CallIntMethod(env, loc[jlist], g_CollectionSize);
    if (TryClearJNIExceptions(env))
        goto cleanup;
    if (n <= 0)
        goto cleanup;

    result = (AndroidProxyInfo*)calloc((size_t)n, sizeof(AndroidProxyInfo));
    if (result == NULL)
    {
        ret = -1;
        goto cleanup;
    }

    for (jint i = 0; i < n; i++)
    {
        // Per-iteration locals — released at the end of each loop body.
        INIT_LOCALS(iter, jproxy, jtype, jaddr, jhost);

        iter[jproxy] = (*env)->CallObjectMethod(env, loc[jlist], g_ListGet, i);
        if (TryClearJNIExceptions(env) || iter[jproxy] == NULL)
        {
            RELEASE_LOCALS(iter, env);
            continue;
        }

        iter[jtype] = (*env)->CallObjectMethod(env, iter[jproxy], g_ProxyType_method);
        if (TryClearJNIExceptions(env) || iter[jtype] == NULL)
        {
            RELEASE_LOCALS(iter, env);
            continue;
        }

        int32_t type;
        if ((*env)->IsSameObject(env, iter[jtype], loc[jproxyTypeDirect]))
        {
            result[written].type = ANDROID_PROXY_TYPE_DIRECT;
            result[written].host = NULL;
            result[written].port = 0;
            written++;

            RELEASE_LOCALS(iter, env);
            continue;
        }
        else if ((*env)->IsSameObject(env, iter[jtype], loc[jproxyTypeHttp]))
        {
            type = ANDROID_PROXY_TYPE_HTTP;
        }
        else if ((*env)->IsSameObject(env, iter[jtype], loc[jproxyTypeSocks]))
        {
            // SOCKS is a transport-level proxy protocol (RFC 1928 for SOCKS5). Unlike
            // HTTP CONNECT, it tunnels arbitrary TCP at the socket layer rather than
            // negotiating at the HTTP layer. Android's java.net.Proxy.Type.SOCKS maps
            // to SOCKS5 in modern Java; SocketsHttpHandler accepts the "socks5://"
            // scheme via HttpUtilities.IsSupportedProxyScheme.
            type = ANDROID_PROXY_TYPE_SOCKS;
        }
        else
        {
            // Unknown proxy type: no result entry.
            RELEASE_LOCALS(iter, env);
            continue;
        }

        iter[jaddr] = (*env)->CallObjectMethod(env, iter[jproxy], g_Proxy_address);
        if (TryClearJNIExceptions(env)
            || iter[jaddr] == NULL
            || !(*env)->IsInstanceOf(env, iter[jaddr], g_InetSocketAddress))
        {
            RELEASE_LOCALS(iter, env);
            continue;
        }

        iter[jhost] = (*env)->CallObjectMethod(env, iter[jaddr], g_InetSocketAddress_getHostString);
        if (TryClearJNIExceptions(env) || iter[jhost] == NULL)
        {
            RELEASE_LOCALS(iter, env);
            continue;
        }

        jint port = (*env)->CallIntMethod(env, iter[jaddr], g_InetSocketAddress_getPort);
        if (TryClearJNIExceptions(env))
        {
            RELEASE_LOCALS(iter, env);
            continue;
        }

        // Copy the host as NUL-terminated UTF-16. Marshal.PtrToStringUni on the managed
        // side is a zero-conversion copy because System.String is internally UTF-16.
        // AllocateString aborts on allocation failure; we never see a NULL return here.
        uint16_t* host = AllocateString(env, (jstring)iter[jhost]);

        result[written].type = type;
        result[written].host = host; // ownership transferred to the result; lifetime ≥ jhost
        result[written].port = (int32_t)port;
        written++;

        RELEASE_LOCALS(iter, env);
    }

cleanup:
    RELEASE_LOCALS(loc, env);

    if (ret == 0 && written > 0)
    {
        *outCount = written;
        *outProxies = result;
    }
    else if (result != NULL)
    {
        // Either an error path or no proxy entries survived filtering (unknown types,
        // invalid addresses, etc). Free anything we may have partially populated so that
        // callers can rely on outProxies == NULL whenever outCount == 0.
        for (int32_t i = 0; i < written; i++)
            free(result[i].host);
        free(result);
    }

    return ret;
}

PALEXPORT void AndroidCryptoNative_FreeProxyResult(AndroidProxyInfo* proxies, int32_t count)
{
    if (proxies == NULL)
        return;

    for (int32_t i = 0; i < count; i++)
        free(proxies[i].host);

    free(proxies);
}
