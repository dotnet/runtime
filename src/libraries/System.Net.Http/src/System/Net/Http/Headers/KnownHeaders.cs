// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    internal static class KnownHeaders
    {
        // If you add a new entry here, you need to add it to TryGetKnownHeader below as well.

        public static KnownHeader Accept = new KnownHeader("Accept", HttpHeaderType.Request, MediaTypeHeaderParser.MultipleValuesParser);
        public static KnownHeader AcceptCharset = new KnownHeader("Accept-Charset", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser);
        public static KnownHeader AcceptEncoding = new KnownHeader("Accept-Encoding", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser);
        public static KnownHeader AcceptLanguage = new KnownHeader("Accept-Language", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser);
        public static KnownHeader AcceptPatch = new KnownHeader("Accept-Patch");
        public static KnownHeader AcceptRanges = new KnownHeader("Accept-Ranges", HttpHeaderType.Response, GenericHeaderParser.TokenListParser);
        public static KnownHeader AccessControlAllowCredentials = new KnownHeader("Access-Control-Allow-Credentials");
        public static KnownHeader AccessControlAllowHeaders = new KnownHeader("Access-Control-Allow-Headers");
        public static KnownHeader AccessControlAllowMethods = new KnownHeader("Access-Control-Allow-Methods");
        public static KnownHeader AccessControlAllowOrigin = new KnownHeader("Access-Control-Allow-Origin");
        public static KnownHeader AccessControlExposeHeaders = new KnownHeader("Access-Control-Expose-Headers");
        public static KnownHeader AccessControlMaxAge = new KnownHeader("Access-Control-Max-Age");
        public static KnownHeader Age = new KnownHeader("Age", HttpHeaderType.Response, TimeSpanHeaderParser.Parser);
        public static KnownHeader Allow = new KnownHeader("Allow", HttpHeaderType.Content, GenericHeaderParser.TokenListParser);
        public static KnownHeader AltSvc = new KnownHeader("Alt-Svc");
        public static KnownHeader Authorization = new KnownHeader("Authorization", HttpHeaderType.Request, GenericHeaderParser.SingleValueAuthenticationParser);
        public static KnownHeader CacheControl = new KnownHeader("Cache-Control", HttpHeaderType.General, CacheControlHeaderParser.Parser);
        public static KnownHeader Connection = new KnownHeader("Connection", HttpHeaderType.General, GenericHeaderParser.TokenListParser);
        public static KnownHeader ContentDisposition = new KnownHeader("Content-Disposition", HttpHeaderType.Content, GenericHeaderParser.ContentDispositionParser);
        public static KnownHeader ContentEncoding = new KnownHeader("Content-Encoding", HttpHeaderType.Content, GenericHeaderParser.TokenListParser);
        public static KnownHeader ContentLanguage = new KnownHeader("Content-Language", HttpHeaderType.Content, GenericHeaderParser.TokenListParser);
        public static KnownHeader ContentLength = new KnownHeader("Content-Length", HttpHeaderType.Content, Int64NumberHeaderParser.Parser);
        public static KnownHeader ContentLocation = new KnownHeader("Content-Location", HttpHeaderType.Content, UriHeaderParser.RelativeOrAbsoluteUriParser);
        public static KnownHeader ContentMD5 = new KnownHeader("Content-MD5", HttpHeaderType.Content, ByteArrayHeaderParser.Parser);
        public static KnownHeader ContentRange = new KnownHeader("Content-Range", HttpHeaderType.Content, GenericHeaderParser.ContentRangeParser);
        public static KnownHeader ContentSecurityPolicy = new KnownHeader("Content-Security-Policy");
        public static KnownHeader ContentType = new KnownHeader("Content-Type", HttpHeaderType.Content, MediaTypeHeaderParser.SingleValueParser);
        public static KnownHeader Cookie = new KnownHeader("Cookie");
        public static KnownHeader Cookie2 = new KnownHeader("Cookie2");
        public static KnownHeader Date = new KnownHeader("Date", HttpHeaderType.General, DateHeaderParser.Parser);
        public static KnownHeader ETag = new KnownHeader("ETag", HttpHeaderType.Response, GenericHeaderParser.SingleValueEntityTagParser);
        public static KnownHeader Expect = new KnownHeader("Expect", HttpHeaderType.Request, GenericHeaderParser.MultipleValueNameValueWithParametersParser);
        public static KnownHeader Expires = new KnownHeader("Expires", HttpHeaderType.Content, DateHeaderParser.Parser);
        public static KnownHeader From = new KnownHeader("From", HttpHeaderType.Request, GenericHeaderParser.MailAddressParser);
        public static KnownHeader Host = new KnownHeader("Host", HttpHeaderType.Request, GenericHeaderParser.HostParser);
        public static KnownHeader IfMatch = new KnownHeader("If-Match", HttpHeaderType.Request, GenericHeaderParser.MultipleValueEntityTagParser);
        public static KnownHeader IfModifiedSince = new KnownHeader("If-Modified-Since", HttpHeaderType.Request, DateHeaderParser.Parser);
        public static KnownHeader IfNoneMatch = new KnownHeader("If-None-Match", HttpHeaderType.Request, GenericHeaderParser.MultipleValueEntityTagParser);
        public static KnownHeader IfRange = new KnownHeader("If-Range", HttpHeaderType.Request, GenericHeaderParser.RangeConditionParser);
        public static KnownHeader IfUnmodifiedSince = new KnownHeader("If-Unmodified-Since", HttpHeaderType.Request, DateHeaderParser.Parser);
        public static KnownHeader KeepAlive = new KnownHeader("Keep-Alive");
        public static KnownHeader LastModified = new KnownHeader("Last-Modified", HttpHeaderType.Content, DateHeaderParser.Parser);
        public static KnownHeader Link = new KnownHeader("Link");
        public static KnownHeader Location = new KnownHeader("Location", HttpHeaderType.Response, UriHeaderParser.RelativeOrAbsoluteUriParser);
        public static KnownHeader MaxForwards = new KnownHeader("Max-Forwards", HttpHeaderType.Request, Int32NumberHeaderParser.Parser);
        public static KnownHeader Origin = new KnownHeader("Origin");
        public static KnownHeader P3P = new KnownHeader("P3P");
        public static KnownHeader Pragma = new KnownHeader("Pragma", HttpHeaderType.General, GenericHeaderParser.MultipleValueNameValueParser);
        public static KnownHeader ProxyAuthenticate = new KnownHeader("Proxy-Authenticate", HttpHeaderType.Response, GenericHeaderParser.MultipleValueAuthenticationParser);
        public static KnownHeader ProxyAuthorization = new KnownHeader("Proxy-Authorization", HttpHeaderType.Request, GenericHeaderParser.SingleValueAuthenticationParser);
        public static KnownHeader ProxyConnection = new KnownHeader("Proxy-Connection");
        public static KnownHeader PublicKeyPins = new KnownHeader("Public-Key-Pins");
        public static KnownHeader Range = new KnownHeader("Range", HttpHeaderType.Request, GenericHeaderParser.RangeParser);
        public static KnownHeader Referer = new KnownHeader("Referer", HttpHeaderType.Request, UriHeaderParser.RelativeOrAbsoluteUriParser); // NB: The spelling-mistake "Referer" for "Referrer" must be matched.
        public static KnownHeader RetryAfter = new KnownHeader("Retry-After", HttpHeaderType.Response, GenericHeaderParser.RetryConditionParser);
        public static KnownHeader SecWebSocketAccept = new KnownHeader("Sec-WebSocket-Accept");
        public static KnownHeader SecWebSocketExtensions = new KnownHeader("Sec-WebSocket-Extensions");
        public static KnownHeader SecWebSocketKey = new KnownHeader("Sec-WebSocket-Key");
        public static KnownHeader SecWebSocketProtocol = new KnownHeader("Sec-WebSocket-Protocol");
        public static KnownHeader SecWebSocketVersion = new KnownHeader("Sec-WebSocket-Version");
        public static KnownHeader Server = new KnownHeader("Server", HttpHeaderType.Response, ProductInfoHeaderParser.MultipleValueParser);
        public static KnownHeader SetCookie = new KnownHeader("Set-Cookie");
        public static KnownHeader SetCookie2 = new KnownHeader("Set-Cookie2");
        public static KnownHeader StrictTransportSecurity = new KnownHeader("Strict-Transport-Security");
        public static KnownHeader TE = new KnownHeader("TE", HttpHeaderType.Request, TransferCodingHeaderParser.MultipleValueWithQualityParser);
        public static KnownHeader TSV = new KnownHeader("TSV");
        public static KnownHeader Trailer = new KnownHeader("Trailer", HttpHeaderType.General, GenericHeaderParser.TokenListParser);
        public static KnownHeader TransferEncoding = new KnownHeader("Transfer-Encoding", HttpHeaderType.General, TransferCodingHeaderParser.MultipleValueParser);
        public static KnownHeader Upgrade = new KnownHeader("Upgrade", HttpHeaderType.General, GenericHeaderParser.MultipleValueProductParser);
        public static KnownHeader UpgradeInsecureRequests = new KnownHeader("Upgrade-Insecure-Requests");
        public static KnownHeader UserAgent = new KnownHeader("User-Agent", HttpHeaderType.Request, ProductInfoHeaderParser.MultipleValueParser);
        public static KnownHeader Vary = new KnownHeader("Vary", HttpHeaderType.Response, GenericHeaderParser.TokenListParser);
        public static KnownHeader Via = new KnownHeader("Via", HttpHeaderType.General, GenericHeaderParser.MultipleValueViaParser);
        public static KnownHeader WWWAuthenticate = new KnownHeader("WWW-Authenticate", HttpHeaderType.Response, GenericHeaderParser.MultipleValueAuthenticationParser);
        public static KnownHeader Warning = new KnownHeader("Warning", HttpHeaderType.General, GenericHeaderParser.MultipleValueWarningParser);
        public static KnownHeader XAspNetVersion = new KnownHeader("X-AspNet-Version");
        public static KnownHeader XContentDuration = new KnownHeader("X-Content-Duration");
        public static KnownHeader XContentTypeOptions = new KnownHeader("X-Content-Type-Options");
        public static KnownHeader XFrameOptions = new KnownHeader("X-Frame-Options");
        public static KnownHeader XMSEdgeRef = new KnownHeader("X-MSEdge-Ref");
        public static KnownHeader XPoweredBy = new KnownHeader("X-Powered-By");
        public static KnownHeader XRequestID = new KnownHeader("X-Request-ID");
        public static KnownHeader XUACompatible = new KnownHeader("X-UA-Compatible");

        // Helper interface for making GetCandidate generic over strings, utf8, etc
        private interface IHeaderNameAccessor
        {
            int Length { get; }
            char UpperCharAt(int index);
        }

        private struct StringAccessor : IHeaderNameAccessor
        {
            private readonly string _string;

            public StringAccessor(string s)
            {
                _string = s;
            }

            public int Length => _string.Length;
            public char UpperCharAt(int index) => char.ToUpperInvariant(_string[index]);
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
                    switch (key.UpperCharAt(0))
                    {
                        case 'A': return Age; // [A]ge
                        case 'P': return P3P; // [P]3P
                        case 'T': return TSV; // [T]SV
                        case 'V': return Via; // [V]ia
                    }
                    break;

                case 4:
                    switch (key.UpperCharAt(0))
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
                    switch (key.UpperCharAt(0))
                    {
                        case 'A': return Allow; // [A]llow
                        case 'R': return Range; // [R]ange
                    }
                    break;

                case 6:
                    switch (key.UpperCharAt(0))
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
                    switch (key.UpperCharAt(0))
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
                    switch (key.UpperCharAt(3))
                    {
                        case 'M': return IfMatch;  // If-[M]atch
                        case 'R': return IfRange;  // If-[R]ange
                        case 'A': return Location; // Loc[a]tion
                    }
                    break;

                case 10:
                    switch (key.UpperCharAt(0))
                    {
                        case 'C': return Connection; // [C]onnection
                        case 'K': return KeepAlive;  // [K]eep-Alive
                        case 'S': return SetCookie;  // [S]et-Cookie
                        case 'U': return UserAgent;  // [U]ser-Agent
                    }
                    break;

                case 11:
                    switch (key.UpperCharAt(0))
                    {
                        case 'C': return ContentMD5; // [C]ontent-MD5
                        case 'R': return RetryAfter; // [R]etry-After
                        case 'S': return SetCookie2; // [S]et-Cookie2
                    }
                    break;

                case 12:
                    switch (key.UpperCharAt(2))
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
                    switch (key.UpperCharAt(6))
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
                    switch (key.UpperCharAt(0))
                    {
                        case 'A': return AcceptCharset; // [A]ccept-Charset
                        case 'C': return ContentLength; // [C]ontent-Length
                    }
                    break;

                case 15:
                    switch (key.UpperCharAt(7))
                    {
                        case '-': return XFrameOptions;  // X-Frame[-]Options
                        case 'M': return XUACompatible;  // X-UA-Co[m]patible
                        case 'E': return AcceptEncoding; // Accept-[E]ncoding
                        case 'K': return PublicKeyPins;  // Public-[K]ey-Pins
                        case 'L': return AcceptLanguage; // Accept-[L]anguage
                    }
                    break;

                case 16:
                    switch (key.UpperCharAt(11))
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
                    switch (key.UpperCharAt(0))
                    {
                        case 'I': return IfModifiedSince;  // [I]f-Modified-Since
                        case 'S': return SecWebSocketKey;  // [S]ec-WebSocket-Key
                        case 'T': return TransferEncoding; // [T]ransfer-Encoding
                    }
                    break;

                case 18:
                    switch (key.UpperCharAt(0))
                    {
                        case 'P': return ProxyAuthenticate; // [P]roxy-Authenticate
                        case 'X': return XContentDuration;  // [X]-Content-Duration
                    }
                    break;

                case 19:
                    switch (key.UpperCharAt(0))
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
                    switch (key.UpperCharAt(0))
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
                    switch (key.UpperCharAt(0))
                    {
                        case 'S': return StrictTransportSecurity; // [S]trict-Transport-Security
                        case 'U': return UpgradeInsecureRequests; // [U]pgrade-Insecure-Requests
                    }
                    break;

                case 27:
                    return AccessControlAllowOrigin; // Access-Control-Allow-Origin

                case 28:
                    switch (key.UpperCharAt(21))
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
