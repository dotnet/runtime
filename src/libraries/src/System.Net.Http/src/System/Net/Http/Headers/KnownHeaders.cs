// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.HPack;
using System.Runtime.InteropServices;

namespace System.Net.Http.Headers
{
    internal static class KnownHeaders
    {
        // If you add a new entry here, you need to add it to TryGetKnownHeader below as well.

        public static readonly KnownHeader Accept = new KnownHeader("Accept", HttpHeaderType.Request, MediaTypeHeaderParser.MultipleValuesParser, null, StaticTable.Accept);
        public static readonly KnownHeader AcceptCharset = new KnownHeader("Accept-Charset", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, StaticTable.AcceptCharset);
        public static readonly KnownHeader AcceptEncoding = new KnownHeader("Accept-Encoding", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, StaticTable.AcceptEncoding);
        public static readonly KnownHeader AcceptLanguage = new KnownHeader("Accept-Language", HttpHeaderType.Request, GenericHeaderParser.MultipleValueStringWithQualityParser, null, StaticTable.AcceptLanguage);
        public static readonly KnownHeader AcceptPatch = new KnownHeader("Accept-Patch");
        public static readonly KnownHeader AcceptRanges = new KnownHeader("Accept-Ranges", HttpHeaderType.Response, GenericHeaderParser.TokenListParser, null, StaticTable.AcceptRanges);
        public static readonly KnownHeader AccessControlAllowCredentials = new KnownHeader("Access-Control-Allow-Credentials");
        public static readonly KnownHeader AccessControlAllowHeaders = new KnownHeader("Access-Control-Allow-Headers");
        public static readonly KnownHeader AccessControlAllowMethods = new KnownHeader("Access-Control-Allow-Methods");
        public static readonly KnownHeader AccessControlAllowOrigin = new KnownHeader("Access-Control-Allow-Origin", StaticTable.AccessControlAllowOrigin);
        public static readonly KnownHeader AccessControlExposeHeaders = new KnownHeader("Access-Control-Expose-Headers");
        public static readonly KnownHeader AccessControlMaxAge = new KnownHeader("Access-Control-Max-Age");
        public static readonly KnownHeader Age = new KnownHeader("Age", HttpHeaderType.Response, TimeSpanHeaderParser.Parser, null, StaticTable.Age);
        public static readonly KnownHeader Allow = new KnownHeader("Allow", HttpHeaderType.Content, GenericHeaderParser.TokenListParser, null, StaticTable.Allow);
        public static readonly KnownHeader AltSvc = new KnownHeader("Alt-Svc");
        public static readonly KnownHeader Authorization = new KnownHeader("Authorization", HttpHeaderType.Request, GenericHeaderParser.SingleValueAuthenticationParser, null, StaticTable.Authorization);
        public static readonly KnownHeader CacheControl = new KnownHeader("Cache-Control", HttpHeaderType.General, CacheControlHeaderParser.Parser, null, StaticTable.CacheControl);
        public static readonly KnownHeader Connection = new KnownHeader("Connection", HttpHeaderType.General, GenericHeaderParser.TokenListParser, new string[] { "close" });
        public static readonly KnownHeader ContentDisposition = new KnownHeader("Content-Disposition", HttpHeaderType.Content, GenericHeaderParser.ContentDispositionParser, null, StaticTable.ContentDisposition);
        public static readonly KnownHeader ContentEncoding = new KnownHeader("Content-Encoding", HttpHeaderType.Content, GenericHeaderParser.TokenListParser, new string[] { "gzip", "deflate" }, StaticTable.ContentEncoding);
        public static readonly KnownHeader ContentLanguage = new KnownHeader("Content-Language", HttpHeaderType.Content, GenericHeaderParser.TokenListParser, null, StaticTable.ContentLanguage);
        public static readonly KnownHeader ContentLength = new KnownHeader("Content-Length", HttpHeaderType.Content, Int64NumberHeaderParser.Parser, null, StaticTable.ContentLength);
        public static readonly KnownHeader ContentLocation = new KnownHeader("Content-Location", HttpHeaderType.Content, UriHeaderParser.RelativeOrAbsoluteUriParser, null, StaticTable.ContentLocation);
        public static readonly KnownHeader ContentMD5 = new KnownHeader("Content-MD5", HttpHeaderType.Content, ByteArrayHeaderParser.Parser);
        public static readonly KnownHeader ContentRange = new KnownHeader("Content-Range", HttpHeaderType.Content, GenericHeaderParser.ContentRangeParser, null, StaticTable.ContentRange);
        public static readonly KnownHeader ContentSecurityPolicy = new KnownHeader("Content-Security-Policy");
        public static readonly KnownHeader ContentType = new KnownHeader("Content-Type", HttpHeaderType.Content, MediaTypeHeaderParser.SingleValueParser);
        public static readonly KnownHeader Cookie = new KnownHeader("Cookie", StaticTable.Cookie);
        public static readonly KnownHeader Cookie2 = new KnownHeader("Cookie2");
        public static readonly KnownHeader Date = new KnownHeader("Date", HttpHeaderType.General, DateHeaderParser.Parser, null, StaticTable.Date);
        public static readonly KnownHeader ETag = new KnownHeader("ETag", HttpHeaderType.Response, GenericHeaderParser.SingleValueEntityTagParser, null, StaticTable.ETag);
        public static readonly KnownHeader Expect = new KnownHeader("Expect", HttpHeaderType.Request, GenericHeaderParser.MultipleValueNameValueWithParametersParser, new string[] { "100-continue" }, StaticTable.Expect);
        public static readonly KnownHeader Expires = new KnownHeader("Expires", HttpHeaderType.Content, DateHeaderParser.Parser, null, StaticTable.Expires);
        public static readonly KnownHeader From = new KnownHeader("From", HttpHeaderType.Request, GenericHeaderParser.MailAddressParser, null, StaticTable.From);
        public static readonly KnownHeader Host = new KnownHeader("Host", HttpHeaderType.Request, GenericHeaderParser.HostParser, null, StaticTable.Host);
        public static readonly KnownHeader IfMatch = new KnownHeader("If-Match", HttpHeaderType.Request, GenericHeaderParser.MultipleValueEntityTagParser, null, StaticTable.IfMatch);
        public static readonly KnownHeader IfModifiedSince = new KnownHeader("If-Modified-Since", HttpHeaderType.Request, DateHeaderParser.Parser, null, StaticTable.IfModifiedSince);
        public static readonly KnownHeader IfNoneMatch = new KnownHeader("If-None-Match", HttpHeaderType.Request, GenericHeaderParser.MultipleValueEntityTagParser, null, StaticTable.IfNoneMatch);
        public static readonly KnownHeader IfRange = new KnownHeader("If-Range", HttpHeaderType.Request, GenericHeaderParser.RangeConditionParser, null, StaticTable.IfRange);
        public static readonly KnownHeader IfUnmodifiedSince = new KnownHeader("If-Unmodified-Since", HttpHeaderType.Request, DateHeaderParser.Parser, null, StaticTable.IfUnmodifiedSince);
        public static readonly KnownHeader KeepAlive = new KnownHeader("Keep-Alive");
        public static readonly KnownHeader LastModified = new KnownHeader("Last-Modified", HttpHeaderType.Content, DateHeaderParser.Parser, null, StaticTable.LastModified);
        public static readonly KnownHeader Link = new KnownHeader("Link", StaticTable.Link);
        public static readonly KnownHeader Location = new KnownHeader("Location", HttpHeaderType.Response, UriHeaderParser.RelativeOrAbsoluteUriParser, null, StaticTable.Location);
        public static readonly KnownHeader MaxForwards = new KnownHeader("Max-Forwards", HttpHeaderType.Request, Int32NumberHeaderParser.Parser, null, StaticTable.MaxForwards);
        public static readonly KnownHeader Origin = new KnownHeader("Origin");
        public static readonly KnownHeader P3P = new KnownHeader("P3P");
        public static readonly KnownHeader Pragma = new KnownHeader("Pragma", HttpHeaderType.General, GenericHeaderParser.MultipleValueNameValueParser);
        public static readonly KnownHeader ProxyAuthenticate = new KnownHeader("Proxy-Authenticate", HttpHeaderType.Response, GenericHeaderParser.MultipleValueAuthenticationParser, null, StaticTable.ProxyAuthenticate);
        public static readonly KnownHeader ProxyAuthorization = new KnownHeader("Proxy-Authorization", HttpHeaderType.Request, GenericHeaderParser.SingleValueAuthenticationParser, null, StaticTable.ProxyAuthorization);
        public static readonly KnownHeader ProxyConnection = new KnownHeader("Proxy-Connection");
        public static readonly KnownHeader ProxySupport = new KnownHeader("Proxy-Support");
        public static readonly KnownHeader PublicKeyPins = new KnownHeader("Public-Key-Pins");
        public static readonly KnownHeader Range = new KnownHeader("Range", HttpHeaderType.Request, GenericHeaderParser.RangeParser, null, StaticTable.Range);
        public static readonly KnownHeader Referer = new KnownHeader("Referer", HttpHeaderType.Request, UriHeaderParser.RelativeOrAbsoluteUriParser, null, StaticTable.Referer); // NB: The spelling-mistake "Referer" for "Referrer" must be matched.
        public static readonly KnownHeader Refresh = new KnownHeader("Refresh", StaticTable.Refresh);
        public static readonly KnownHeader RetryAfter = new KnownHeader("Retry-After", HttpHeaderType.Response, GenericHeaderParser.RetryConditionParser, null, StaticTable.RetryAfter);
        public static readonly KnownHeader SecWebSocketAccept = new KnownHeader("Sec-WebSocket-Accept");
        public static readonly KnownHeader SecWebSocketExtensions = new KnownHeader("Sec-WebSocket-Extensions");
        public static readonly KnownHeader SecWebSocketKey = new KnownHeader("Sec-WebSocket-Key");
        public static readonly KnownHeader SecWebSocketProtocol = new KnownHeader("Sec-WebSocket-Protocol");
        public static readonly KnownHeader SecWebSocketVersion = new KnownHeader("Sec-WebSocket-Version");
        public static readonly KnownHeader Server = new KnownHeader("Server", HttpHeaderType.Response, ProductInfoHeaderParser.MultipleValueParser, null, StaticTable.Server);
        public static readonly KnownHeader SetCookie = new KnownHeader("Set-Cookie", StaticTable.SetCookie);
        public static readonly KnownHeader SetCookie2 = new KnownHeader("Set-Cookie2");
        public static readonly KnownHeader StrictTransportSecurity = new KnownHeader("Strict-Transport-Security", StaticTable.StrictTransportSecurity);
        public static readonly KnownHeader TE = new KnownHeader("TE", HttpHeaderType.Request, TransferCodingHeaderParser.MultipleValueWithQualityParser);
        public static readonly KnownHeader TSV = new KnownHeader("TSV");
        public static readonly KnownHeader Trailer = new KnownHeader("Trailer", HttpHeaderType.General, GenericHeaderParser.TokenListParser);
        public static readonly KnownHeader TransferEncoding = new KnownHeader("Transfer-Encoding", HttpHeaderType.General, TransferCodingHeaderParser.MultipleValueParser, new string[] { "chunked" }, StaticTable.TransferEncoding);
        public static readonly KnownHeader Upgrade = new KnownHeader("Upgrade", HttpHeaderType.General, GenericHeaderParser.MultipleValueProductParser);
        public static readonly KnownHeader UpgradeInsecureRequests = new KnownHeader("Upgrade-Insecure-Requests");
        public static readonly KnownHeader UserAgent = new KnownHeader("User-Agent", HttpHeaderType.Request, ProductInfoHeaderParser.MultipleValueParser, null, StaticTable.UserAgent);
        public static readonly KnownHeader Vary = new KnownHeader("Vary", HttpHeaderType.Response, GenericHeaderParser.TokenListParser, null, StaticTable.Vary);
        public static readonly KnownHeader Via = new KnownHeader("Via", HttpHeaderType.General, GenericHeaderParser.MultipleValueViaParser, null, StaticTable.Via);
        public static readonly KnownHeader WWWAuthenticate = new KnownHeader("WWW-Authenticate", HttpHeaderType.Response, GenericHeaderParser.MultipleValueAuthenticationParser, null, StaticTable.WwwAuthenticate);
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
        private unsafe readonly struct BytePtrAccessor : IHeaderNameAccessor
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
                    switch (key[0])
                    {
                        case 'A': case 'a': return Age; // [A]ge
                        case 'P': case 'p': return P3P; // [P]3P
                        case 'T': case 't': return TSV; // [T]SV
                        case 'V': case 'v': return Via; // [V]ia
                    }
                    break;

