// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        // Class for constants defined for the enum CURLoption in curl.h
        internal static partial class CURLoption
        {
            // Curl options are of the format <type base> + <n>
            private const int CurlOptionLongBase = 0;
            private const int CurlOptionObjectPointBase = 10000;

            internal const int CURLOPT_URL = CurlOptionObjectPointBase + 2;
            internal const int CURLOPT_FOLLOWLOCATION = CurlOptionLongBase + 52;
            internal const int CURLOPT_PROXYPORT = CurlOptionLongBase + 59;
            internal const int CURLOPT_PROXYTYPE = CurlOptionLongBase + 101;

            internal const int CURLOPT_WRITEDATA = CurlOptionObjectPointBase + 1;
            internal const int CURLOPT_PROXY = CurlOptionObjectPointBase + 4;
            internal const int CURLOPT_PROXYUSERPWD = CurlOptionObjectPointBase + 6;
            internal const int CURLOPT_ACCEPTENCODING = CurlOptionObjectPointBase + 102;
        }

        // Class for constants defined for the enum CURLINFO in curl.h
        internal static partial class CURLINFO
        {
            // Curl info are of the format <type base> + <n>
            private const int CurlInfoLongBase = 0x200000;
            internal const int CURLINFO_RESPONSE_CODE = CurlInfoLongBase + 2;
        }

        // Class for constants defined for the enum curl_proxytype in curl.h
        internal static partial class curl_proxytype
        {
            internal const int CURLPROXY_HTTP = 0;
        }

        // Class for constants defined for the enum CURLcode in curl.h
        internal static partial class CURLcode
        {
            internal const int CURLE_OK = 0;
        }
    }
}
