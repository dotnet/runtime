// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    public enum HttpRequestHeader
    {
        CacheControl = 0,    // general-header [section 4.5]
        Connection = 1,    // general-header [section 4.5]
        Date = 2,    // general-header [section 4.5]
        KeepAlive = 3,    // general-header [not in RFC]
        Pragma = 4,    // general-header [section 4.5]
        Trailer = 5,    // general-header [section 4.5]
        TransferEncoding = 6,    // general-header [section 4.5]
        Upgrade = 7,    // general-header [section 4.5]
        Via = 8,    // general-header [section 4.5]
        Warning = 9,    // general-header [section 4.5]
        Allow = 10,   // entity-header  [section 7.1]
        ContentLength = 11,   // entity-header  [section 7.1]
        ContentType = 12,   // entity-header  [section 7.1]
        ContentEncoding = 13,   // entity-header  [section 7.1]
        ContentLanguage = 14,   // entity-header  [section 7.1]
        ContentLocation = 15,   // entity-header  [section 7.1]
        ContentMd5 = 16,   // entity-header  [section 7.1]
        ContentRange = 17,   // entity-header  [section 7.1]
        Expires = 18,   // entity-header  [section 7.1]
        LastModified = 19,   // entity-header  [section 7.1]

        Accept = 20,   // request-header [section 5.3]
        AcceptCharset = 21,   // request-header [section 5.3]
        AcceptEncoding = 22,   // request-header [section 5.3]
        AcceptLanguage = 23,   // request-header [section 5.3]
        Authorization = 24,   // request-header [section 5.3]
        Cookie = 25,   // request-header [not in RFC]
        Expect = 26,   // request-header [section 5.3]
        From = 27,   // request-header [section 5.3]
        Host = 28,   // request-header [section 5.3]
        IfMatch = 29,   // request-header [section 5.3]
        IfModifiedSince = 30,   // request-header [section 5.3]
        IfNoneMatch = 31,   // request-header [section 5.3]
        IfRange = 32,   // request-header [section 5.3]
        IfUnmodifiedSince = 33,   // request-header [section 5.3]
        MaxForwards = 34,   // request-header [section 5.3]
        ProxyAuthorization = 35,   // request-header [section 5.3]
        Referer = 36,   // request-header [section 5.3]
        Range = 37,   // request-header [section 5.3]
        Te = 38,   // request-header [section 5.3]
        Translate = 39,   // request-header [webDAV, not in RFC 2518]
        UserAgent = 40,   // request-header [section 5.3]
    }

    internal static class HttpRequestHeaderExtensions
    {
        private static readonly string[] s_names = {
            "Cache-Control",
            "Connection",
            "Date",
            "Keep-Alive",
            "Pragma",
            "Trailer",
            "Transfer-Encoding",
            "Upgrade",
            "Via",
            "Warning",
            "Allow",
            "Content-Length",
            "Content-Type",
            "Content-Encoding",
            "Content-Language",
            "Content-Location",
            "Content-MD5",
            "Content-Range",
            "Expires",
            "Last-Modified",
            "Accept",
            "Accept-Charset",
            "Accept-Encoding",
            "Accept-Language",
            "Authorization",
            "Cookie",
            "Expect",
            "From",
            "Host",
            "If-Match",
            "If-Modified-Since",
            "If-None-Match",
            "If-Range",
            "If-Unmodified-Since",
            "Max-Forwards",
            "Proxy-Authorization",
            "Referer",
            "Range",
            "Te",
            "Translate",
            "User-Agent",
        };

        public static string GetName(this HttpRequestHeader header)
        {
            return s_names[(int)header];
        }
    }
}