                case 4:
                    switch (key[0])
                    {
                        case 'D': case 'd': return Date; // [D]ate
                        case 'E': case 'e': return ETag; // [E]Tag
                        case 'F': case 'f': return From; // [F]rom
                        case 'H': case 'h': return Host; // [H]ost
                        case 'L': case 'l': return Link; // [L]ink
                        case 'V': case 'v': return Vary; // [V]ary
                    }
                    break;

                case 5:
                    switch (key[0])
                    {
                        case 'A': case 'a': return Allow; // [A]llow
                        case 'R': case 'r': return Range; // [R]ange
                    }
                    break;

                case 6:
                    switch (key[0])
                    {
                        case 'A': case 'a': return Accept; // [A]ccept
                        case 'C': case 'c': return Cookie; // [C]ookie
                        case 'E': case 'e': return Expect; // [E]xpect
                        case 'O': case 'o': return Origin; // [O]rigin
                        case 'P': case 'p': return Pragma; // [P]ragma
                        case 'S': case 's': return Server; // [S]erver
                    }
                    break;

                case 7:
                    switch (key[0])
                    {
                        case 'A': case 'a': return AltSvc;  // [A]lt-Svc
                        case 'C': case 'c': return Cookie2; // [C]ookie2
                        case 'E': case 'e': return Expires; // [E]xpires
                        case 'R': case 'r':
                            switch (key[3])
                            {
                                case 'E': case 'e': return Referer; // [R]ef[e]rer
                                case 'R': case 'r': return Refresh; // [R]ef[r]esh
                            }
                            break;
                        case 'T': case 't': return Trailer; // [T]railer
                        case 'U': case 'u': return Upgrade; // [U]pgrade
                        case 'W': case 'w': return Warning; // [W]arning
                    }
                    break;

