// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once
#include "pal_types.h"

/**
* Constants from curl.h for supported features
*/
enum CurlFeatures : int32_t
{
    PAL_CURL_VERSION_IPV6 =         (1<<0),
    PAL_CURL_VERSION_KERBEROS4 =    (1<<1),
    PAL_CURL_VERSION_SSL =          (1<<2),
    PAL_CURL_VERSION_LIBZ =         (1<<3),
    PAL_CURL_VERSION_NTLM =         (1<<4),
    PAL_CURL_VERSION_GSSNEGOTIATE = (1<<5),
    PAL_CURL_VERSION_DEBUG =        (1<<6),
    PAL_CURL_VERSION_ASYNCHDNS =    (1<<7),
    PAL_CURL_VERSION_SPNEGO =       (1<<8),
    PAL_CURL_VERSION_LARGEFILE =    (1<<9),
    PAL_CURL_VERSION_IDN =          (1<<10),
    PAL_CURL_VERSION_SSPI =         (1<<11),
    PAL_CURL_VERSION_CONV =         (1<<12),
    PAL_CURL_VERSION_CURLDEBUG =    (1<<13),
    PAL_CURL_VERSION_TLSAUTH_SRP =  (1<<14),
    PAL_CURL_VERSION_NTLM_WB =      (1<<15),
    PAL_CURL_VERSION_HTTP2 =        (1<<16),
    PAL_CURL_VERSION_GSSAPI =       (1<<17),
    PAL_CURL_VERSION_KERBEROS5 =    (1<<18),
    PAL_CURL_VERSION_UNIX_SOCKETS = (1<<19),
    PAL_CURL_VERSION_PSL =          (1<<20),
};

/*
Gets the features supported by libcurl.

Returns 1 if multiplexing is supported, otherwise 0.
*/
extern "C" int32_t HttpNative_GetSupportedFeatures();

/*
Gets the features supported by libcurl.

Returns 1 if multiplexing is supported, otherwise 0.
*/
extern "C" int32_t HttpNative_GetSupportsHttp2Multiplexing();

/*
Gets a string description of the version in use.
*/
extern "C" char* HttpNative_GetVersionDescription();

/*
Gets a string description of the SSL version in use.
*/
extern "C" char* HttpNative_GetSslVersionDescription();
