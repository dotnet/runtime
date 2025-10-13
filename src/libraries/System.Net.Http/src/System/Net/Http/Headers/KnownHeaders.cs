// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.HPack;
using System.Net.Http.QPack;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.Http.Headers
{
    internal static class KnownHeaders
    {
        // If you add a new entry here, you need to add it to TryGetKnownHeader below as well.

        public static readonly KnownHeader PseudoStatus = new(":status", HttpHeaderType.Response, parser: null);
        public static readonly KnownHeader Accept = new("Accept", HttpHeaderType.Request, MediaTypeHeaderParser.MultipleValuesParser, null, H2StaticTable.Accept, H3StaticTable.AcceptAny);
        public static readonly KnownHeader AcceptCharset = new("Accept-Charset", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, H2StaticTable.AcceptCharset);
        public static readonly KnownHeader AcceptEncoding = new("Accept-Encoding", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, H2StaticTable.AcceptEncoding, H3StaticTable.AcceptEncodingGzipDeflateBr);
        public static readonly KnownHeader AcceptLanguage = new("Accept-Language", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, H2StaticTable.AcceptLanguage, H3StaticTable.AcceptLanguage);
        public static readonly KnownHeader AcceptPatch = new("Accept-Patch");
        public static readonly KnownHeader AcceptRanges = new("Accept-Ranges", HttpHeaderType.Response, GenericHeaderParser.TokenListParser, ["bytes", "none"], H2StaticTable.AcceptRanges, H3StaticTable.AcceptRangesBytes);
        public static readonly KnownHeader AccessControlAllowCredentials = new("Access-Control-Allow-Credentials", HttpHeaderType.Response, parser: null, ["true"], http3StaticTableIndex: H3StaticTable.AccessControlAllowCredentials);
        public static readonly KnownHeader AccessControlAllowHeaders = new("Access-Control-Allow-Headers", HttpHeaderType.Response, parser: null, ["*"], http3StaticTableIndex: H3StaticTable.AccessControlAllowHeadersCacheControl);
        public static readonly KnownHeader AccessControlAllowMethods = new("Access-Control-Allow-Methods", HttpHeaderType.Response, parser: null, ["POST", "*"], http3StaticTableIndex: H3StaticTable.AccessControlAllowMethodsGet);
        public static readonly KnownHeader AccessControlAllowOrigin = new("Access-Control-Allow-Origin", HttpHeaderType.Response, parser: null, ["*", "null"], H2StaticTable.AccessControlAllowOrigin, H3StaticTable.AccessControlAllowOriginAny);
        public static readonly KnownHeader AccessControlExposeHeaders = new("Access-Control-Expose-Headers", HttpHeaderType.Response, parser: null, ["*"], H3StaticTable.AccessControlExposeHeadersContentLength);
        public static readonly KnownHeader AccessControlMaxAge = new("Access-Control-Max-Age");
        public static readonly KnownHeader Age = new("Age", HttpHeaderType.Response | HttpHeaderType.NonTrailing, TimeSpanHeaderParser.Parser, ["0"], H2StaticTable.Age, H3StaticTable.Age0);
        public static readonly KnownHeader Allow = new("Allow", HttpHeaderType.Content, GenericHeaderParser.TokenListParser, null, H2StaticTable.Allow);
        public static readonly KnownHeader AltSvc = new("Alt-Svc", HttpHeaderType.Response, GetAltSvcHeaderParser(), ["h3=\":443\"", "h3=\":443\"; ma=2592000", "clear"], http3StaticTableIndex: H3StaticTable.AltSvcClear);
        public static readonly KnownHeader AltUsed = new("Alt-Used", HttpHeaderType.Request, parser: null);
        public static readonly KnownHeader Authorization = new("Authorization", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.SingleValueAuthenticationParser, null, H2StaticTable.Authorization, H3StaticTable.Authorization);
        public static readonly KnownHeader CacheControl = new("Cache-Control", HttpHeaderType.General | HttpHeaderType.NonTrailing, CacheControlHeaderParser.Parser, ["must-revalidate", "no-cache", "no-store", "no-transform", "private", "proxy-revalidate", "public"], H2StaticTable.CacheControl, H3StaticTable.CacheControlMaxAge0);
        public static readonly KnownHeader Connection = new("Connection", HttpHeaderType.General, GenericHeaderParser.TokenListParser, ["keep-alive", "close", "Upgrade", "Transfer-Encoding"]);
        public static readonly KnownHeader ContentDisposition = new("Content-Disposition", HttpHeaderType.Content | HttpHeaderType.NonTrailing, GenericHeaderParser.ContentDispositionParser, ["inline", "attachment"], H2StaticTable.ContentDisposition, H3StaticTable.ContentDisposition);
        public static readonly KnownHeader ContentEncoding = new("Content-Encoding", HttpHeaderType.Content | HttpHeaderType.NonTrailing, GenericHeaderParser.TokenListParser, ["gzip", "deflate", "br", "zstd", "compress", "identity"], H2StaticTable.ContentEncoding, H3StaticTable.ContentEncodingBr);
        public static readonly KnownHeader ContentLanguage = new("Content-Language", HttpHeaderType.Content, GenericHeaderParser.TokenListParser, null, H2StaticTable.ContentLanguage);
        public static readonly KnownHeader ContentLength = new("Content-Length", HttpHeaderType.Content | HttpHeaderType.NonTrailing, Int64NumberHeaderParser.Parser, null, H2StaticTable.ContentLength, H3StaticTable.ContentLength0);
        public static readonly KnownHeader ContentLocation = new("Content-Location", HttpHeaderType.Content | HttpHeaderType.NonTrailing, UriHeaderParser.RelativeOrAbsoluteUriParser, null, H2StaticTable.ContentLocation);
        public static readonly KnownHeader ContentMD5 = new("Content-MD5", HttpHeaderType.Content, ByteArrayHeaderParser.Parser);
        public static readonly KnownHeader ContentRange = new("Content-Range", HttpHeaderType.Content | HttpHeaderType.NonTrailing, GenericHeaderParser.ContentRangeParser, null, H2StaticTable.ContentRange);
        public static readonly KnownHeader ContentSecurityPolicy = new("Content-Security-Policy", ["upgrade-insecure-requests", "frame-ancestors 'self'"], http3StaticTableIndex: H3StaticTable.ContentSecurityPolicyAllNone);
        public static readonly KnownHeader ContentType = new("Content-Type", HttpHeaderType.Content | HttpHeaderType.NonTrailing, MediaTypeHeaderParser.SingleValueParser, null, H2StaticTable.ContentType, H3StaticTable.ContentTypeApplicationDnsMessage);
        public static readonly KnownHeader Cookie = new("Cookie", HttpHeaderType.Custom, CookieHeaderParser.Parser, null, H2StaticTable.Cookie, H3StaticTable.Cookie);
        public static readonly KnownHeader Cookie2 = new("Cookie2");
        public static readonly KnownHeader CrossOriginResourcePolicy = new("Cross-Origin-Resource-Policy", ["cross-origin", "same-site", "same-origin"]);
        public static readonly KnownHeader Date = new("Date", HttpHeaderType.General | HttpHeaderType.NonTrailing, DateHeaderParser.Parser, null, H2StaticTable.Date, H3StaticTable.Date);
        public static readonly KnownHeader ETag = new("ETag", HttpHeaderType.Response, GenericHeaderParser.SingleValueEntityTagParser, null, H2StaticTable.ETag, H3StaticTable.ETag);
        public static readonly KnownHeader Expect = new("Expect", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueNameValueWithParametersParser, ["100-continue"], H2StaticTable.Expect);
        public static readonly KnownHeader ExpectCT = new("Expect-CT");
        public static readonly KnownHeader Expires = new("Expires", HttpHeaderType.Content | HttpHeaderType.NonTrailing, DateHeaderParser.Parser, ["Thu, 01 Jan 1970 00:00:01 GMT", "Mon, 01 Jan 1990 00:00:00 GMT"], H2StaticTable.Expires);
        public static readonly KnownHeader From = new("From", HttpHeaderType.Request, GenericHeaderParser.SingleValueParserWithoutValidation, null, H2StaticTable.From);
        public static readonly KnownHeader GrpcEncoding = new("grpc-encoding", ["identity", "gzip", "deflate"]);
        public static readonly KnownHeader GrpcMessage = new("grpc-message");
        public static readonly KnownHeader GrpcStatus = new("grpc-status", ["0"]);
        public static readonly KnownHeader Host = new("Host", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.HostParser, null, H2StaticTable.Host);
        public static readonly KnownHeader IfMatch = new("If-Match", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueEntityTagParser, null, H2StaticTable.IfMatch);
        public static readonly KnownHeader IfModifiedSince = new("If-Modified-Since", HttpHeaderType.Request | HttpHeaderType.NonTrailing, DateHeaderParser.Parser, null, H2StaticTable.IfModifiedSince, H3StaticTable.IfModifiedSince);
        public static readonly KnownHeader IfNoneMatch = new("If-None-Match", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueEntityTagParser, null, H2StaticTable.IfNoneMatch, H3StaticTable.IfNoneMatch);
        public static readonly KnownHeader IfRange = new("If-Range", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.RangeConditionParser, null, H2StaticTable.IfRange, H3StaticTable.IfRange);
        public static readonly KnownHeader IfUnmodifiedSince = new("If-Unmodified-Since", HttpHeaderType.Request | HttpHeaderType.NonTrailing, DateHeaderParser.Parser, null, H2StaticTable.IfUnmodifiedSince);
        public static readonly KnownHeader KeepAlive = new("Keep-Alive");
        public static readonly KnownHeader LastModified = new("Last-Modified", HttpHeaderType.Content, DateHeaderParser.Parser, null, H2StaticTable.LastModified, H3StaticTable.LastModified);
        public static readonly KnownHeader Link = new("Link", null, H2StaticTable.Link, H3StaticTable.Link);
        public static readonly KnownHeader Location = new("Location", HttpHeaderType.Response | HttpHeaderType.NonTrailing, UriHeaderParser.RelativeOrAbsoluteUriParser, null, H2StaticTable.Location, H3StaticTable.Location);
        public static readonly KnownHeader MaxForwards = new("Max-Forwards", HttpHeaderType.Request | HttpHeaderType.NonTrailing, Int32NumberHeaderParser.Parser, null, H2StaticTable.MaxForwards);
        public static readonly KnownHeader Origin = new("Origin", http3StaticTableIndex: H3StaticTable.Origin);
        public static readonly KnownHeader P3P = new("P3P");
        public static readonly KnownHeader Pragma = new("Pragma", HttpHeaderType.General | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueNameValueParser, ["no-cache"]);
        public static readonly KnownHeader ProxyAuthenticate = new("Proxy-Authenticate", HttpHeaderType.Response | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueAuthenticationParser, null, H2StaticTable.ProxyAuthenticate);
        public static readonly KnownHeader ProxyAuthorization = new("Proxy-Authorization", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.SingleValueAuthenticationParser, null, H2StaticTable.ProxyAuthorization);
        public static readonly KnownHeader ProxyConnection = new("Proxy-Connection");
        public static readonly KnownHeader ProxySupport = new("Proxy-Support");
        public static readonly KnownHeader PublicKeyPins = new("Public-Key-Pins");
        public static readonly KnownHeader Range = new("Range", HttpHeaderType.Request | HttpHeaderType.NonTrailing, GenericHeaderParser.RangeParser, null, H2StaticTable.Range, H3StaticTable.RangeBytes0ToAll);
        public static readonly KnownHeader Referer = new("Referer", HttpHeaderType.Request, UriHeaderParser.RelativeOrAbsoluteUriParser, null, H2StaticTable.Referer, H3StaticTable.Referer); // NB: The spelling-mistake "Referer" for "Referrer" must be matched.
        public static readonly KnownHeader ReferrerPolicy = new("Referrer-Policy", ["same-origin", "strict-origin-when-cross-origin", "no-referrer-when-downgrade", "origin-when-cross-origin", "strict-origin", "origin", "no-referrer", "unsafe-url"]);
        public static readonly KnownHeader Refresh = new("Refresh", null, H2StaticTable.Refresh);
        public static readonly KnownHeader RetryAfter = new("Retry-After", HttpHeaderType.Response | HttpHeaderType.NonTrailing, GenericHeaderParser.RetryConditionParser, null, H2StaticTable.RetryAfter);
        public static readonly KnownHeader SecWebSocketAccept = new("Sec-WebSocket-Accept");
        public static readonly KnownHeader SecWebSocketExtensions = new("Sec-WebSocket-Extensions", ["permessage-deflate"]);
        public static readonly KnownHeader SecWebSocketKey = new("Sec-WebSocket-Key");
        public static readonly KnownHeader SecWebSocketProtocol = new("Sec-WebSocket-Protocol");
        public static readonly KnownHeader SecWebSocketVersion = new("Sec-WebSocket-Version");
        public static readonly KnownHeader Server = new("Server", HttpHeaderType.Response, ProductInfoHeaderParser.MultipleValueParser, null, H2StaticTable.Server, H3StaticTable.Server);
        public static readonly KnownHeader ServerTiming = new("Server-Timing");
        public static readonly KnownHeader SetCookie = new("Set-Cookie", HttpHeaderType.Custom | HttpHeaderType.NonTrailing, null, null, H2StaticTable.SetCookie, H3StaticTable.SetCookie);
        public static readonly KnownHeader SetCookie2 = new("Set-Cookie2", HttpHeaderType.Custom | HttpHeaderType.NonTrailing, null);
        public static readonly KnownHeader StrictTransportSecurity = new("Strict-Transport-Security", ["max-age=31536000", "max-age=31536000; includeSubDomains; preload", "max-age=31536000; includeSubDomains", "max-age=63072000; includeSubDomains; preload"], H2StaticTable.StrictTransportSecurity, H3StaticTable.StrictTransportSecurityMaxAge31536000);
        public static readonly KnownHeader TE = new("TE", HttpHeaderType.Request | HttpHeaderType.NonTrailing, TransferCodingHeaderParser.MultipleValueWithQualityParser, ["trailers", "compress", "deflate", "gzip"]);
        public static readonly KnownHeader TimingAllowOrigin = new("Timing-Allow-Origin", ["*"]);
        public static readonly KnownHeader Trailer = new("Trailer", HttpHeaderType.General | HttpHeaderType.NonTrailing, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader TransferEncoding = new("Transfer-Encoding", HttpHeaderType.General | HttpHeaderType.NonTrailing, TransferCodingHeaderParser.MultipleValueParser, ["chunked", "compress", "deflate", "gzip", "identity"], H2StaticTable.TransferEncoding);
        public static readonly KnownHeader TSV = new("TSV");
        public static readonly KnownHeader Upgrade = new("Upgrade", HttpHeaderType.General, GenericHeaderParser.MultipleValueProductParser);
        public static readonly KnownHeader UpgradeInsecureRequests = new("Upgrade-Insecure-Requests", ["1"], http3StaticTableIndex: H3StaticTable.UpgradeInsecureRequests1);
        public static readonly KnownHeader UserAgent = new("User-Agent", HttpHeaderType.Request, ProductInfoHeaderParser.MultipleValueParser, null, H2StaticTable.UserAgent, H3StaticTable.UserAgent);
        public static readonly KnownHeader Vary = new("Vary", HttpHeaderType.Response | HttpHeaderType.NonTrailing, GenericHeaderParser.TokenListParser, ["Accept-Encoding", "accept-encoding", "Origin", "Referer", "*"], H2StaticTable.Vary, H3StaticTable.VaryAcceptEncoding);
        public static readonly KnownHeader Via = new("Via", HttpHeaderType.General, GenericHeaderParser.MultipleValueViaParser, null, H2StaticTable.Via);
        public static readonly KnownHeader WWWAuthenticate = new("WWW-Authenticate", HttpHeaderType.Response | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueAuthenticationParser, null, H2StaticTable.WwwAuthenticate);
        public static readonly KnownHeader Warning = new("Warning", HttpHeaderType.General | HttpHeaderType.NonTrailing, GenericHeaderParser.MultipleValueWarningParser);
        public static readonly KnownHeader XAspNetVersion = new("X-AspNet-Version");
        public static readonly KnownHeader XCache = new("X-Cache", ["HIT", "MISS", "hit", "miss"]);
        public static readonly KnownHeader XContentDuration = new("X-Content-Duration");
        public static readonly KnownHeader XContentTypeOptions = new("X-Content-Type-Options", ["nosniff"], http3StaticTableIndex: H3StaticTable.XContentTypeOptionsNoSniff);
        public static readonly KnownHeader XFrameOptions = new("X-Frame-Options", ["SAMEORIGIN", "DENY", "sameorigin", "deny"], http3StaticTableIndex: H3StaticTable.XFrameOptionsDeny);
        public static readonly KnownHeader XMSEdgeRef = new("X-MSEdge-Ref");
        public static readonly KnownHeader XPoweredBy = new("X-Powered-By");
        public static readonly KnownHeader XRequestID = new("X-Request-ID");
        public static readonly KnownHeader XServedBy = new("X-Served-By");
        public static readonly KnownHeader XUACompatible = new("X-UA-Compatible");
        public static readonly KnownHeader XXssProtection = new("X-XSS-Protection", ["1; mode=block", "0", "1"]);

#if TARGET_BROWSER || TARGET_WASI
        private static HttpHeaderParser? GetAltSvcHeaderParser() => null; // Allow for the AltSvcHeaderParser to be trimmed on Browser since Alt-Svc is only for SocketsHttpHandler, which isn't used on Browser.
#else
        private static AltSvcHeaderParser? GetAltSvcHeaderParser() => AltSvcHeaderParser.Parser;
#endif

        // Helper interface for making GetCandidate generic over strings, utf8, etc
        private interface IHeaderNameAccessor
        {
            int Length { get; }
            char this[int index] { get; }
        }

        private readonly struct StringAccessor : IHeaderNameAccessor
        {
            private readonly string _string;

            public StringAccessor(string s)
            {
                _string = s;
            }

            public int Length => _string.Length;
            public char this[int index] => _string[index];
        }

        // Can't use Span here as it's unsupported.
        private readonly unsafe struct BytePtrAccessor : IHeaderNameAccessor
        {
            private readonly byte* _p;
            private readonly int _length;

            public BytePtrAccessor(byte* p, int length)
            {
                _p = p;
                _length = length;
            }

            public int Length => _length;
            public char this[int index] => (char)_p[index];
        }

        /// <summary>
        /// Find possible known header match via lookup on length and a distinguishing char for that length.
        /// </summary>
        /// <remarks>
        /// Matching is case-insensitive. Because of this, we do not preserve the case of the original header,
        /// whether from the wire or from the user explicitly setting a known header using a header name string.
        /// </remarks>
        private static KnownHeader? GetCandidate<T>(T key)
            where T : struct, IHeaderNameAccessor     // Enforce struct for performance
        {
            // Lookup is performed by first switching on the header name's length, and then switching
            // on the most unique position in that length's string.

            int length = key.Length;
            switch (length)
            {
                case 2:
                    return TE; // TE

                case 3:
                    switch (key[0] | 0x20)
                    {
                        case 'a': return Age; // [A]ge
                        case 'p': return P3P; // [P]3P
                        case 't': return TSV; // [T]SV
                        case 'v': return Via; // [V]ia
                    }
                    break;

                case 4:
                    switch (key[0] | 0x20)
                    {
                        case 'd': return Date; // [D]ate
                        case 'e': return ETag; // [E]Tag
                        case 'f': return From; // [F]rom
                        case 'h': return Host; // [H]ost
                        case 'l': return Link; // [L]ink
                        case 'v': return Vary; // [V]ary
                    }
                    break;

                case 5:
                    switch (key[0] | 0x20)
                    {
                        case 'a': return Allow; // [A]llow
                        case 'r': return Range; // [R]ange
                    }
                    break;

                case 6:
                    switch (key[0] | 0x20)
                    {
                        case 'a': return Accept; // [A]ccept
                        case 'c': return Cookie; // [C]ookie
                        case 'e': return Expect; // [E]xpect
                        case 'o': return Origin; // [O]rigin
                        case 'p': return Pragma; // [P]ragma
                        case 's': return Server; // [S]erver
                    }
                    break;

                case 7:
                    switch (key[0] | 0x20)
                    {
                        case ':': return PseudoStatus; // [:]status
                        case 'a': return AltSvc;  // [A]lt-Svc
                        case 'c': return Cookie2; // [C]ookie2
                        case 'e': return Expires; // [E]xpires
                        case 'r':
                            switch (key[3] | 0x20)
                            {
                                case 'e': return Referer; // [R]ef[e]rer
                                case 'r': return Refresh; // [R]ef[r]esh
                            }
                            break;
                        case 't': return Trailer; // [T]railer
                        case 'u': return Upgrade; // [U]pgrade
                        case 'w': return Warning; // [W]arning
                        case 'x': return XCache;  // [X]-Cache
                    }
                    break;

                case 8:
                    switch (key[3] | 0x20)
                    {
                        case '-': return AltUsed;  // Alt[-]Used
                        case 'a': return Location; // Loc[a]tion
                        case 'm': return IfMatch;  // If-[M]atch
                        case 'r': return IfRange;  // If-[R]ange
                    }
                    break;

                case 9:
                    return ExpectCT; // Expect-CT

                case 10:
                    switch (key[0] | 0x20)
                    {
                        case 'c': return Connection; // [C]onnection
                        case 'k': return KeepAlive;  // [K]eep-Alive
                        case 's': return SetCookie;  // [S]et-Cookie
                        case 'u': return UserAgent;  // [U]ser-Agent
                    }
                    break;

                case 11:
                    switch (key[0] | 0x20)
                    {
                        case 'c': return ContentMD5; // [C]ontent-MD5
                        case 'g': return GrpcStatus; // [g]rpc-status
                        case 'r': return RetryAfter; // [R]etry-After
                        case 's': return SetCookie2; // [S]et-Cookie2
                        case 'x': return XServedBy;  // [X]-Served-By
                    }
                    break;

                case 12:
                    switch (key[5] | 0x20)
                    {
                        case 'd': return XMSEdgeRef;  // X-MSE[d]ge-Ref
                        case 'e': return XPoweredBy;  // X-Pow[e]red-By
                        case 'm': return GrpcMessage; // grpc-[m]essage
                        case 'n': return ContentType; // Conte[n]t-Type
                        case 'o': return MaxForwards; // Max-F[o]rwards
                        case 't': return AcceptPatch; // Accep[t]-Patch
                        case 'u': return XRequestID;  // X-Req[u]est-ID
                    }
                    break;

                case 13:
                    switch (key[12] | 0x20)
                    {
                        case 'd': return LastModified;  // Last-Modifie[d]
                        case 'e': return ContentRange;  // Content-Rang[e]
                        case 'g':
                            switch (key[0] | 0x20)
                            {
                                case 's': return ServerTiming;  // [S]erver-Timin[g]
                                case 'g': return GrpcEncoding;  // [g]rpc-encodin[g]
                            }
                            break;
                        case 'h': return IfNoneMatch;   // If-None-Matc[h]
                        case 'l': return CacheControl;  // Cache-Contro[l]
                        case 'n': return Authorization; // Authorizatio[n]
                        case 's': return AcceptRanges;  // Accept-Range[s]
                        case 't': return ProxySupport;  // Proxy-Suppor[t]
                    }
                    break;

                case 14:
                    switch (key[0] | 0x20)
                    {
                        case 'a': return AcceptCharset; // [A]ccept-Charset
                        case 'c': return ContentLength; // [C]ontent-Length
                    }
                    break;

                case 15:
                    switch (key[7] | 0x20)
                    {
                        case '-': return XFrameOptions;  // X-Frame[-]Options
                        case 'e': return AcceptEncoding; // Accept-[E]ncoding
                        case 'k': return PublicKeyPins;  // Public-[K]ey-Pins
                        case 'l': return AcceptLanguage; // Accept-[L]anguage
                        case 'm': return XUACompatible;  // X-UA-Co[m]patible
                        case 'r': return ReferrerPolicy; // Referre[r]-Policy
                    }
                    break;

                case 16:
                    switch (key[11] | 0x20)
                    {
                        case 'a': return ContentLocation; // Content-Loc[a]tion
                        case 'c':
                            switch (key[0] | 0x20)
                            {
                                case 'p': return ProxyConnection; // [P]roxy-Conne[c]tion
                                case 'x': return XXssProtection;  // [X]-XSS-Prote[c]tion
                            }
                            break;
                        case 'g': return ContentLanguage; // Content-Lan[g]uage
                        case 'i': return WWWAuthenticate; // WWW-Authent[i]cate
                        case 'o': return ContentEncoding; // Content-Enc[o]ding
                        case 'r': return XAspNetVersion;  // X-AspNet-Ve[r]sion
                    }
                    break;

                case 17:
                    switch (key[0] | 0x20)
                    {
                        case 'i': return IfModifiedSince;  // [I]f-Modified-Since
                        case 's': return SecWebSocketKey;  // [S]ec-WebSocket-Key
                        case 't': return TransferEncoding; // [T]ransfer-Encoding
                    }
                    break;

                case 18:
                    switch (key[0] | 0x20)
                    {
                        case 'p': return ProxyAuthenticate; // [P]roxy-Authenticate
                        case 'x': return XContentDuration;  // [X]-Content-Duration
                    }
                    break;

                case 19:
                    switch (key[0] | 0x20)
                    {
                        case 'c': return ContentDisposition; // [C]ontent-Disposition
                        case 'i': return IfUnmodifiedSince;  // [I]f-Unmodified-Since
                        case 'p': return ProxyAuthorization; // [P]roxy-Authorization
                        case 't': return TimingAllowOrigin;  // [T]iming-Allow-Origin
                    }
                    break;

                case 20:
                    return SecWebSocketAccept; // Sec-WebSocket-Accept

                case 21:
                    return SecWebSocketVersion; // Sec-WebSocket-Version

                case 22:
                    switch (key[0] | 0x20)
                    {
                        case 'a': return AccessControlMaxAge;  // [A]ccess-Control-Max-Age
                        case 's': return SecWebSocketProtocol; // [S]ec-WebSocket-Protocol
                        case 'x': return XContentTypeOptions;  // [X]-Content-Type-Options
                    }
                    break;

                case 23:
                    return ContentSecurityPolicy; // Content-Security-Policy

                case 24:
                    return SecWebSocketExtensions; // Sec-WebSocket-Extensions

                case 25:
                    switch (key[0] | 0x20)
                    {
                        case 's': return StrictTransportSecurity; // [S]trict-Transport-Security
                        case 'u': return UpgradeInsecureRequests; // [U]pgrade-Insecure-Requests
                    }
                    break;

                case 27:
                    return AccessControlAllowOrigin; // Access-Control-Allow-Origin

                case 28:
                    switch (key[21] | 0x20)
                    {
                        case 'h': return AccessControlAllowHeaders; // Access-Control-Allow-[H]eaders
                        case 'm': return AccessControlAllowMethods; // Access-Control-Allow-[M]ethods
                        case '-': return CrossOriginResourcePolicy; // Cross-Origin-Resource[-]Policy
                    }
                    break;

                case 29:
                    return AccessControlExposeHeaders; // Access-Control-Expose-Headers

                case 32:
                    return AccessControlAllowCredentials; // Access-Control-Allow-Credentials
            }

            return null;
        }

        internal static KnownHeader? TryGetKnownHeader(string name)
        {
            KnownHeader? candidate = GetCandidate(new StringAccessor(name));
            if (candidate != null && StringComparer.OrdinalIgnoreCase.Equals(name, candidate.Name))
            {
                return candidate;
            }

            return null;
        }

        internal static unsafe KnownHeader? TryGetKnownHeader(ReadOnlySpan<byte> name)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(name))
            {
                KnownHeader? candidate = GetCandidate(new BytePtrAccessor(p, name.Length));
                if (candidate != null && Ascii.EqualsIgnoreCase(name, candidate.Name))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