                case 8:
                    switch (key[3])
                    {
                        case 'M': case 'm': return IfMatch;  // If-[M]atch
                        case 'R': case 'r': return IfRange;  // If-[R]ange
                        case 'A': case 'a': return Location; // Loc[a]tion
                    }
                    break;

                case 10:
                    switch (key[0])
                    {
                        case 'C': case 'c': return Connection; // [C]onnection
                        case 'K': case 'k': return KeepAlive;  // [K]eep-Alive
                        case 'S': case 's': return SetCookie;  // [S]et-Cookie
                        case 'U': case 'u': return UserAgent;  // [U]ser-Agent
                    }
                    break;

                case 11:
                    switch (key[0])
                    {
                        case 'C': case 'c': return ContentMD5; // [C]ontent-MD5
                        case 'R': case 'r': return RetryAfter; // [R]etry-After
                        case 'S': case 's': return SetCookie2; // [S]et-Cookie2
                    }
                    break;

                case 12:
                    switch (key[2])
                    {
                        case 'C': case 'c': return AcceptPatch; // Ac[c]ept-Patch
                        case 'N': case 'n': return ContentType; // Co[n]tent-Type
                        case 'X': case 'x': return MaxForwards; // Ma[x]-Forwards
                        case 'M': case 'm': return XMSEdgeRef;  // X-[M]SEdge-Ref
                        case 'P': case 'p': return XPoweredBy;  // X-[P]owered-By
                        case 'R': case 'r': return XRequestID;  // X-[R]equest-ID
                    }
                    break;

