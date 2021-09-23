// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http.Headers
{
    internal static class QPackStaticTable
    {
        // https://tools.ietf.org/html/draft-ietf-quic-qpack-11#appendix-A
        // TODO: can we put some of this logic into H3StaticTable and/or generate it using data that is already there?
        internal static (HeaderDescriptor descriptor, string value)[] HeaderLookup { get; } = new (HeaderDescriptor descriptor, string value)[]
        {
            (new HeaderDescriptor(":authority"), ""), // 0
            (new HeaderDescriptor(":path"), "/"), // 1
            (new HeaderDescriptor(KnownHeaders.Age), "0"), // 2
            (new HeaderDescriptor(KnownHeaders.ContentDisposition), ""), // 3
            (new HeaderDescriptor(KnownHeaders.ContentLength), "0"), // 4
            (new HeaderDescriptor(KnownHeaders.Cookie), ""), // 5
            (new HeaderDescriptor(KnownHeaders.Date), ""), // 6
            (new HeaderDescriptor(KnownHeaders.ETag), ""), // 7
            (new HeaderDescriptor(KnownHeaders.IfModifiedSince), ""), // 8
            (new HeaderDescriptor(KnownHeaders.IfNoneMatch), ""), // 9
            (new HeaderDescriptor(KnownHeaders.LastModified), ""), // 10
            (new HeaderDescriptor(KnownHeaders.Link), ""), // 11
            (new HeaderDescriptor(KnownHeaders.Location), ""), // 12
            (new HeaderDescriptor(KnownHeaders.Referer), ""), // 13
            (new HeaderDescriptor(KnownHeaders.SetCookie), ""), // 14
            (new HeaderDescriptor(":method"), "CONNECT"), // 15
            (new HeaderDescriptor(":method"), "DELETE"), // 16
            (new HeaderDescriptor(":method"), "GET"), // 17
            (new HeaderDescriptor(":method"), "HEAD"), // 18
            (new HeaderDescriptor(":method"), "OPTIONS"), // 19
            (new HeaderDescriptor(":method"), "POST"), // 20
            (new HeaderDescriptor(":method"), "PUT"), // 21
            (new HeaderDescriptor(":scheme"), "http"), // 22
            (new HeaderDescriptor(":scheme"), "https"), // 23
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "103"), // 24
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "200"), // 25
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "304"), // 26
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "404"), // 27
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "503"), // 28
            (new HeaderDescriptor(KnownHeaders.Accept), "*/*"), // 29
            (new HeaderDescriptor(KnownHeaders.Accept), "application/dns-message"), // 30
            (new HeaderDescriptor(KnownHeaders.AcceptEncoding), "gzip, deflate, br"), // 31
            (new HeaderDescriptor(KnownHeaders.AcceptRanges), "bytes"), // 32
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowHeaders), "cache-control"), // 33
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowHeaders), "content-type"), // 34
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowOrigin), "*"), // 35
            (new HeaderDescriptor(KnownHeaders.CacheControl), "max-age=0"), // 36
            (new HeaderDescriptor(KnownHeaders.CacheControl), "max-age=2592000"), // 37
            (new HeaderDescriptor(KnownHeaders.CacheControl), "max-age=604800"), // 38
            (new HeaderDescriptor(KnownHeaders.CacheControl), "no-cache"), // 39
            (new HeaderDescriptor(KnownHeaders.CacheControl), "no-store"), // 40
            (new HeaderDescriptor(KnownHeaders.CacheControl), "public, max-age=31536000"), // 41
            (new HeaderDescriptor(KnownHeaders.ContentEncoding), "br"), // 42
            (new HeaderDescriptor(KnownHeaders.ContentEncoding), "gzip"), // 43
            (new HeaderDescriptor(KnownHeaders.ContentType), "application/dns-message"), // 44
            (new HeaderDescriptor(KnownHeaders.ContentType), "application/javascript"), // 45
            (new HeaderDescriptor(KnownHeaders.ContentType), "application/json"), // 46
            (new HeaderDescriptor(KnownHeaders.ContentType), "application/x-www-form-urlencoded"), // 47
            (new HeaderDescriptor(KnownHeaders.ContentType), "image/gif"), // 48
            (new HeaderDescriptor(KnownHeaders.ContentType), "image/jpeg"), // 49
            (new HeaderDescriptor(KnownHeaders.ContentType), "image/png"), // 50
            (new HeaderDescriptor(KnownHeaders.ContentType), "text/css"), // 51
            (new HeaderDescriptor(KnownHeaders.ContentType), "text/html; charset=utf-8"), // 52; Whitespace is correct, see spec.
            (new HeaderDescriptor(KnownHeaders.ContentType), "text/plain"), // 53
            (new HeaderDescriptor(KnownHeaders.ContentType), "text/plain;charset=utf-8"), // 54; Whitespace is correct, see spec.
            (new HeaderDescriptor(KnownHeaders.Range), "bytes=0-"), // 55
            (new HeaderDescriptor(KnownHeaders.StrictTransportSecurity), "max-age=31536000"), // 56
            (new HeaderDescriptor(KnownHeaders.StrictTransportSecurity), "max-age=31536000; includesubdomains"), // 57
            (new HeaderDescriptor(KnownHeaders.StrictTransportSecurity), "max-age=31536000; includesubdomains; preload"), // 58
            (new HeaderDescriptor(KnownHeaders.Vary), "accept-encoding"), // 59
            (new HeaderDescriptor(KnownHeaders.Vary), "origin"), // 60
            (new HeaderDescriptor(KnownHeaders.XContentTypeOptions), "nosniff"), // 61
            (new HeaderDescriptor("x-xss-protection"), "1; mode=block"), // 62
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "100"), // 63
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "204"), // 64
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "206"), // 65
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "302"), // 66
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "400"), // 67
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "403"), // 68
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "421"), // 69
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "425"), // 70
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "500"), // 71
            (new HeaderDescriptor(KnownHeaders.AcceptLanguage), ""), // 72
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowCredentials), "FALSE"), // 73
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowCredentials), "TRUE"), // 74
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowHeaders), "*"), // 75
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowMethods), "get"), // 76
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowMethods), "get, post, options"), // 77
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowMethods), "options"), // 78
            (new HeaderDescriptor(KnownHeaders.AccessControlExposeHeaders), "content-length"), // 79
            (new HeaderDescriptor("access-control-request-headers"), "content-type"), // 80
            (new HeaderDescriptor("access-control-request-method"), "get"), // 81
            (new HeaderDescriptor("access-control-request-method"), "post"), // 82
            (new HeaderDescriptor(KnownHeaders.AltSvc), "clear"), // 83
            (new HeaderDescriptor(KnownHeaders.Authorization), ""), // 84
            (new HeaderDescriptor(KnownHeaders.ContentSecurityPolicy), "script-src 'none'; object-src 'none'; base-uri 'none'"), // 85
            (new HeaderDescriptor("early-data"), "1"), // 86
            (new HeaderDescriptor("expect-ct"), ""), // 87
            (new HeaderDescriptor("forwarded"), ""), // 88
            (new HeaderDescriptor(KnownHeaders.IfRange), ""), // 89
            (new HeaderDescriptor(KnownHeaders.Origin), ""), // 90
            (new HeaderDescriptor("purpose"), "prefetch"), // 91
            (new HeaderDescriptor(KnownHeaders.Server), ""), // 92
            (new HeaderDescriptor("timing-allow-origin"), "*"), // 93
            (new HeaderDescriptor(KnownHeaders.UpgradeInsecureRequests), "1"), // 94
            (new HeaderDescriptor(KnownHeaders.UserAgent), ""), // 95
            (new HeaderDescriptor("x-forwarded-for"), ""), // 96
            (new HeaderDescriptor(KnownHeaders.XFrameOptions), "deny"), // 97
            (new HeaderDescriptor(KnownHeaders.XFrameOptions), "sameorigin") // 98
        };
    }
}
