// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_versioninfo.h"

#include <string.h>
#include <curl/curl.h>

c_static_assert(PAL_CURL_VERSION_IPV6 == CURL_VERSION_IPV6);
c_static_assert(PAL_CURL_VERSION_KERBEROS4 == CURL_VERSION_KERBEROS4);
c_static_assert(PAL_CURL_VERSION_SSL == CURL_VERSION_SSL);
c_static_assert(PAL_CURL_VERSION_LIBZ == CURL_VERSION_LIBZ);
c_static_assert(PAL_CURL_VERSION_NTLM == CURL_VERSION_NTLM);
c_static_assert(PAL_CURL_VERSION_GSSNEGOTIATE == CURL_VERSION_GSSNEGOTIATE);
c_static_assert(PAL_CURL_VERSION_DEBUG == CURL_VERSION_DEBUG);
c_static_assert(PAL_CURL_VERSION_ASYNCHDNS == CURL_VERSION_ASYNCHDNS);
c_static_assert(PAL_CURL_VERSION_SPNEGO == CURL_VERSION_SPNEGO);
c_static_assert(PAL_CURL_VERSION_LARGEFILE == CURL_VERSION_LARGEFILE);
c_static_assert(PAL_CURL_VERSION_IDN == CURL_VERSION_IDN);
c_static_assert(PAL_CURL_VERSION_SSPI == CURL_VERSION_SSPI);
c_static_assert(PAL_CURL_VERSION_CONV == CURL_VERSION_CONV);
c_static_assert(PAL_CURL_VERSION_CURLDEBUG == CURL_VERSION_CURLDEBUG);
c_static_assert(PAL_CURL_VERSION_TLSAUTH_SRP == CURL_VERSION_TLSAUTH_SRP);
c_static_assert(PAL_CURL_VERSION_NTLM_WB == CURL_VERSION_NTLM_WB);
#ifdef CURL_VERSION_HTTP2
c_static_assert(PAL_CURL_VERSION_HTTP2 == CURL_VERSION_HTTP2);
#endif
#ifdef CURL_VERSION_GSSAPI
c_static_assert(PAL_CURL_VERSION_GSSAPI == CURL_VERSION_GSSAPI);
#endif
#ifdef CURL_VERSION_KERBEROS5
c_static_assert(PAL_CURL_VERSION_KERBEROS5 == CURL_VERSION_KERBEROS5);
#endif
#ifdef CURL_VERSION_UNIX_SOCKETS
c_static_assert(PAL_CURL_VERSION_UNIX_SOCKETS == CURL_VERSION_UNIX_SOCKETS);
#endif
#ifdef CURL_VERSION_PSL
c_static_assert(PAL_CURL_VERSION_PSL == CURL_VERSION_PSL);
#endif

// Based on docs/libcurl/symbols-in-versions in libcurl source tree,
// the CURL_VERSION_HTTP2 was introduced in libcurl 7.33.0 and
// the CURLPIPE_MULTIPLEX was introduced in libcurl 7.43.0.
#define MIN_VERSION_WITH_CURLPIPE_MULTIPLEX 0x072B00

int32_t HttpNative_GetSupportedFeatures()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != NULL ? info->features : 0;
}

int32_t HttpNative_GetSupportsHttp2Multiplexing()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != NULL &&
        (info->version_num >= MIN_VERSION_WITH_CURLPIPE_MULTIPLEX) && 
        ((info->features & PAL_CURL_VERSION_HTTP2) == PAL_CURL_VERSION_HTTP2) ? 1 : 0;
}

char* HttpNative_GetVersionDescription()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != NULL && info->version != NULL ? strdup(info->version) : NULL;
}

char* HttpNative_GetSslVersionDescription()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != NULL && info->ssl_version != NULL ? strdup(info->ssl_version) : NULL;
}
