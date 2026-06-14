// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_jni.h"
#include "pal_types.h"

typedef enum
{
    ANDROID_PROXY_TYPE_DIRECT = 0,
    ANDROID_PROXY_TYPE_HTTP   = 1,
    ANDROID_PROXY_TYPE_SOCKS  = 2,
} AndroidProxyType;

typedef struct
{
    int32_t   type;  // AndroidProxyType
    uint16_t* host;  // NUL-terminated UTF-16, allocated via xmalloc (so .NET internals
                     // can read it directly with Marshal.PtrToStringUni / new string(char*));
                     // freed via AndroidCryptoNative_FreeProxyResult.
    int32_t   port;
} AndroidProxyInfo;

// Resolves the system proxy chain for the destination URL by querying
// java.net.ProxySelector.getDefault().select(URI.create(url)).
//
// On success returns 0. *outCount is the number of DIRECT/HTTP/SOCKS
// entries. *outProxies is an array of AndroidProxyInfo allocated via
// malloc; the caller must release it via AndroidCryptoNative_FreeProxyResult.
// DIRECT entries represent Java's Proxy.NO_PROXY / Proxy.Type.DIRECT and have
// NULL host and port 0.
//
// JNI exceptions while constructing the URI or resolving the proxy list are
// treated as "no proxy" and the function returns success with outCount == 0.
// JNI exceptions while reading an individual proxy entry are cleared and that
// entry is skipped; previously read entries may still be returned.
//
// On result-array allocation failure returns -1. Host-string allocation uses
// xmalloc via AllocateString and aborts on allocation failure.
PALEXPORT int32_t AndroidCryptoNative_GetProxyForUrl(const char* urlUtf8,
                                                     int32_t*    outCount,
                                                     AndroidProxyInfo** outProxies);

PALEXPORT void AndroidCryptoNative_FreeProxyResult(AndroidProxyInfo* proxies, int32_t count);
