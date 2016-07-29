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

extern "C" int32_t HttpNative_GetSupportedFeatures()
{
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != nullptr ? info->features : 0;
}

extern "C" int32_t HttpNative_GetSupportsHttp2Multiplexing()
{
#if HAVE_CURLPIPE_MULTIPLEX
    curl_version_info_data* info = curl_version_info(CURLVERSION_NOW);
    return info != nullptr && (info->features & CURL_VERSION_HTTP2) == CURL_VERSION_HTTP2 ? 1 : 0;
#else
    return 0;
#endif
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

// TODO: Remove HttpNative_GetCurlVersionInfo once the build containing the above functions
//       is available as part of a package.
extern "C" int32_t HttpNative_GetCurlVersionInfo(int32_t* age,
                                                 int32_t* supportsSsl,
                                                 int32_t* supportsAutoDecompression,
                                                 int32_t* supportsHttp2Multiplexing)
{
    curl_version_info_data* versionInfo = curl_version_info(CURLVERSION_NOW);

    if (!versionInfo || !age || !supportsSsl || !supportsAutoDecompression || !supportsHttp2Multiplexing)
    {
        if (age)
            *age = 0;
        if (supportsSsl)
            *supportsSsl = 0;
        if (supportsAutoDecompression)
            *supportsAutoDecompression = 0;
        if (supportsHttp2Multiplexing)
            *supportsHttp2Multiplexing = 0;

        return 0;
    }

    *age = versionInfo->age;
    *supportsSsl = (versionInfo->features & CURL_VERSION_SSL) == CURL_VERSION_SSL;
    *supportsAutoDecompression = (versionInfo->features & CURL_VERSION_LIBZ) == CURL_VERSION_LIBZ;
    *supportsHttp2Multiplexing =
#if HAVE_CURLPIPE_MULTIPLEX
        (versionInfo->features & CURL_VERSION_HTTP2) == CURL_VERSION_HTTP2;
#else
        0;
#endif

    return 1;
}
