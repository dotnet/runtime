// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    internal static class KnownHeaders
    {
        // If you add a new entry here, you need to add it to TryGetKnownHeader below as well.

        public static readonly KnownHeader Accept = new KnownHeader("Accept", HttpHeaderType.Request, MediaTypeHeaderParser.MultipleValuesParser);
        public static readonly KnownHeader AcceptCharset = new KnownHeader("Accept-Charset", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser);
        public static readonly KnownHeader AcceptEncoding = new KnownHeader("Accept-Encoding", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser);
        public static readonly KnownHeader AcceptLanguage = new KnownHeader("Accept-Language", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser);
        public static readonly KnownHeader AcceptPatch = new KnownHeader("Accept-Patch");
        public static readonly KnownHeader AcceptRanges = new KnownHeader("Accept-Ranges", HttpHeaderType.Response, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader AccessControlAllowCredentials = new KnownHeader("Access-Control-Allow-Credentials");
        public static readonly KnownHeader AccessControlAllowHeaders = new KnownHeader("Access-Control-Allow-Headers");
        public static readonly KnownHeader AccessControlAllowMethods = new KnownHeader("Access-Control-Allow-Methods");
        public static readonly KnownHeader AccessControlAllowOrigin = new KnownHeader("Access-Control-Allow-Origin");
        public static readonly KnownHeader AccessControlExposeHeaders = new KnownHeader("Access-Control-Expose-Headers");
        public static readonly KnownHeader AccessControlMaxAge = new KnownHeader("Access-Control-Max-Age");
        public static readonly KnownHeader Age = new KnownHeader("Age", HttpHeaderType.Response, TimeSpanHeaderParser.Parser);
        public static readonly KnownHeader Allow = new KnownHeader("Allow", HttpHeaderType.Content, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader AltSvc = new KnownHeader("Alt-Svc");
        public static readonly KnownHeader Authorization = new KnownHeader("Authorization", HttpHeaderType.Request, GenericHeaderParser.SingleValueAuthenticationParser);
        public static readonly KnownHeader CacheControl = new KnownHeader("Cache-Control", HttpHeaderType.General, CacheControlHeaderParser.Parser);
        public static readonly KnownHeader Connection = new KnownHeader("Connection", HttpHeaderType.General, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader ContentDisposition = new KnownHeader("Content-Disposition", HttpHeaderType.Content, GenericHeaderParser.ContentDispositionParser);
        public static readonly KnownHeader ContentEncoding = new KnownHeader("Content-Encoding", HttpHeaderType.Content, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader ContentLanguage = new KnownHeader("Content-Language", HttpHeaderType.Content, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader ContentLength = new KnownHeader("Content-Length", HttpHeaderType.Content, Int64NumberHeaderParser.Parser);
        public static readonly KnownHeader ContentLocation = new KnownHeader("Content-Location", HttpHeaderType.Content, UriHeaderParser.RelativeOrAbsoluteUriParser);
        public static readonly KnownHeader ContentMD5 = new KnownHeader("Content-MD5", HttpHeaderType.Content, ByteArrayHeaderParser.Parser);
        public static readonly KnownHeader ContentRange = new KnownHeader("Content-Range", HttpHeaderType.Content, GenericHeaderParser.ContentRangeParser);
        public static readonly KnownHeader ContentSecurityPolicy = new KnownHeader("Content-Security-Policy");
        public static readonly KnownHeader ContentType = new KnownHeader("Content-Type", HttpHeaderType.Content, MediaTypeHeaderParser.SingleValueParser);
        public static readonly KnownHeader Cookie = new KnownHeader("Cookie");
        public static readonly KnownHeader Cookie2 = new KnownHeader("Cookie2");
        public static readonly KnownHeader Date = new KnownHeader("Date", HttpHeaderType.General, DateHeaderParser.Parser);
        public static readonly KnownHeader ETag = new KnownHeader("ETag", HttpHeaderType.Response, GenericHeaderParser.SingleValueEntityTagParser);
        public static readonly KnownHeader Expect = new KnownHeader("Expect", HttpHeaderType.Request, GenericHeaderParser.MultipleValueNameValueWithParametersParser);
        public static readonly KnownHeader Expires = new KnownHeader("Expires", HttpHeaderType.Content, DateHeaderParser.Parser);
        public static readonly KnownHeader From = new KnownHeader("From", HttpHeaderType.Request, GenericHeaderParser.MailAddressParser);
        public static readonly KnownHeader Host = new KnownHeader("Host", HttpHeaderType.Request, GenericHeaderParser.HostParser);
        public static readonly KnownHeader IfMatch = new KnownHeader("If-Match", HttpHeaderType.Request, GenericHeaderParser.MultipleValueEntityTagParser);
        public static readonly KnownHeader IfModifiedSince = new KnownHeader("If-Modified-Since", HttpHeaderType.Request, DateHeaderParser.Parser);
        public static readonly KnownHeader IfNoneMatch = new KnownHeader("If-None-Match", HttpHeaderType.Request, GenericHeaderParser.MultipleValueEntityTagParser);
        public static readonly KnownHeader IfRange = new KnownHeader("If-Range", HttpHeaderType.Request, GenericHeaderParser.RangeConditionParser);
        public static readonly KnownHeader IfUnmodifiedSince = new KnownHeader("If-Unmodified-Since", HttpHeaderType.Request, DateHeaderParser.Parser);
        public static readonly KnownHeader KeepAlive = new KnownHeader("Keep-Alive");
        public static readonly KnownHeader LastModified = new KnownHeader("Last-Modified", HttpHeaderType.Content, DateHeaderParser.Parser);
        public static readonly KnownHeader Link = new KnownHeader("Link");
        public static readonly KnownHeader Location = new KnownHeader("Location", HttpHeaderType.Response, UriHeaderParser.RelativeOrAbsoluteUriParser);
        public static readonly KnownHeader MaxForwards = new KnownHeader("Max-Forwards", HttpHeaderType.Request, Int32NumberHeaderParser.Parser);
        public static readonly KnownHeader Origin = new KnownHeader("Origin");
        public static readonly KnownHeader P3P = new KnownHeader("P3P");
        public static readonly KnownHeader Pragma = new KnownHeader("Pragma", HttpHeaderType.General, GenericHeaderParser.MultipleValueNameValueParser);
        public static readonly KnownHeader ProxyAuthenticate = new KnownHeader("Proxy-Authenticate", HttpHeaderType.Response, GenericHeaderParser.MultipleValueAuthenticationParser);
        public static readonly KnownHeader ProxyAuthorization = new KnownHeader("Proxy-Authorization", HttpHeaderType.Request, GenericHeaderParser.SingleValueAuthenticationParser);
        public static readonly KnownHeader ProxyConnection = new KnownHeader("Proxy-Connection");
        public static readonly KnownHeader PublicKeyPins = new KnownHeader("Public-Key-Pins");
        public static readonly KnownHeader Range = new KnownHeader("Range", HttpHeaderType.Request, GenericHeaderParser.RangeParser);
        public static readonly KnownHeader Referer = new KnownHeader("Referer", HttpHeaderType.Request, UriHeaderParser.RelativeOrAbsoluteUriParser); // NB: The spelling-mistake "Referer" for "Referrer" must be matched.
        public static readonly KnownHeader RetryAfter = new KnownHeader("Retry-After", HttpHeaderType.Response, GenericHeaderParser.RetryConditionParser);
        public static readonly KnownHeader SecWebSocketAccept = new KnownHeader("Sec-WebSocket-Accept");
        public static readonly KnownHeader SecWebSocketExtensions = new KnownHeader("Sec-WebSocket-Extensions");
        public static readonly KnownHeader SecWebSocketKey = new KnownHeader("Sec-WebSocket-Key");
        public static readonly KnownHeader SecWebSocketProtocol = new KnownHeader("Sec-WebSocket-Protocol");
        public static readonly KnownHeader SecWebSocketVersion = new KnownHeader("Sec-WebSocket-Version");
        public static readonly KnownHeader Server = new KnownHeader("Server", HttpHeaderType.Response, ProductInfoHeaderParser.MultipleValueParser);
        public static readonly KnownHeader SetCookie = new KnownHeader("Set-Cookie");
        public static readonly KnownHeader SetCookie2 = new KnownHeader("Set-Cookie2");
        public static readonly KnownHeader StrictTransportSecurity = new KnownHeader("Strict-Transport-Security");
        public static readonly KnownHeader TE = new KnownHeader("TE", HttpHeaderType.Request, TransferCodingHeaderParser.MultipleValueWithQualityParser);
        public static readonly KnownHeader TSV = new KnownHeader("TSV");
        public static readonly KnownHeader Trailer = new KnownHeader("Trailer", HttpHeaderType.General, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader TransferEncoding = new KnownHeader("Transfer-Encoding", HttpHeaderType.General, TransferCodingHeaderParser.MultipleValueParser);
        public static readonly KnownHeader Upgrade = new KnownHeader("Upgrade", HttpHeaderType.General, GenericHeaderParser.MultipleValueProductParser);
        public static readonly KnownHeader UpgradeInsecureRequests = new KnownHeader("Upgrade-Insecure-Requests");
        public static readonly KnownHeader UserAgent = new KnownHeader("User-Agent", HttpHeaderType.Request, ProductInfoHeaderParser.MultipleValueParser);
        public static readonly KnownHeader Vary = new KnownHeader("Vary", HttpHeaderType.Response, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader Via = new KnownHeader("Via", HttpHeaderType.General, GenericHeaderParser.MultipleValueViaParser);
        public static readonly KnownHeader WWWAuthenticate = new KnownHeader("WWW-Authenticate", HttpHeaderType.Response, GenericHeaderParser.MultipleValueAuthenticationParser);
        public static readonly KnownHeader Warning = new KnownHeader("Warning", HttpHeaderType.General, GenericHeaderParser.MultipleValueWarningParser);
        public static readonly KnownHeader XAspNetVersion = new KnownHeader("X-AspNet-Version");
        public static readonly KnownHeader XContentDuration = new KnownHeader("X-Content-Duration");
        public static readonly KnownHeader XContentTypeOptions = new KnownHeader("X-Content-Type-Options");
        public static readonly KnownHeader XFrameOptions = new KnownHeader("X-Frame-Options");
        public static readonly KnownHeader XMSEdgeRef = new KnownHeader("X-MSEdge-Ref");
        public static readonly KnownHeader XPoweredBy = new KnownHeader("X-Powered-By");
        public static readonly KnownHeader XRequestID = new KnownHeader("X-Request-ID");
        public static readonly KnownHeader XUACompatible = new KnownHeader("X-UA-Compatible");

        // Helper interface for making GetCandidate generic over strings, utf8, etc
        private interface IHeaderNameAccessor
        {
            int Length { get; }
            char CharAt(int index);
        }

        private struct StringAccessor : IHeaderNameAccessor
        {
            private readonly string _string;

            public StringAccessor(string s)
            {
                _string = s;
            }

            public int Length => _string.Length;
            public char CharAt(int index) => _string[index];
        }

        // Find possible known header match via lookup on length and a distinguishing char for that length.
        // Matching is case-insenstive.
        // NOTE: Because of this, we do not preserve the case of the original header,
        // whether from the wire or from the user explicitly setting a known header using a header name string.
        private static KnownHeader GetCandidate<T>(T key)
            where T : struct, IHeaderNameAccessor     // Enforce struct for performance
        {
            int length = key.Length;
            switch (length)
            {
                case 2:
                    return TE; // TE

                case 3:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'A': return Age; // [A]ge
                        case 'P': return P3P; // [P]3P
                        case 'T': return TSV; // [T]SV
                        case 'V': return Via; // [V]ia
                    }
                    break;

                case 4:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'D': return Date; // [D]ate
                        case 'E': return ETag; // [E]Tag
                        case 'F': return From; // [F]rom
                        case 'H': return Host; // [H]ost
                        case 'L': return Link; // [L]ink
                        case 'V': return Vary; // [V]ary
                    }
                    break;

                case 5:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'A': return Allow; // [A]llow
                        case 'R': return Range; // [R]ange
                    }
                    break;

                case 6:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'A': return Accept; // [A]ccept
                        case 'C': return Cookie; // [C]ookie
                        case 'E': return Expect; // [E]xpect
                        case 'O': return Origin; // [O]rigin
                        case 'P': return Pragma; // [P]ragma
                        case 'S': return Server; // [S]erver
                    }
                    break;

                case 7:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'A': return AltSvc;  // [A]lt-Svc
                        case 'C': return Cookie2; // [C]ookie2
                        case 'E': return Expires; // [E]xpires
                        case 'R': return Referer; // [R]eferer
                        case 'T': return Trailer; // [T]railer
                        case 'U': return Upgrade; // [U]pgrade
                        case 'W': return Warning; // [W]arning
                    }
                    break;

                case 8:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(3)))
                    {
                        case 'M': return IfMatch;  // If-[M]atch
                        case 'R': return IfRange;  // If-[R]ange
                        case 'A': return Location; // Loc[a]tion
                    }
                    break;

                case 10:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'C': return Connection; // [C]onnection
                        case 'K': return KeepAlive;  // [K]eep-Alive
                        case 'S': return SetCookie;  // [S]et-Cookie
                        case 'U': return UserAgent;  // [U]ser-Agent
                    }
                    break;

                case 11:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'C': return ContentMD5; // [C]ontent-MD5
                        case 'R': return RetryAfter; // [R]etry-After
                        case 'S': return SetCookie2; // [S]et-Cookie2
                    }
                    break;

                case 12:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(2)))
                    {
                        case 'C': return AcceptPatch; // Ac[c]ept-Patch
                        case 'N': return ContentType; // Co[n]tent-Type
                        case 'X': return MaxForwards; // Ma[x]-Forwards
                        case 'M': return XMSEdgeRef;  // X-[M]SEdge-Ref
                        case 'P': return XPoweredBy;  // X-[P]owered-By
                        case 'R': return XRequestID;  // X-[R]equest-ID
                    }
                    break;

                case 13:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(6)))
                    {
                        case '-': return AcceptRanges;  // Accept[-]Ranges
                        case 'I': return Authorization; // Author[i]zation
                        case 'C': return CacheControl;  // Cache-[C]ontrol
                        case 'T': return ContentRange;  // Conten[t]-Range
                        case 'E': return IfNoneMatch;   // If-Non[e]-Match
                        case 'O': return LastModified;  // Last-M[o]dified
                    }
                    break;

                case 14:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'A': return AcceptCharset; // [A]ccept-Charset
                        case 'C': return ContentLength; // [C]ontent-Length
                    }
                    break;

                case 15:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(7)))
                    {
                        case '-': return XFrameOptions;  // X-Frame[-]Options
                        case 'M': return XUACompatible;  // X-UA-Co[m]patible
                        case 'E': return AcceptEncoding; // Accept-[E]ncoding
                        case 'K': return PublicKeyPins;  // Public-[K]ey-Pins
                        case 'L': return AcceptLanguage; // Accept-[L]anguage
                    }
                    break;

                case 16:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(11)))
                    {
                        case 'O': return ContentEncoding; // Content-Enc[o]ding
                        case 'G': return ContentLanguage; // Content-Lan[g]uage
                        case 'A': return ContentLocation; // Content-Loc[a]tion
                        case 'C': return ProxyConnection; // Proxy-Conne[c]tion
                        case 'I': return WWWAuthenticate; // WWW-Authent[i]cate
                        case 'R': return XAspNetVersion;  // X-AspNet-Ve[r]sion
                    }
                    break;

                case 17:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'I': return IfModifiedSince;  // [I]f-Modified-Since
                        case 'S': return SecWebSocketKey;  // [S]ec-WebSocket-Key
                        case 'T': return TransferEncoding; // [T]ransfer-Encoding
                    }
                    break;

                case 18:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'P': return ProxyAuthenticate; // [P]roxy-Authenticate
                        case 'X': return XContentDuration;  // [X]-Content-Duration
                    }
                    break;

                case 19:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'C': return ContentDisposition; // [C]ontent-Disposition
                        case 'I': return IfUnmodifiedSince;  // [I]f-Unmodified-Since
                        case 'P': return ProxyAuthorization; // [P]roxy-Authorization
                    }
                    break;

                case 20:
                    return SecWebSocketAccept; // Sec-WebSocket-Accept

                case 21:
                    return SecWebSocketVersion; // Sec-WebSocket-Version

                case 22:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'A': return AccessControlMaxAge;  // [A]ccess-Control-Max-Age
                        case 'S': return SecWebSocketProtocol; // [S]ec-WebSocket-Protocol
                        case 'X': return XContentTypeOptions;  // [X]-Content-Type-Options
                    }
                    break;

                case 23:
                    return ContentSecurityPolicy; // Content-Security-Policy

                case 24:
                    return SecWebSocketExtensions; // Sec-WebSocket-Extensions

                case 25:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(0)))
                    {
                        case 'S': return StrictTransportSecurity; // [S]trict-Transport-Security
                        case 'U': return UpgradeInsecureRequests; // [U]pgrade-Insecure-Requests
                    }
                    break;

                case 27:
                    return AccessControlAllowOrigin; // Access-Control-Allow-Origin

                case 28:
                    switch (CharArrayHelpers.ToUpperAscii(key.CharAt(21)))
                    {
                        case 'H': return AccessControlAllowHeaders; // Access-Control-Allow-[H]eaders
                        case 'M': return AccessControlAllowMethods; // Access-Control-Allow-[M]ethods
                    }
                    break;

                case 29:
                    return AccessControlExposeHeaders; // Access-Control-Expose-Headers

                case 32:
                    return AccessControlAllowCredentials; // Access-Control-Allow-Credentials
            }

            return null;
        }

        internal static KnownHeader TryGetKnownHeader(string name)
        {
            KnownHeader candidate = GetCandidate(new StringAccessor(name));
            if (candidate != null && StringComparer.OrdinalIgnoreCase.Equals(name, candidate.Name))
            {
                return candidate;
            }

            return null;
        }
    }
}
