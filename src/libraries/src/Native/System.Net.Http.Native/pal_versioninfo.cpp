// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_versioninfo.h"

#include <curl/curl.h>

extern "C" int32_t GetCurlVersionInfo(
    int32_t* age, 
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
