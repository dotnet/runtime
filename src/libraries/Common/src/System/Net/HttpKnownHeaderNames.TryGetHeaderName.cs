// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net
{
    internal static partial class HttpKnownHeaderNames
    {
        private const string Gzip = "gzip";
        private const string Deflate = "deflate";

        public static string GetHeaderValue(string name, ReadOnlySpan<char> value)
        {
            Debug.Assert(name != null);

            if (value.IsEmpty)
            {
                return string.Empty;
            }

            // If it's a known header value, use the known value instead of allocating a new string.

            // Do a really quick reference equals check to see if name is the same object as
            // HttpKnownHeaderNames.ContentEncoding, in which case the value is very likely to
            // be either "gzip" or "deflate".
            if (ReferenceEquals(name, ContentEncoding))
            {
                if (value.Equals(Gzip.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return Gzip;
                }

                if (value.Equals(Deflate.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return Deflate;
                }
            }

            return value.ToString();
        }

        /// <summary>
        /// Gets a known header name string from a matching span segment, using a case-sensitive
        /// ordinal comparison. Used to avoid allocating new strings for known header names.
        /// </summary>
        public static bool TryGetHeaderName(ReadOnlySpan<char> nameSpan, [NotNullWhen(true)] out string? name)
        {
            // When adding a new constant, add it to HttpKnownHeaderNames.cs as well.

            // The lookup works as follows: first switch on the length of the passed-in key.
            //
            //  - If there is only one known header of that length, set potentialHeader to that known header
            //    and goto TryMatch to see if the key fully matches potentialHeader.
            //
            //  - If there are more than one known headers of that length, switch on a unique char from that
            //    set of same-length known headers. Typically this will be the first char, but some sets of
            //    same-length known headers do not have unique chars in the first position, so a char in a
            //    position further in the strings is used. If the char from the key matches one of the
            //    known headers, set potentialHeader to that known header and goto TryMatch to see if the key
            //    fully matches potentialHeader.
            //
            //  - Otherwise, there is no match, so set the out param to null and return false.
            //
            // Matching is case-sensitive: we only want to return a known header that exactly matches the key.

            string potentialHeader;

            switch (nameSpan.Length)
            {
                case 2:
                    potentialHeader = TE; goto TryMatch; // TE

                case 3:
                    switch (nameSpan[0])
                    {
                        case 'A': potentialHeader = Age; goto TryMatch; // [A]ge
                        case 'P': potentialHeader = P3P; goto TryMatch; // [P]3P
                        case 'T': potentialHeader = TSV; goto TryMatch; // [T]SV
                        case 'V': potentialHeader = Via; goto TryMatch; // [V]ia
                    }
                    break;

                case 4:
                    switch (nameSpan[0])
                    {
                        case 'D': potentialHeader = Date; goto TryMatch; // [D]ate
                        case 'E': potentialHeader = ETag; goto TryMatch; // [E]Tag
                        case 'F': potentialHeader = From; goto TryMatch; // [F]rom
                        case 'H': potentialHeader = Host; goto TryMatch; // [H]ost
                        case 'L': potentialHeader = Link; goto TryMatch; // [L]ink
                        case 'V': potentialHeader = Vary; goto TryMatch; // [V]ary
                    }
                    break;

                case 5:
                    switch (nameSpan[0])
                    {
                        case 'A': potentialHeader = Allow; goto TryMatch; // [A]llow
                        case 'R': potentialHeader = Range; goto TryMatch; // [R]ange
                    }
                    break;

                case 6:
                    switch (nameSpan[0])
                    {
                        case 'A': potentialHeader = Accept; goto TryMatch; // [A]ccept
                        case 'C': potentialHeader = Cookie; goto TryMatch; // [C]ookie
                        case 'E': potentialHeader = Expect; goto TryMatch; // [E]xpect
                        case 'O': potentialHeader = Origin; goto TryMatch; // [O]rigin
                        case 'P': potentialHeader = Pragma; goto TryMatch; // [P]ragma
                        case 'S': potentialHeader = Server; goto TryMatch; // [S]erver
                    }
                    break;

                case 7:
                    switch (nameSpan[0])
                    {
                        case 'A': potentialHeader = AltSvc; goto TryMatch;  // [A]lt-Svc
                        case 'C': potentialHeader = Cookie2; goto TryMatch; // [C]ookie2
                        case 'E': potentialHeader = Expires; goto TryMatch; // [E]xpires
                        case 'R': potentialHeader = Referer; goto TryMatch; // [R]eferer
                        case 'T': potentialHeader = Trailer; goto TryMatch; // [T]railer
                        case 'U': potentialHeader = Upgrade; goto TryMatch; // [U]pgrade
                        case 'W': potentialHeader = Warning; goto TryMatch; // [W]arning
                    }
                    break;

                case 8:
                    switch (nameSpan[3])
                    {
                        case 'M': potentialHeader = IfMatch; goto TryMatch;  // If-[M]atch
                        case 'R': potentialHeader = IfRange; goto TryMatch;  // If-[R]ange
                        case 'a': potentialHeader = Location; goto TryMatch; // Loc[a]tion
                    }
                    break;

                case 10:
                    switch (nameSpan[0])
                    {
                        case 'C': potentialHeader = Connection; goto TryMatch; // [C]onnection
                        case 'K': potentialHeader = KeepAlive; goto TryMatch;  // [K]eep-Alive
                        case 'S': potentialHeader = SetCookie; goto TryMatch;  // [S]et-Cookie
                        case 'U': potentialHeader = UserAgent; goto TryMatch;  // [U]ser-Agent
                    }
                    break;

                case 11:
                    switch (nameSpan[0])
                    {
                        case 'C': potentialHeader = ContentMD5; goto TryMatch; // [C]ontent-MD5
                        case 'R': potentialHeader = RetryAfter; goto TryMatch; // [R]etry-After
                        case 'S': potentialHeader = SetCookie2; goto TryMatch; // [S]et-Cookie2
                    }
                    break;

                case 12:
                    switch (nameSpan[2])
                    {
                        case 'c': potentialHeader = AcceptPatch; goto TryMatch; // Ac[c]ept-Patch
                        case 'n': potentialHeader = ContentType; goto TryMatch; // Co[n]tent-Type
                        case 'x': potentialHeader = MaxForwards; goto TryMatch; // Ma[x]-Forwards
                        case 'M': potentialHeader = XMSEdgeRef; goto TryMatch;  // X-[M]SEdge-Ref
                        case 'P': potentialHeader = XPoweredBy; goto TryMatch;  // X-[P]owered-By
                        case 'R': potentialHeader = XRequestID; goto TryMatch;  // X-[R]equest-ID
                    }
                    break;

                case 13:
                    switch (nameSpan[6])
                    {
                        case '-': potentialHeader = AcceptRanges; goto TryMatch;  // Accept[-]Ranges
                        case 'i': potentialHeader = Authorization; goto TryMatch; // Author[i]zation
                        case 'C': potentialHeader = CacheControl; goto TryMatch;  // Cache-[C]ontrol
                        case 't': potentialHeader = ContentRange; goto TryMatch;  // Conten[t]-Range
                        case 'e': potentialHeader = IfNoneMatch; goto TryMatch;   // If-Non[e]-Match
                        case 'o': potentialHeader = LastModified; goto TryMatch;  // Last-M[o]dified
                    }
                    break;

                case 14:
                    switch (nameSpan[0])
                    {
                        case 'A': potentialHeader = AcceptCharset; goto TryMatch; // [A]ccept-Charset
                        case 'C': potentialHeader = ContentLength; goto TryMatch; // [C]ontent-Length
                    }
                    break;

                case 15:
                    switch (nameSpan[7])
                    {
                        case '-': potentialHeader = XFrameOptions; goto TryMatch;  // X-Frame[-]Options
                        case 'm': potentialHeader = XUACompatible; goto TryMatch;  // X-UA-Co[m]patible
                        case 'E': potentialHeader = AcceptEncoding; goto TryMatch; // Accept-[E]ncoding
                        case 'K': potentialHeader = PublicKeyPins; goto TryMatch;  // Public-[K]ey-Pins
                        case 'L': potentialHeader = AcceptLanguage; goto TryMatch; // Accept-[L]anguage
                    }
                    break;

                case 16:
                    switch (nameSpan[11])
                    {
                        case 'o': potentialHeader = ContentEncoding; goto TryMatch; // Content-Enc[o]ding
                        case 'g': potentialHeader = ContentLanguage; goto TryMatch; // Content-Lan[g]uage
                        case 'a': potentialHeader = ContentLocation; goto TryMatch; // Content-Loc[a]tion
                        case 'c': potentialHeader = ProxyConnection; goto TryMatch; // Proxy-Conne[c]tion
                        case 'i': potentialHeader = WWWAuthenticate; goto TryMatch; // WWW-Authent[i]cate
                        case 'r': potentialHeader = XAspNetVersion; goto TryMatch;  // X-AspNet-Ve[r]sion
                    }
                    break;

                case 17:
                    switch (nameSpan[0])
                    {
                        case 'I': potentialHeader = IfModifiedSince; goto TryMatch;  // [I]f-Modified-Since
                        case 'S': potentialHeader = SecWebSocketKey; goto TryMatch;  // [S]ec-WebSocket-Key
                        case 'T': potentialHeader = TransferEncoding; goto TryMatch; // [T]ransfer-Encoding
                    }
                    break;

                case 18:
                    switch (nameSpan[0])
                    {
                        case 'P': potentialHeader = ProxyAuthenticate; goto TryMatch; // [P]roxy-Authenticate
                        case 'X': potentialHeader = XContentDuration; goto TryMatch;  // [X]-Content-Duration
                    }
                    break;

                case 19:
                    switch (nameSpan[0])
                    {
                        case 'C': potentialHeader = ContentDisposition; goto TryMatch; // [C]ontent-Disposition
                        case 'I': potentialHeader = IfUnmodifiedSince; goto TryMatch;  // [I]f-Unmodified-Since
                        case 'P': potentialHeader = ProxyAuthorization; goto TryMatch; // [P]roxy-Authorization
                    }
                    break;

                case 20:
                    potentialHeader = SecWebSocketAccept; goto TryMatch; // Sec-WebSocket-Accept

                case 21:
                    potentialHeader = SecWebSocketVersion; goto TryMatch; // Sec-WebSocket-Version

                case 22:
                    switch (nameSpan[0])
                    {
                        case 'A': potentialHeader = AccessControlMaxAge; goto TryMatch;  // [A]ccess-Control-Max-Age
                        case 'S': potentialHeader = SecWebSocketProtocol; goto TryMatch; // [S]ec-WebSocket-Protocol
                        case 'X': potentialHeader = XContentTypeOptions; goto TryMatch;  // [X]-Content-Type-Options
                    }
                    break;

                case 23:
                    potentialHeader = ContentSecurityPolicy; goto TryMatch; // Content-Security-Policy

                case 24:
                    potentialHeader = SecWebSocketExtensions; goto TryMatch; // Sec-WebSocket-Extensions

                case 25:
                    switch (nameSpan[0])
                    {
                        case 'S': potentialHeader = StrictTransportSecurity; goto TryMatch; // [S]trict-Transport-Security
                        case 'U': potentialHeader = UpgradeInsecureRequests; goto TryMatch; // [U]pgrade-Insecure-Requests
                    }
                    break;

                case 27:
                    potentialHeader = AccessControlAllowOrigin; goto TryMatch; // Access-Control-Allow-Origin

                case 28:
                    switch (nameSpan[21])
                    {
                        case 'H': potentialHeader = AccessControlAllowHeaders; goto TryMatch; // Access-Control-Allow-[H]eaders
                        case 'M': potentialHeader = AccessControlAllowMethods; goto TryMatch; // Access-Control-Allow-[M]ethods
                    }
                    break;

                case 29:
                    potentialHeader = AccessControlExposeHeaders; goto TryMatch; // Access-Control-Expose-Headers

                case 32:
                    potentialHeader = AccessControlAllowCredentials; goto TryMatch; // Access-Control-Allow-Credentials
            }

            name = null;
            return false;

        TryMatch:
            Debug.Assert(potentialHeader != null);

            if (nameSpan.SequenceEqual(potentialHeader.AsSpan()))
            {
                name = potentialHeader;
                return true;
            }

            name = null;
            return false;
        }
    }
}