                case 13:
                    switch (key[6])
                    {
                        case '-': return AcceptRanges;            // Accept[-]Ranges
                        case 'I': case 'i': return Authorization; // Author[i]zation
                        case 'C': case 'c': return CacheControl;  // Cache-[C]ontrol
                        case 'T': case 't': return ContentRange;  // Conten[t]-Range
                        case 'E': case 'e': return IfNoneMatch;   // If-Non[e]-Match
                        case 'O': case 'o': return LastModified;  // Last-M[o]dified
                        case 'S': case 's': return ProxySupport;  // Proxy-[S]upport
                    }
                    break;

                case 14:
                    switch (key[0])
                    {
                        case 'A': case 'a': return AcceptCharset; // [A]ccept-Charset
                        case 'C': case 'c': return ContentLength; // [C]ontent-Length
                    }
                    break;

                case 15:
                    switch (key[7])
                    {
                        case '-': return XFrameOptions;  // X-Frame[-]Options
                        case 'M': case 'm': return XUACompatible;  // X-UA-Co[m]patible
                        case 'E': case 'e': return AcceptEncoding; // Accept-[E]ncoding
                        case 'K': case 'k': return PublicKeyPins;  // Public-[K]ey-Pins
                        case 'L': case 'l': return AcceptLanguage; // Accept-[L]anguage
                    }
                    break;

