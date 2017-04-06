// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_versioninfo.h"

#include <string.h>
#include <curl/curl.h>

static_assert(PAL_CURL_VERSION_IPV6 == CURL_VERSION_IPV6, "");
static_assert(PAL_CURL_VERSION_KERBEROS4 == CURL_VERSION_KERBEROS4, "");
static_assert(PAL_CURL_VERSION_SSL == CURL_VERSION_SSL, "");
static_assert(PAL_CURL_VERSION_LIBZ == CURL_VERSION_LIBZ, "");
static_assert(PAL_CURL_VERSION_NTLM == CURL_VERSION_NTLM, "");
static_assert(PAL_CURL_VERSION_GSSNEGOTIATE == CURL_VERSION_GSSNEGOTIATE, "");
static_assert(PAL_CURL_VERSION_DEBUG == CURL_VERSION_DEBUG, "");
static_assert(PAL_CURL_VERSION_ASYNCHDNS == CURL_VERSION_ASYNCHDNS, "");
static_assert(PAL_CURL_VERSION_SPNEGO == CURL_VERSION_SPNEGO, "");
static_assert(PAL_CURL_VERSION_LARGEFILE == CURL_VERSION_LARGEFILE, "");
static_assert(PAL_CURL_VERSION_IDN == CURL_VERSION_IDN, "");
static_assert(PAL_CURL_VERSION_SSPI == CURL_VERSION_SSPI, "");
static_assert(PAL_CURL_VERSION_CONV == CURL_VERSION_CONV, "");
static_assert(PAL_CURL_VERSION_CURLDEBUG == CURL_VERSION_CURLDEBUG, "");
static_assert(PAL_CURL_VERSION_TLSAUTH_SRP == CURL_VERSION_TLSAUTH_SRP, "");
static_assert(PAL_CURL_VERSION_NTLM_WB == CURL_VERSION_NTLM_WB, "");
#ifdef CURL_VERSION_HTTP2
static_assert(PAL_CURL_VERSION_HTTP2 == CURL_VERSION_HTTP2, "");
#endif
#ifdef CURL_VERSION_GSSAPI
static_assert(PAL_CURL_VERSION_GSSAPI == CURL_VERSION_GSSAPI, "");
#endif
#ifdef CURL_VERSION_KERBEROS5
static_assert(PAL_CURL_VERSION_KERBEROS5 == CURL_VERSION_KERBEROS5, "");
#endif
#ifdef CURL_VERSION_UNIX_SOCKETS
static_assert(PAL_CURL_VERSION_UNIX_SOCKETS == CURL_VERSION_UNIX_SOCKETS, "");
#endif
#ifdef CURL_VERSION_PSL
static_assert(PAL_CURL_VERSION_PSL == CURL_VERSION_PSL, "");
#endif

// Based on docs/libcurl/symbols-in-versions in libcurl source tree,
// the CURL_VERSION_HTTP2 was introduced in libcurl 7.33.0 and
// the CURLPIPE_MULTIPLEX was introduced in libcurl 7.43.0.
#define MIN_VERSION_WITH_CURLPIPE_MULTIPLEX 0x074300

extern "C" int32_t HttpNative_GetSupportedFeatures()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != nullptr ? info->features : 0;
}

extern "C" int32_t HttpNative_GetSupportsHttp2Multiplexing()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != nullptr &&
        (info->version_num >= MIN_VERSION_WITH_CURLPIPE_MULTIPLEX) && 
        ((info->features & PAL_CURL_VERSION_HTTP2) == PAL_CURL_VERSION_HTTP2) ? 1 : 0;
}

extern "C" char* HttpNative_GetVersionDescription()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != nullptr && info->version != nullptr ? strdup(info->version) : nullptr;
}

extern "C" char* HttpNative_GetSslVersionDescription()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != nullptr && info->ssl_version != nullptr ? strdup(info->ssl_version) : nullptr;
}