                case 16:
                    switch (key[11])
                    {
                        case 'O': case 'o': return ContentEncoding; // Content-Enc[o]ding
                        case 'G': case 'g': return ContentLanguage; // Content-Lan[g]uage
                        case 'A': case 'a': return ContentLocation; // Content-Loc[a]tion
                        case 'C': case 'c': return ProxyConnection; // Proxy-Conne[c]tion
                        case 'I': case 'i': return WWWAuthenticate; // WWW-Authent[i]cate
                        case 'R': case 'r': return XAspNetVersion;  // X-AspNet-Ve[r]sion
                    }
                    break;

                case 17:
                    switch (key[0])
                    {
                        case 'I': case 'i': return IfModifiedSince;  // [I]f-Modified-Since
                        case 'S': case 's': return SecWebSocketKey;  // [S]ec-WebSocket-Key
                        case 'T': case 't': return TransferEncoding; // [T]ransfer-Encoding
                    }
                    break;

                case 18:
                    switch (key[0])
                    {
                        case 'P': case 'p': return ProxyAuthenticate; // [P]roxy-Authenticate
                        case 'X': case 'x': return XContentDuration;  // [X]-Content-Duration
                    }
                    break;

                case 19:
                    switch (key[0])
                    {
                        case 'C': case 'c': return ContentDisposition; // [C]ontent-Disposition
                        case 'I': case 'i': return IfUnmodifiedSince;  // [I]f-Unmodified-Since
                        case 'P': case 'p': return ProxyAuthorization; // [P]roxy-Authorization
                    }
                    break;

                case 20:
                    return SecWebSocketAccept; // Sec-WebSocket-Accept

                case 21:
                    return SecWebSocketVersion; // Sec-WebSocket-Version

                case 22:
                    switch (key[0])
                    {
                        case 'A': case 'a': return AccessControlMaxAge;  // [A]ccess-Control-Max-Age
                        case 'S': case 's': return SecWebSocketProtocol; // [S]ec-WebSocket-Protocol
                        case 'X': case 'x': return XContentTypeOptions;  // [X]-Content-Type-Options
                    }
                    break;

                case 23:
                    return ContentSecurityPolicy; // Content-Security-Policy

                case 24:
                    return SecWebSocketExtensions; // Sec-WebSocket-Extensions

                case 25:
                    switch (key[0])
                    {
                        case 'S': case 's': return StrictTransportSecurity; // [S]trict-Transport-Security
                        case 'U': case 'u': return UpgradeInsecureRequests; // [U]pgrade-Insecure-Requests
                    }
                    break;

                case 27:
                    return AccessControlAllowOrigin; // Access-Control-Allow-Origin

                case 28:
                    switch (key[21])
                    {
                        case 'H': case 'h': return AccessControlAllowHeaders; // Access-Control-Allow-[H]eaders
                        case 'M': case 'm': return AccessControlAllowMethods; // Access-Control-Allow-[M]ethods
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

        internal unsafe static KnownHeader TryGetKnownHeader(ReadOnlySpan<byte> name)
        {
            fixed (byte* p = &MemoryMarshal.GetReference(name))
            {
                KnownHeader candidate = GetCandidate(new BytePtrAccessor(p, name.Length));
                if (candidate != null && ByteArrayHelpers.EqualsOrdinalAsciiIgnoreCase(candidate.Name, name))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
